namespace Almny.Api.Configurations;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public WindowOptions Global { get; set; } = new();
    public WindowOptions Authentication { get; set; } = new();
    public WindowOptions Api { get; set; } = new();
}

public class WindowOptions
{
    public int PermitLimit { get; set; }
    public int Window { get; set; }
    public int QueueLimit { get; set; }
}
