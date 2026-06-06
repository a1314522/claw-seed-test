# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/AIKnowledgeBase.sln
RUN dotnet publish src/AIKnowledgeBase.API/AIKnowledgeBase.API.csproj -c Release -o /app/api
RUN dotnet publish src/AIKnowledgeBase.Web/AIKnowledgeBase.Web.csproj -c Release -o /app/web

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/api .
COPY --from=build /app/web/wwwroot ./wwwroot
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "AIKnowledgeBase.API.dll"]
