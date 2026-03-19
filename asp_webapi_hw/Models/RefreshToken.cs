namespace asp_webapi_hw.Models;

public class RefreshToken
{
    public string Token     { get; set; } = string.Empty;
    public int    UserId    { get; set; }
    public DateTime ExpiresAt  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public bool   IsRevoked { get; set; } = false;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;
}
