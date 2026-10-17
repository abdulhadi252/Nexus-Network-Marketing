namespace Nexos_Network_System.Interfaces;

/// <summary>
/// A single outbound email: one reply going back to whoever filled in the
/// public contact form.
/// </summary>
/// <param name="To">Recipient address.</param>
/// <param name="ToName">Recipient display name, when we know it.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="Body">Plain-text body.</param>
public record OutboundEmail(string To, string? ToName, string Subject, string Body);

/// <summary>
/// Raised when an email cannot be handed to the provider — missing API key,
/// rejected request or a transport failure. Callers surface the message to the
/// admin instead of failing the whole request.
/// </summary>
public class EmailSendException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// The app's only outbound mail need: sending the admin's reply back to a
/// contact message sender.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default);
}
