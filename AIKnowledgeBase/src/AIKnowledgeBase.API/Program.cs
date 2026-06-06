using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;
using AIKnowledgeBase.Infrastructure.Identity;
using AIKnowledgeBase.Infrastructure.Services;
using AIKnowledgeBase.Infrastructure.Repositories;
using AIKnowledgeBase.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=knowledgebase;Username=postgres;Password=postgres";
var dbProvider = builder.Configuration.GetValue<string>("Database:Provider", "postgresql");

if (dbProvider == "postgresql")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, b => b.MigrationsAssembly("AIKnowledgeBase.API")));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString, b => b.MigrationsAssembly("AIKnowledgeBase.API")));
}

// JWT
var jwtSecret = builder.Configuration.GetValue<string>("Jwt:Secret") ?? "DefaultSecretKey12345678901234567890!";
var jwtExpiry = builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
builder.Services.AddSingleton(new JwtService(jwtSecret, jwtExpiry));

// Identity
builder.Services.AddScoped<PasswordHasher>();

// Repositories & UoW
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDocumentParser, DocumentParserService>();
builder.Services.AddScoped<ISearchEngine, SearchEngineService>();

// Asset Management Services
builder.Services.AddScoped<IAssetService, AssetService>();

// MeiliSearch
var meiliHost = builder.Configuration.GetValue<string>("MeiliSearch:Host") ?? "http://localhost:7700";
var meiliKey = builder.Configuration.GetValue<string>("MeiliSearch:ApiKey") ?? "masterKey";
builder.Services.AddSingleton<IMeiliSearchService>(new MeiliSearchService(meiliHost, meiliKey));
var ollamaEnabled = builder.Configuration.GetValue<bool>("Ollama:Enabled", false);
var ollamaUrl = builder.Configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
var ollamaModel = builder.Configuration.GetValue<string>("Ollama:Model") ?? "qwen2.5";
if (ollamaEnabled)
{
    builder.Services.AddHttpClient<ILLMService, OllamaLLMService>(client =>
    {
        client.BaseAddress = new Uri(ollamaUrl);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
}
else
{
    builder.Services.AddScoped<ILLMService, MockLLMService>();
}

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "AIKnowledgeBase",
            ValidAudience = "AIKnowledgeBase",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireClaim("is_admin", "true"));
    options.AddPolicy("RequireUserView", policy => policy.RequireClaim("permission", "UserView"));
    options.AddPolicy("RequireUserCreate", policy => policy.RequireClaim("permission", "UserCreate"));
    options.AddPolicy("RequireUserEdit", policy => policy.RequireClaim("permission", "UserEdit"));
    options.AddPolicy("RequireUserDelete", policy => policy.RequireClaim("permission", "UserDelete"));
    options.AddPolicy("RequireRoleManage", policy => policy.RequireClaim("permission", "RoleManage"));
    options.AddPolicy("RequireCategoryView", policy => policy.RequireClaim("permission", "CategoryView"));
    options.AddPolicy("RequireCategoryCreate", policy => policy.RequireClaim("permission", "CategoryCreate"));
    options.AddPolicy("RequireCategoryEdit", policy => policy.RequireClaim("permission", "CategoryEdit"));
    options.AddPolicy("RequireCategoryDelete", policy => policy.RequireClaim("permission", "CategoryDelete"));
    options.AddPolicy("RequireDocumentView", policy => policy.RequireClaim("permission", "DocumentView"));
    options.AddPolicy("RequireDocumentUpload", policy => policy.RequireClaim("permission", "DocumentUpload"));
    options.AddPolicy("RequireDocumentDelete", policy => policy.RequireClaim("permission", "DocumentDelete"));
    options.AddPolicy("RequireDocumentManage", policy => policy.RequireClaim("permission", "DocumentManage"));
    options.AddPolicy("RequireSearchAll", policy => policy.RequireClaim("permission", "SearchAll"));
    options.AddPolicy("RequireHistoryView", policy => policy.RequireClaim("permission", "HistoryView"));
    options.AddPolicy("RequireHistoryClear", policy => policy.RequireClaim("permission", "HistoryClear"));
    options.AddPolicy("RequireSystemManage", policy => policy.RequireClaim("permission", "SystemManage"));
});

// Authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Knowledge Base API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(context);

    var searchEngine = scope.ServiceProvider.GetRequiredService<ISearchEngine>();
    await searchEngine.BuildIndexAsync();
}

// Middleware
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program { }
