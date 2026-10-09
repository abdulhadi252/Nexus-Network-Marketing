namespace Nexos_Network_System.Models;

public class Feedback
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? ConnectionId { get; set; }
    public Connection? Connection { get; set; }
    public int? Rating { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsReviewed { get; set; }
}
