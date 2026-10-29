namespace Wms.Core.Utils;

/// <summary>
/// 日期区间工具：统一处理报表的起止日期（包含结束日当天）。
/// </summary>
public static class DateRange
{
    /// <summary>将日期规整为当天 00:00:00</summary>
    public static DateTime StartOfDay(DateTime date) => date.Date;

    /// <summary>将日期规整为次日 00:00:00（用于构造左闭右开区间）</summary>
    public static DateTime EndOfDayExclusive(DateTime date) => date.Date.AddDays(1);

    /// <summary>解析可选日期字符串，解析失败返回 null</summary>
    public static DateTime? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var result) ? result : null;
    }
}
