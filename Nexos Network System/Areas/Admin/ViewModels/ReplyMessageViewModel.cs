using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.Areas.Admin.ViewModels;

/// <summary>
/// Backs the admin reply composer: the original contact message shown next to
/// the reply that is emailed back to its sender.
/// </summary>
public class ReplyMessageViewModel
{
    public int MessageId { get; set; }

    /// <summary>Name the message was submitted with.</summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>Address the reply is sent to. Read-only on the page.</summary>
    public string SenderEmail { get; set; } = string.Empty;

    public string OriginalSubject { get; set; } = string.Empty;

    public string OriginalMessage { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; }

    [Required(ErrorMessage = "Enter a subject for the reply.")]
    [StringLength(200, ErrorMessage = "Keep the subject under 200 characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Write the reply before sending it.")]
    [StringLength(5000, MinimumLength = 2, ErrorMessage = "The reply must be between 2 and 5000 characters.")]
    [Display(Name = "Reply")]
    public string Body { get; set; } = string.Empty;
}
