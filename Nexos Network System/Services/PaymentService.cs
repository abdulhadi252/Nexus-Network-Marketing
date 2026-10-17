using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Helpers;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Services;

public class PaymentService(ApplicationDbContext db) : IPaymentService
{
    /// <summary>Methods a customer can use from their own panel (cash is recorded at a branch).</summary>
    private static readonly PaymentMethod[] CustomerMethods =
    {
        PaymentMethod.Card,
        PaymentMethod.BankTransfer,
        PaymentMethod.MobileWallet,
        PaymentMethod.Cheque
    };

    private static readonly PaymentMethod[] ReferenceRequiredMethods =
    {
        PaymentMethod.Card,
        PaymentMethod.BankTransfer,
        PaymentMethod.MobileWallet,
        PaymentMethod.Cheque
    };

    public async Task<Payment> RecordPaymentAsync(PaymentRecordViewModel model, int? recordedByEmployeeId, CancellationToken cancellationToken = default)
    {
        if (model.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        if (model.Amount > 9999999)
        {
            throw new InvalidOperationException("Payment amount cannot exceed 9,999,999.");
        }

        if (!Enum.IsDefined(model.Method))
        {
            throw new InvalidOperationException("Choose a valid payment method.");
        }

        if (model.PaymentReference?.Length > 150 || model.Notes?.Length > 1000)
        {
            throw new InvalidOperationException("Payment reference or notes exceed the allowed length.");
        }

        var paymentReference = string.IsNullOrWhiteSpace(model.PaymentReference)
            ? null
            : model.PaymentReference.Trim();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (paymentReference is not null &&
            await db.Payments.AnyAsync(p => p.PaymentReference == paymentReference, cancellationToken))
        {
            throw new InvalidOperationException("This payment reference has already been recorded.");
        }

        var bill = await LoadPayableBillAsync(model.BillId, cancellationToken);

        if (await db.Payments.AnyAsync(
                p => p.BillId == bill.Id && p.Status == PaymentStatus.PendingVerification,
                cancellationToken))
        {
            throw new InvalidOperationException("This bill has an online payment awaiting verification. Confirm or reject that payment first.");
        }

        if (model.Amount > bill.DueAmount)
        {
            throw new InvalidOperationException($"Amount exceeds the outstanding due ({bill.DueAmount:0.00}).");
        }

        var payment = new Payment
        {
            ReceiptNumber = $"RCP-{Guid.NewGuid():N}",
            BillId = bill.Id,
            CustomerId = bill.CustomerId,
            ConnectionId = bill.ConnectionId,
            Amount = model.Amount,
            PaidAt = DateTimeOffset.UtcNow,
            Method = model.Method,
            Status = PaymentStatus.Recorded,
            PaymentReference = paymentReference,
            RecordedByEmployeeId = recordedByEmployeeId,
            Notes = model.Notes
        };

        ApplyToBill(bill, payment.Amount);

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> SubmitOnlinePaymentAsync(CustomerPaymentViewModel model, int customerId, CancellationToken cancellationToken = default)
    {
        if (!CustomerMethods.Contains(model.Method))
        {
            throw new InvalidOperationException("Choose a billing method available in the customer panel.");
        }

        if (model.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        if (model.Amount > 9999999)
        {
            throw new InvalidOperationException("Payment amount cannot exceed 9,999,999.");
        }

        var reference = string.IsNullOrWhiteSpace(model.PaymentReference) ? null : model.PaymentReference.Trim();
        if (ReferenceRequiredMethods.Contains(model.Method) && reference is null)
        {
            throw new InvalidOperationException("Enter the transaction reference (card, wallet or bank transfer number) so accounts can verify the payment.");
        }

        if (reference?.Length > 150 || model.Notes?.Length > 500)
        {
            throw new InvalidOperationException("Payment reference or notes exceed the allowed length.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var bill = await LoadPayableBillAsync(model.BillId, cancellationToken);
        if (bill.CustomerId != customerId)
        {
            throw new InvalidOperationException("That bill does not belong to your account.");
        }

        if (await db.Payments.AnyAsync(
                p => p.BillId == bill.Id && p.Status == PaymentStatus.PendingVerification,
                cancellationToken))
        {
            throw new InvalidOperationException("A payment for this bill is already awaiting accounts verification. Wait for it to be confirmed before paying again.");
        }

        if (reference is not null &&
            await db.Payments.AnyAsync(p => p.PaymentReference == reference, cancellationToken))
        {
            throw new InvalidOperationException("This transaction reference has already been submitted.");
        }

        if (model.Amount > bill.DueAmount)
        {
            throw new InvalidOperationException($"Amount exceeds the outstanding due ({bill.DueAmount:0.00}).");
        }

        var payment = new Payment
        {
            ReceiptNumber = $"PAY-{Guid.NewGuid():N}",
            BillId = bill.Id,
            CustomerId = bill.CustomerId,
            ConnectionId = bill.ConnectionId,
            Amount = model.Amount,
            PaidAt = DateTimeOffset.UtcNow,
            Method = model.Method,
            Status = PaymentStatus.PendingVerification,
            PaymentReference = reference,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? "Submitted from the customer panel." : model.Notes.Trim()
        };

        // The bill is intentionally left untouched: accounts must confirm the money first.
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> VerifySubmittedPaymentAsync(int paymentId, int employeeId, string? notes, CancellationToken cancellationToken = default)
    {
        if (notes?.Length > 1000)
        {
            throw new InvalidOperationException("Verification notes cannot exceed 1,000 characters.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var payment = await db.Payments
            .Include(p => p.Bill)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        if (payment.Status != PaymentStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only a payment awaiting verification can be confirmed.");
        }

        var bill = await LoadPayableBillAsync(payment.BillId, cancellationToken);
        if (payment.Amount > bill.DueAmount)
        {
            throw new InvalidOperationException($"The outstanding due is now PKR {bill.DueAmount:N2}, which is less than the submitted PKR {payment.Amount:N2}. Reject this payment instead.");
        }

        ApplyToBill(bill, payment.Amount);

        payment.Status = PaymentStatus.Recorded;
        payment.ReceiptNumber = $"RCP-{Guid.NewGuid():N}";
        payment.RecordedByEmployeeId = employeeId;
        payment.Notes = AppendNote(payment.Notes, notes is null ? "Confirmed by accounts." : $"Confirmed by accounts. {notes.Trim()}");

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> RejectSubmittedPaymentAsync(int paymentId, int employeeId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        if (reason.Length > 1000)
        {
            throw new InvalidOperationException("The rejection reason cannot exceed 1,000 characters.");
        }

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        if (payment.Status != PaymentStatus.PendingVerification)
        {
            throw new InvalidOperationException("Only a payment awaiting verification can be rejected.");
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.RecordedByEmployeeId = employeeId;
        payment.Notes = AppendNote(payment.Notes, $"Rejected by accounts: {reason.Trim()}");

        await db.SaveChangesAsync(cancellationToken);
        return payment;
    }

    private async Task<Bill> LoadPayableBillAsync(int billId, CancellationToken cancellationToken)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new InvalidOperationException("Bill not found.");

        if (bill.Status == BillStatus.Cancelled)
        {
            throw new InvalidOperationException("This bill is cancelled and cannot receive payments.");
        }

        if (!await db.Bills.LatestBillPerConnection()
                .AnyAsync(candidate => candidate.Id == bill.Id, cancellationToken))
        {
            throw new InvalidOperationException("Payments can only be made against the latest bill for a connection.");
        }

        if (bill.DueAmount <= 0)
        {
            throw new InvalidOperationException("This bill has no outstanding due amount.");
        }

        return bill;
    }

    private static void ApplyToBill(Bill bill, decimal amount)
    {
        bill.PaidAmount += amount;
        bill.DueAmount = bill.TotalAmount + (bill.PreviousBalance - bill.PreviousPaidAmount) - bill.PaidAmount;
        bill.Status = bill.DueAmount <= 0
            ? BillStatus.Paid
            : bill.PaidAmount > 0 ? BillStatus.PartiallyPaid : BillStatus.Unpaid;
    }

    private static string AppendNote(string? current, string addition) =>
        string.IsNullOrWhiteSpace(current) ? addition : $"{current} {addition}";
}
