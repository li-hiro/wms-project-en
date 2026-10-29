namespace Wms.Core.Common;

/// <summary>通用启用/停用状态</summary>
public enum CommonStatus
{
    /// <summary>停用</summary>
    Disabled = 0,

    /// <summary>启用</summary>
    Enabled = 1
}

/// <summary>单据通用状态（入库/出库）</summary>
public enum OrderStatus
{
    /// <summary>草稿</summary>
    Draft = 0,

    /// <summary>已确认（出库时表示库存已锁定）</summary>
    Confirmed = 1,

    /// <summary>已完成（库存已实际变更）</summary>
    Completed = 2,

    /// <summary>已取消</summary>
    Cancelled = 3
}

/// <summary>盘点单状态</summary>
public enum StockTakeStatus
{
    /// <summary>草稿（待盘点）</summary>
    Draft = 0,

    /// <summary>已完成（已调整库存）</summary>
    Completed = 1,

    /// <summary>已取消</summary>
    Cancelled = 2
}

/// <summary>入库类型</summary>
public enum InboundType
{
    /// <summary>采购入库</summary>
    Purchase = 1,

    /// <summary>生产入库</summary>
    Production = 2,

    /// <summary>退货入库</summary>
    Return = 3,

    /// <summary>其他入库</summary>
    Other = 9
}

/// <summary>出库类型</summary>
public enum OutboundType
{
    /// <summary>销售出库</summary>
    Sales = 1,

    /// <summary>生产领料</summary>
    Production = 2,

    /// <summary>其他出库</summary>
    Other = 9
}

/// <summary>库位类型</summary>
public enum LocationType
{
    /// <summary>存储位</summary>
    Storage = 1,

    /// <summary>收货位</summary>
    Receiving = 2,

    /// <summary>发货位</summary>
    Shipping = 3,

    /// <summary>其他</summary>
    Other = 9
}

/// <summary>库存流水类型</summary>
public enum InventoryTransactionType
{
    /// <summary>入库</summary>
    Inbound = 1,

    /// <summary>出库</summary>
    Outbound = 2,

    /// <summary>调拨入</summary>
    TransferIn = 3,

    /// <summary>调拨出</summary>
    TransferOut = 4,

    /// <summary>锁定库存</summary>
    Lock = 5,

    /// <summary>释放锁定</summary>
    Unlock = 6,

    /// <summary>盘点调整</summary>
    Adjust = 7,

    /// <summary>期初导入</summary>
    Init = 8
}

/// <summary>菜单类型</summary>
public enum MenuType
{
    /// <summary>目录</summary>
    Directory = 0,

    /// <summary>菜单</summary>
    Menu = 1
}
