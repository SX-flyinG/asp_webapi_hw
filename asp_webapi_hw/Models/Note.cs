namespace asp_webapi_hw.Models;

public class Note
{
    public int      Id        { get; set; }
    public string   Title     { get; set; } = string.Empty;
    public string   Content   { get; set; } = string.Empty;
    public int      UserId    { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
