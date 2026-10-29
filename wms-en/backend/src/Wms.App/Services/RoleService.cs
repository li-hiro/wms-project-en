using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;

namespace Wms.App.Services;

/// <summary>角色与权限管理服务</summary>
public class RoleService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RoleService(WmsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>查询全部角色</summary>
    public async Task<List<RoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var roleIds = roles.Select(x => x.Id).ToList();

        var userCounts = await _db.UserRoles
            .Where(x => roleIds.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var permissionLinks = await _db.RolePermissions
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.PermissionId })
            .ToListAsync(cancellationToken);

        var menuLinks = await _db.RoleMenus
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.MenuId })
            .ToListAsync(cancellationToken);

        return roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Remark = role.Remark,
            Status = role.Status,
            UserCount = userCounts.FirstOrDefault(x => x.RoleId == role.Id)?.Count ?? 0,
            PermissionIds = permissionLinks.Where(x => x.RoleId == role.Id).Select(x => x.PermissionId).ToList(),
            MenuIds = menuLinks.Where(x => x.RoleId == role.Id).Select(x => x.MenuId).ToList()
        }).ToList();
    }

    /// <summary>查询全部权限项</summary>
    public async Task<List<PermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken = default)
        => await _db.Permissions
            .OrderBy(x => x.Module).ThenBy(x => x.Sort)
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Module = x.Module,
                Sort = x.Sort
            })
            .ToListAsync(cancellationToken);

    /// <summary>创建角色</summary>
    public async Task<long> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("Role code and name are required");

        if (await _db.Roles.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException($"Role code {code} already exists");

        var role = new Role
        {
            Code = code,
            Name = request.Name.Trim(),
            Remark = request.Remark,
            Status = request.Status,
            CreatedBy = _currentUser.UserId
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        await ReplaceGrantsAsync(role.Id, request, cancellationToken);
        return role.Id;
    }

    /// <summary>更新角色</summary>
    public async Task UpdateAsync(long id, SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("Role not found");

        if (role.Code == "admin" && request.Status != CommonStatus.Enabled)
            throw new BusinessException("The built-in administrator role cannot be disabled");

        var code = request.Code.Trim();
        if (await _db.Roles.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"Role code {code} already exists");

        role.Code = code;
        role.Name = request.Name.Trim();
        role.Remark = request.Remark;
        role.Status = request.Status;
        role.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
        await ReplaceGrantsAsync(id, request, cancellationToken);
    }

    /// <summary>删除角色（内置管理员角色与已分配用户的角色不可删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("Role not found");

        if (role.Code == "admin")
            throw new BusinessException("The built-in administrator role cannot be deleted");

        if (await _db.UserRoles.AnyAsync(x => x.RoleId == id, cancellationToken))
            throw new BusinessException("Users are still assigned to this role. Remove the assignments first");

        _db.RolePermissions.RemoveRange(_db.RolePermissions.Where(x => x.RoleId == id));
        _db.RoleMenus.RemoveRange(_db.RoleMenus.Where(x => x.RoleId == id));
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceGrantsAsync(long roleId, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var permissionIds = request.PermissionIds.Distinct().ToList();
        var validPermissionIds = await _db.Permissions
            .Where(x => permissionIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);

        var menuIds = request.MenuIds.Distinct().ToList();
        var validMenuIds = await _db.Menus
            .Where(x => menuIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);

        _db.RolePermissions.RemoveRange(_db.RolePermissions.Where(x => x.RoleId == roleId));
        _db.RoleMenus.RemoveRange(_db.RoleMenus.Where(x => x.RoleId == roleId));

        foreach (var permissionId in validPermissionIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });

        foreach (var menuId in validMenuIds)
            _db.RoleMenus.Add(new RoleMenu { RoleId = roleId, MenuId = menuId });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
