namespace Wms.Core.Common;

/// <summary>业务异常状态码定义</summary>
public static class ErrorCodes
{
    /// <summary>请求参数不合法</summary>
    public const int BadRequest = 4000;

    /// <summary>未登录或凭证失效</summary>
    public const int Unauthorized = 4010;

    /// <summary>无访问权限</summary>
    public const int Forbidden = 4030;

    /// <summary>资源不存在</summary>
    public const int NotFound = 4040;

    /// <summary>数据冲突（如编码重复）</summary>
    public const int Conflict = 4090;

    /// <summary>库存不足</summary>
    public const int InventoryInsufficient = 4201;

    /// <summary>库存已被锁定，无法调整</summary>
    public const int InventoryLocked = 4202;

    /// <summary>单据状态不允许当前操作</summary>
    public const int InvalidStatus = 4301;

    /// <summary>服务端内部错误</summary>
    public const int ServerError = 5000;
}

/// <summary>
/// 业务异常：用于表达可预期的业务规则校验失败，由全局异常中间件转换为统一响应。
/// </summary>
public class BusinessException : Exception
{
    /// <summary>业务状态码</summary>
    public int Code { get; }

    public BusinessException(string message, int code = ErrorCodes.BadRequest)
        : base(message)
    {
        Code = code;
    }
}

/// <summary>资源未找到异常</summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message, ErrorCodes.NotFound) { }
}

/// <summary>数据冲突异常（例如编码重复）</summary>
public class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message, ErrorCodes.Conflict) { }
}
