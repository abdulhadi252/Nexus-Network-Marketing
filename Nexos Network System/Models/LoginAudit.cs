namespace Nexos_Network_System.Models;

/// <summary>
/// One row per sign-in attempt (successful or failed) for every account type - customers
/// as well as the Admin, Retail, Technical and Accounts panel staff.
///
/// No password material is stored here. Passwords live only as ASP.NET Core Identity
/// hashes in AspNetUsers.PasswordHash.
/// </summary>
public class LoginAudit
{
    public long Id { get; set; }

    /// <summary>Identity user id, or null when the email did not match any account.</summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public required string Email { get; set; }

    public string? FullName { get; set; }

    /// <summary>Comma-separated roles at the time of the attempt, e.g. "Customer" or "Admin".</summary>
    public string? Roles { get; set; }

    /// <summary>Linked customer record for customer accounts.</summary>
    public int? CustomerId { get; set; }

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
}
