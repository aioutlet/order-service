namespace OrderService.Core.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string Key { get; set; } = string.Empty;
}
