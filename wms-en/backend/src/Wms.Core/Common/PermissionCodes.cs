namespace Wms.Core.Common;

/// <summary>
/// 系统权限编码常量。控制器与种子数据统一引用，避免散落的字符串。
/// </summary>
public static class PermissionCodes
{
    /// <summary>通配权限（超级管理员）</summary>
    public const string All = "*";

    // 首页
    public const string DashboardView = "dashboard:view";

    // 基础资料
    public const string MaterialView = "material:view";
    public const string MaterialEdit = "material:edit";
    public const string CategoryView = "category:view";
    public const string CategoryEdit = "category:edit";
    public const string SupplierView = "supplier:view";
    public const string SupplierEdit = "supplier:edit";
    public const string CustomerView = "customer:view";
    public const string CustomerEdit = "customer:edit";

    // 仓库配置
    public const string WarehouseView = "warehouse:view";
    public const string WarehouseEdit = "warehouse:edit";
    public const string LocationView = "location:view";
    public const string LocationEdit = "location:edit";

    // 作业
    public const string InboundView = "inbound:view";
    public const string InboundEdit = "inbound:edit";
    public const string InboundExecute = "inbound:execute";
    public const string OutboundView = "outbound:view";
    public const string OutboundEdit = "outbound:edit";
    public const string OutboundExecute = "outbound:execute";
    public const string StockTakeView = "stocktake:view";
    public const string StockTakeEdit = "stocktake:edit";
    public const string StockTakeExecute = "stocktake:execute";
    public const string TransferView = "transfer:view";
    public const string TransferEdit = "transfer:edit";
    public const string TransferExecute = "transfer:execute";

    // 库存
    public const string InventoryView = "inventory:view";

    // 报表
    public const string ReportView = "report:view";

    // 系统管理
    public const string UserView = "system:user:view";
    public const string UserEdit = "system:user:edit";
    public const string RoleView = "system:role:view";
    public const string RoleEdit = "system:role:edit";
    public const string MenuView = "system:menu:view";
    public const string MenuEdit = "system:menu:edit";
    public const string LogView = "system:log:view";
    public const string PluginView = "system:plugin:view";
    public const string PluginEdit = "system:plugin:edit";
}
