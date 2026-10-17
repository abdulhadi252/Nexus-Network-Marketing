using System.Globalization;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Helpers;

public static class PaymentUi
{
    public static string StatusKind(PaymentStatus status) => status switch
    {
        PaymentStatus.Recorded => "success",
        PaymentStatus.Reversed => "refunded",
        PaymentStatus.Cancelled => "failed",
        _ => "pending"
    };

    public static string StatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Recorded => "Successful",
        PaymentStatus.Reversed => "Refunded",
        PaymentStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string StatusIcon(string kind) => kind switch
    {
        "success" => "pi-check",
        "pending" => "pi-clock",
        "refunded" => "pi-refresh",
        "failed" => "pi-undo",
        _ => "pi-info"
    };

    public static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Card => "Card",
        PaymentMethod.BankTransfer => "Bank Transfer",
        PaymentMethod.Cheque => "Cheque",
        PaymentMethod.Other => "Other",
        _ => method.ToString()
    };

    public static string MethodIcon(PaymentMethod method) => method switch
    {
        PaymentMethod.Card => "pi-card",
        PaymentMethod.BankTransfer => "pi-bank",
        PaymentMethod.Cheque => "pi-cheque",
        PaymentMethod.Other => "pi-wallet",
        _ => "pi-wallet"
    };

    public static string AvatarTone(int id) => (id % 5).ToString();

    public static string Initials(Customer customer)
    {
        var parts = $"{customer.FirstName} {customer.LastName}".Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = parts.Length > 0 ? parts[0][..1] : "";
        var last = parts.Length > 1 ? parts[1][..1] : "";
        var initials = (first + last).ToUpperInvariant();
        return initials.Length == 0 ? "?" : initials;
    }
}
