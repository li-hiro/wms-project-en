using Wms.Core.Common;

namespace Wms.App.Dtos;

// ---------------- 用户 ----------------

/// <summary>用户查询条件</summary>
public class UserQuery : PagedQuery
{
    public CommonStatus? Status { get; set; }
    public long? RoleId { get; set; }
}

/// <summary>用户列表项</summary>
public class UserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<long> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

/// <summary>保存用户请求</summary>
public class SaveUserRequest
{
    public long? Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public List<long> RoleIds { get; set; } = new();
}

// ---------------- 角色 ----------------

/// <summary>角色列表项</summary>
public class RoleDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public CommonStatus Status { get; set; }
    public int UserCount { get; set; }
    public List<long> PermissionIds { get; set; } = new();
    public List<long> MenuIds { get; set; } = new();
}

/// <summary>保存角色请求</summary>
public class SaveRoleRequest
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public List<long> PermissionIds { get; set; } = new();
    public List<long> MenuIds { get; set; } = new();
}

// ---------------- 权限与菜单 ----------------

/// <summary>权限项</summary>
public class PermissionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int Sort { get; set; }
}

/// <summary>菜单管理节点</summary>
public class MenuDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public long ParentId { get; set; }
    public int Sort { get; set; }
    public int Type { get; set; }
    public bool Visible { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}

/// <summary>保存菜单请求</summary>
public class SaveMenuRequest
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public long ParentId { get; set; }
    public int Sort { get; set; }
    public int Type { get; set; }
    public bool Visible { get; set; } = true;
}

// ---------------- 操作日志 ----------------

/// <summary>日志查询条件</summary>
public class LogQuery : PagedQuery
{
    public string? Module { get; set; }
    public bool? Success { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>日志列表项</summary>
public class LogDto
{
    public long Id { get; set; }
    public string? UserName { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? Params { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------------- 插件 ----------------

/// <summary>插件信息</summary>
public class PluginDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
    public bool BuiltIn { get; set; } = true;
}

/// <summary>更新插件状态与配置</summary>
public class UpdatePluginRequest
{
    public bool Enabled { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();
}

/// <summary>重置密码请求（password 为空时由系统生成随机密码）</summary>
public class ResetPasswordRequest
{
    public string? Password { get; set; }
}
