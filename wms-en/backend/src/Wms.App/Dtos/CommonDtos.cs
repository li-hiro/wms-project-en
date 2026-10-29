namespace Wms.App.Dtos;

/// <summary>下拉选项通用结构</summary>
public class OptionDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Extra { get; set; }
}
