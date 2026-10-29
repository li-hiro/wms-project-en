using System.Security.Claims;
using Wms.Core.Common;
using Wms.Core.Security;

namespace Wms.WebApi.Infrastructure;

/// <summary>
/// 基于 HttpContext 的当前用户实现，将 JWT 声明转换为业务层可用的用户上下文。
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    private List<string>? _roles;
    private List<string>? _permissions;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public long? UserId
    {
        get
        {
            var value = Principal?.FindFirst(JwtTokenService.ClaimUserId)?.Value;
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? NickName => Principal?.FindFirst(JwtTokenService.ClaimNickName)?.Value;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            _roles ??= Principal?.FindAll(JwtTokenService.ClaimRole).Select(x => x.Value).ToList() ?? new List<string>();
            return _roles;
        }
    }

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            _permissions ??= Principal?.FindAll(JwtTokenService.ClaimPermission).Select(x => x.Value).ToList() ?? new List<string>();
            return _permissions;
        }
    }

    public bool HasPermission(string code)
        => Permissions.Contains(PermissionCodes.All) || Permissions.Contains(code);
}
