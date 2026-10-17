using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nexos_Network_System.Interfaces;

namespace Nexos_Network_System.Services;

/// <summary>
/// Sends mail through Resend's HTTP API (https://resend.com/docs/api-reference).
/// Only one message is ever needed here, so plain HttpClient is enough and the
/// project takes on no extra package.
///
/// Configuration lives under "Email" so nothing secret is committed:
///   Email:ApiKey     - required. Env var RESEND_API_KEY also works.
///   Email:From       - verified sender address (defaults to Resend's test sender).
///   Email:FromName   - display name shown to the recipient.
///   Email:ApiBaseUrl - optional override, defaults to https://api.resend.com.
/// </summary>
public class ResendEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public const string DefaultApiBaseUrl = "https://api.resend.com";

    // Resend's shared test sender: delivers to the account owner without a
    // verified domain. Replace it with a domain address for real mail.
    public const string DefaultFromAddress = "onboarding@resend.dev";

    public async Task SendAsync(OutboundEmail email, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Email:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new EmailSendException(
                "Email sending is not configured. Set Email__ApiKey (or RESEND_API_KEY) and restart the app.");
        }

        var fromAddress = configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            fromAddress = DefaultFromAddress;
        }

        var fromName = configuration["Email:FromName"];
        var from = string.IsNullOrWhiteSpace(fromName) ? fromAddress : $"{fromName} <{fromAddress}>";

        var to = string.IsNullOrWhiteSpace(email.ToName) ? email.To : $"{email.ToName} <{email.To}>";

        var baseUrl = configuration["Email:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultApiBaseUrl;
        }

        var payload = new ResendMessage(from, [to], email.Subject, email.Body);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach the email provider while replying to {Recipient}", email.To);
            throw new EmailSendException("The email provider could not be reached. Check the connection and try again.", ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "The email provider rejected the reply to {Recipient} with {StatusCode}: {Detail}",
                email.To, (int)response.StatusCode, detail);

            throw new EmailSendException(
                $"The email provider rejected the message ({(int)response.StatusCode} {response.ReasonPhrase}).");
        }
    }

    private record ResendMessage(string from, string[] to, string subject, string text);
}
