namespace LogPath.Api.Models;

public class PosEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Genera un ID único automático
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty; // INFO, ERROR, WARN, SUCCESS
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}