namespace Wms.Core.Common;

/// <summary>
/// 当前登录用户上下文抽象，由 Web 层实现，业务层依赖该抽象获取操作人信息。
/// </summary>
public interface ICurrentUser
{
    /// <summary>是否已通过身份认证</summary>
    bool IsAuthenticated { get; }

    /// <summary>用户 ID</summary>
    long? UserId { get; }

    /// <summary>登录账号</summary>
    string? UserName { get; }

    /// <summary>用户昵称</summary>
    string? NickName { get; }

    /// <summary>角色编码集合</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>权限编码集合</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>判断是否拥有指定权限</summary>
    bool HasPermission(string code);
}
