namespace Nexos_Network_System.Models;

public enum ContactMessageStatus
{
    New,
    Pending,
    Replied
}

public class ContactMessage
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Subject { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
}
public static class ContactMessageStatusBadgeColors
{
    public static string GetCssClass(ContactMessageStatus status) => status switch
    {
        ContactMessageStatus.Replied => "status-pill--positive",
        ContactMessageStatus.Pending => "status-pill--warning",
        _ => "status-pill--neutral"
    };
}


