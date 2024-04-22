namespace Unity.Services.Cli.CloudContentDelivery.Model;

class SharedRateLimitStatus
{
    public bool IsRateLimited { get; set; }
    public TimeSpan ResetTime { get; set; } = TimeSpan.Zero;

    public void UpdateRateLimit(bool rateLimited, TimeSpan resetTime)
    {
        IsRateLimited = rateLimited;
        ResetTime = resetTime;
    }
}
