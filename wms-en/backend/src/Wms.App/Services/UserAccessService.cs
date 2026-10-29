using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Entities;
using Wms.Core.Common;

namespace Wms.App.Services;

/// <summary>
/// 用户权限解析服务：集中处理角色、权限与菜单的关联查询，供认证与授权逻辑复用。
/// </summary>
public class UserAccessService
{
    private readonly WmsDbContext _db;

    public UserAccessService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>获取用户角色</summary>
    public async Task<List<Role>> GetRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _db.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        return await _db.Roles
            .Where(x => roleIds.Contains(x.Id) && x.Status == CommonStatus.Enabled)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>获取用户权限编码集合（超级管理员返回通配权限）</summary>
    public async Task<List<string>> GetPermissionCodesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(userId, cancellationToken);
        if (roles.Any(x => x.Code == "admin"))
            return new List<string> { PermissionCodes.All };

        var roleIds = roles.Select(x => x.Id).ToList();
        var codes = await (
            from rp in _db.RolePermissions
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where roleIds.Contains(rp.RoleId)
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);

        return codes;
    }

    /// <summary>判断用户是否为超级管理员</summary>
    public async Task<bool> IsAdminAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(userId, cancellationToken);
        return roles.Any(x => x.Code == "admin");
    }

    /// <summary>获取用户可访问的菜单 ID 集合</summary>
    public async Task<List<long>> GetMenuIdsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return await _db.Menus.Select(x => x.Id).ToListAsync(cancellationToken);

        var roleIds = await _db.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        return await _db.RoleMenus
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.MenuId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
