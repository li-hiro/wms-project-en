using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>用户管理接口</summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>分页查询用户列表</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.UserView)]
    public async Task<ApiResult<PagedResult<UserDto>>> Query([FromQuery] UserQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<UserDto>>.Ok(await _userService.QueryAsync(query, cancellationToken));

    /// <summary>创建用户</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.UserEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _userService.CreateAsync(request, cancellationToken), "User created successfully");

    /// <summary>更新用户</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.UserEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        await _userService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "User updated successfully");
    }

    /// <summary>删除用户</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.UserEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "User deleted successfully");
    }

    /// <summary>重置用户密码，未指定密码时由系统生成随机密码并返回</summary>
    [HttpPost("{id:long}/reset-password")]
    [HasPermission(PermissionCodes.UserEdit)]
    public async Task<ApiResult<string>> ResetPassword(long id, [FromBody] ResetPasswordRequest? request, CancellationToken cancellationToken)
        => ApiResult<string>.Ok(await _userService.ResetPasswordAsync(id, request?.Password, cancellationToken), "Password has been reset");

    /// <summary>启用 / 停用用户</summary>
    [HttpPut("{id:long}/status")]
    [HasPermission(PermissionCodes.UserEdit)]
    public async Task<ApiResult<object>> ToggleStatus(long id, [FromQuery] CommonStatus status, CancellationToken cancellationToken)
    {
        await _userService.ToggleStatusAsync(id, status, cancellationToken);
        return ApiResult<object>.Ok(new object(), status == CommonStatus.Enabled ? "User enabled" : "User disabled");
    }
}

/// <summary>角色与权限接口</summary>
[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;

    public RolesController(RoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>查询全部角色（含权限与菜单授权）</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.RoleView)]
    public async Task<ApiResult<List<RoleDto>>> List(CancellationToken cancellationToken)
        => ApiResult<List<RoleDto>>.Ok(await _roleService.ListAsync(cancellationToken));

    /// <summary>查询全部权限项（用于角色授权勾选）</summary>
    [HttpGet("permissions")]
    [HasPermission(PermissionCodes.RoleView)]
    public async Task<ApiResult<List<PermissionDto>>> Permissions(CancellationToken cancellationToken)
        => ApiResult<List<PermissionDto>>.Ok(await _roleService.ListPermissionsAsync(cancellationToken));

    /// <summary>创建角色</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.RoleEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _roleService.CreateAsync(request, cancellationToken), "Role created successfully");

    /// <summary>更新角色</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.RoleEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveRoleRequest request, CancellationToken cancellationToken)
    {
        await _roleService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Role updated successfully");
    }

    /// <summary>删除角色</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.RoleEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Role deleted successfully");
    }
}

/// <summary>菜单管理接口</summary>
[ApiController]
[Route("api/menus")]
public class MenusController : ControllerBase
{
    private readonly MenuService _menuService;

    public MenusController(MenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>查询完整菜单树（菜单管理页使用）</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.MenuView)]
    public async Task<ApiResult<List<MenuDto>>> Tree(CancellationToken cancellationToken)
        => ApiResult<List<MenuDto>>.Ok(await _menuService.GetMenuTreeAsync(cancellationToken));

    /// <summary>创建菜单</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.MenuEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveMenuRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _menuService.CreateAsync(request, cancellationToken), "Menu created successfully");

    /// <summary>更新菜单</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.MenuEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveMenuRequest request, CancellationToken cancellationToken)
    {
        await _menuService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Menu updated successfully");
    }

    /// <summary>删除菜单</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.MenuEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _menuService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Menu deleted successfully");
    }
}

/// <summary>操作日志接口</summary>
[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly OperationLogService _logService;

    public LogsController(OperationLogService logService)
    {
        _logService = logService;
    }

    /// <summary>分页查询操作日志</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.LogView)]
    public async Task<ApiResult<PagedResult<LogDto>>> Query([FromQuery] LogQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<LogDto>>.Ok(await _logService.QueryAsync(query, cancellationToken));

    /// <summary>清理指定天数之前的日志</summary>
    [HttpDelete]
    [HasPermission(PermissionCodes.LogView)]
    public async Task<ApiResult<int>> Clear([FromQuery] int keepDays = 30, CancellationToken cancellationToken = default)
        => ApiResult<int>.Ok(await _logService.ClearAsync(keepDays, cancellationToken), "Log cleanup completed");
}
