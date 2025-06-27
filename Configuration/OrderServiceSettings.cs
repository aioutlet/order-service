namespace OrderService.Configuration;

public class OrderServiceSettings
{
    public const string SectionName = "OrderService";
    
    public string DefaultCurrency { get; set; } = "USD";
    public decimal TaxRate { get; set; } = 0.08m;
    public decimal FreeShippingThreshold { get; set; } = 100.00m;
    public decimal DefaultShippingCost { get; set; } = 10.00m;
    public string OrderNumberPrefix { get; set; } = "ORD";
}
