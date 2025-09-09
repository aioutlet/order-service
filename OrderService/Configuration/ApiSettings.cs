namespace OrderService.Configuration;

public class ApiSettings
{
    public const string SectionName = "Api";
    
    public string Title { get; set; } = "Order Service API";
    public string Version { get; set; } = "v1";
    public string Description { get; set; } = "RESTful API for managing orders";
}
