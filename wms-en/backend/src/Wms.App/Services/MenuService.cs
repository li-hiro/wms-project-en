using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;

namespace Wms.App.Services;

/// <summary>菜单管理服务：负责用户侧边栏菜单树与菜单维护</summary>
public class MenuService
{
    private readonly WmsDbContext _db;
    private readonly UserAccessService _access;

    public MenuService(WmsDbContext db, UserAccessService access)
    {
        _db = db;
        _access = access;
    }

    /// <summary>获取指定用户可见的菜单树</summary>
    public async Task<List<MenuTreeDto>> GetUserMenuTreeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var menuIds = await _access.GetMenuIdsAsync(userId, cancellationToken);
        var menus = await _db.Menus
            .Where(x => x.Visible && menuIds.Contains(x.Id))
            .OrderBy(x => x.Sort).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return BuildTree(menus);
    }

    /// <summary>获取全部菜单树（用于菜单管理页）</summary>
    public async Task<List<MenuDto>> GetMenuTreeAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _db.Menus.OrderBy(x => x.Sort).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var containers = new Dictionary<long, MenuDto>();
        foreach (var menu in menus)
            containers[menu.Id] = ToDto(menu);

        var roots = new List<MenuDto>();
        foreach (var menu in menus)
        {
            var node = containers[menu.Id];
            if (menu.ParentId == 0 || !containers.ContainsKey(menu.ParentId))
                roots.Add(node);
            else
                containers[menu.ParentId].Children.Add(node);
        }

        return roots;
    }

    /// <summary>创建菜单</summary>
    public async Task<long> CreateAsync(SaveMenuRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var menu = new Menu();
        Apply(menu, request);
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync(cancellationToken);
        return menu.Id;
    }

    /// <summary>更新菜单</summary>
    public async Task UpdateAsync(long id, SaveMenuRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        if (request.Id.HasValue && request.ParentId == id)
            throw new BusinessException("The parent menu cannot be itself");

        var menu = await _db.Menus.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("Menu not found");

        if (request.ParentId > 0 && await IsDescendantAsync(id, request.ParentId, cancellationToken))
            throw new BusinessException("The parent menu cannot be one of its own submenus");

        Apply(menu, request);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除菜单（存在下级菜单时禁止删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (await _db.Menus.AnyAsync(x => x.ParentId == id, cancellationToken))
            throw new BusinessException("Delete submenus first");

        var menu = await _db.Menus.FindAsync(new object[] { id }, cancellationToken)
                   ?? throw new NotFoundException("Menu not found");

        _db.RoleMenus.RemoveRange(_db.RoleMenus.Where(x => x.MenuId == id));
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(SaveMenuRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("Menu name cannot be empty");
        if (request.Type == (int)MenuType.Menu && string.IsNullOrWhiteSpace(request.Path))
            throw new BusinessException("Menu-type entries must have a route path");
    }

    private static void Apply(Menu menu, SaveMenuRequest request)
    {
        menu.Name = request.Name.Trim();
        menu.Path = request.Path;
        menu.Component = request.Component;
        menu.Icon = request.Icon;
        menu.PermissionCode = request.PermissionCode;
        menu.ParentId = request.ParentId;
        menu.Sort = request.Sort;
        menu.Type = (MenuType)request.Type;
        menu.Visible = request.Visible;
    }

    private async Task<bool> IsDescendantAsync(long ancestorId, long candidateId, CancellationToken cancellationToken)
    {
        var menus = await _db.Menus.Select(x => new { x.Id, x.ParentId }).ToListAsync(cancellationToken);
        var parentMap = menus.ToDictionary(x => x.Id, x => x.ParentId);
        var current = candidateId;
        while (current != 0)
        {
            if (current == ancestorId) return true;
            current = parentMap.TryGetValue(current, out var parent) ? parent : 0;
        }
        return false;
    }

    private static MenuDto ToDto(Menu menu) => new()
    {
        Id = menu.Id,
        Name = menu.Name,
        Path = menu.Path,
        Component = menu.Component,
        Icon = menu.Icon,
        PermissionCode = menu.PermissionCode,
        ParentId = menu.ParentId,
        Sort = menu.Sort,
        Type = (int)menu.Type,
        Visible = menu.Visible
    };

    private static List<MenuTreeDto> BuildTree(List<Menu> menus)
    {
        var containers = menus.ToDictionary(menu => menu.Id, menu => new MenuTreeDto
        {
            Id = menu.Id,
            Name = menu.Name,
            Path = menu.Path,
            Icon = menu.Icon,
            Permission = menu.PermissionCode,
            Type = (int)menu.Type
        });

        var roots = new List<MenuTreeDto>();
        foreach (var menu in menus)
        {
            var node = containers[menu.Id];
            if (menu.ParentId == 0 || !containers.ContainsKey(menu.ParentId))
                roots.Add(node);
            else
                containers[menu.ParentId].Children.Add(node);
        }

        return roots;
    }
}
