namespace Nexos_Network_System.Interfaces;

/// <summary>
/// A single outbound email: a reply to whoever filled in the public contact
/// form, or the "order received" confirmation sent from an order's details page.
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
/// Outbound mail: the admin's reply to a contact message sender, and the
/// order-received notification sent to a customer.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default);
}
