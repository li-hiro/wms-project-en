using Microsoft.AspNetCore.Authorization;

namespace Wms.WebApi.Authorization;

/// <summary>
/// 权限声明特性：使用方式 [HasPermission(PermissionCodes.MaterialView)]。
/// 会生成 perm:{code} 形式的授权策略，由 PermissionPolicyProvider 动态解析。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
    {
        Policy = $"perm:{permissionCode}";
    }
}

/// <summary>权限校验需求</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}
