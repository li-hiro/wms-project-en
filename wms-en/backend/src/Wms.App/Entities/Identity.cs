using Wms.Core.Common;

namespace Wms.App.Entities;

/// <summary>系统用户</summary>
public class User : BaseEntity
{
    /// <summary>登录账号（唯一）</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>用户昵称</summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>密码散列值（PBKDF2）</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>手机号</summary>
    public string? Phone { get; set; }

    /// <summary>账号状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>角色</summary>
public class Role : BaseEntity
{
    /// <summary>角色编码（唯一），如 admin、keeper</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>角色名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>角色描述</summary>
    public string? Remark { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

/// <summary>权限项</summary>
public class Permission : BaseEntity
{
    /// <summary>权限编码，如 material:view</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>权限名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属模块</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>排序值</summary>
    public int Sort { get; set; }
}

/// <summary>菜单（同时用于前端侧边栏与路由权限控制）</summary>
public class Menu : BaseEntity
{
    /// <summary>菜单名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>路由路径</summary>
    public string? Path { get; set; }

    /// <summary>前端组件路径（保留字段，前端采用静态路由）</summary>
    public string? Component { get; set; }

    /// <summary>图标类名（Element UI 图标）</summary>
    public string? Icon { get; set; }

    /// <summary>访问所需权限编码</summary>
    public string? PermissionCode { get; set; }

    /// <summary>上级菜单 ID，0 表示顶级</summary>
    public long ParentId { get; set; }

    /// <summary>排序值</summary>
    public int Sort { get; set; }

    /// <summary>菜单类型：目录/菜单</summary>
    public MenuType Type { get; set; } = MenuType.Menu;

    /// <summary>是否显示</summary>
    public bool Visible { get; set; } = true;
}

/// <summary>用户-角色关联</summary>
public class UserRole : BaseEntity
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
}

/// <summary>角色-权限关联</summary>
public class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}

/// <summary>角色-菜单关联</summary>
public class RoleMenu : BaseEntity
{
    public long RoleId { get; set; }
    public long MenuId { get; set; }
}
