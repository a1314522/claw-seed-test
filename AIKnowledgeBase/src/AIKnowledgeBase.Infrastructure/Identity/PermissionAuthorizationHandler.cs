using Microsoft.AspNetCore.Authorization;

namespace AIKnowledgeBase.Infrastructure.Identity;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
            context.Succeed(requirement);
        else if (context.User.HasClaim(c => c.Type == "is_admin" && c.Value == "true"))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
