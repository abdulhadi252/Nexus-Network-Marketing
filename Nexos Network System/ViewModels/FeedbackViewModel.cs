using System.ComponentModel.DataAnnotations;

namespace Nexos_Network_System.ViewModels;

public class FeedbackViewModel
{
    [Display(Name = "Related Connection")]
    public int? ConnectionId { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int? Rating { get; set; }

    [Required, StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
