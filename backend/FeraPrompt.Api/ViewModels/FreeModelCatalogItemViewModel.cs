namespace FeraPrompt.Api.ViewModels;

public class FreeModelCatalogItemViewModel
{
    public string Category { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string MaxContext { get; set; } = string.Empty;
    public string Capabilities { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
}
