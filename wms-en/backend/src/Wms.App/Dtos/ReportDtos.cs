namespace Wms.App.Dtos;

/// <summary>库存汇总报表项</summary>
public class StockSummaryItem
{
    public long MaterialId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal LockedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal AvgCost { get; set; }
    public decimal Amount { get; set; }
    public decimal SafetyStock { get; set; }
    public bool Alert { get; set; }
}

/// <summary>收发存报表项（期初 + 收入 - 发出 = 期末）</summary>
public class InOutSummaryItem
{
    public long MaterialId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal BeginQty { get; set; }
    public decimal InQty { get; set; }
    public decimal OutQty { get; set; }
    public decimal EndQty { get; set; }
}

/// <summary>出入库趋势项</summary>
public class TrendItem
{
    /// <summary>日期（yyyy-MM-dd）</summary>
    public string Date { get; set; } = string.Empty;
    public decimal InboundQty { get; set; }
    public decimal OutboundQty { get; set; }
    public int InboundOrders { get; set; }
    public int OutboundOrders { get; set; }
}

/// <summary>出库 TOP 物料</summary>
public class TopMaterialItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal OutQty { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>库存分布项（用于饼图）</summary>
public class DistributionItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>工作台概览数据</summary>
public class DashboardDto
{
    public int MaterialCount { get; set; }
    public int WarehouseCount { get; set; }
    public int SupplierCount { get; set; }
    public decimal StockQty { get; set; }
    public decimal StockAmount { get; set; }
    public int TodayInboundOrders { get; set; }
    public int TodayOutboundOrders { get; set; }
    public int PendingInbound { get; set; }
    public int PendingOutbound { get; set; }
    public int AlertCount { get; set; }
    public List<TrendItem> Trend { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public List<DistributionItem> CategoryDistribution { get; set; } = new();
}
