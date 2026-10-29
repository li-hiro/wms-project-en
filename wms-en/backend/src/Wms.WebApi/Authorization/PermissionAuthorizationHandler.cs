using Microsoft.AspNetCore.Authorization;
using Wms.Core.Common;

namespace Wms.WebApi.Authorization;

/// <summary>
/// 权限校验处理器：超级管理员（通配权限 *）直接放行，其余按权限编码匹配。
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll("permission").Select(x => x.Value).ToList();
        if (permissions.Contains(PermissionCodes.All) || permissions.Contains(requirement.PermissionCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
