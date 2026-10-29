namespace Wms.Core.Utils;

/// <summary>
/// 单据编号生成器：前缀 + 时间戳 + 随机数，保证单机环境下唯一且可读。
/// </summary>
public static class OrderNoGenerator
{
    /// <summary>生成单据号</summary>
    /// <param name="prefix">业务前缀，如 IN（入库）、OUT（出库）、TK（盘点）、TR（调拨）</param>
    public static string Generate(string prefix)
        => $"{prefix}{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 10000)}";

    /// <summary>生成库存流水号</summary>
    public static string GenerateTransactionNo() => Generate("IT");
}
