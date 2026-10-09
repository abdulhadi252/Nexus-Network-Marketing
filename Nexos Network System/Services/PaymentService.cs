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

        var bill = await db.Bills.FirstOrDefaultAsync(b => b.Id == model.BillId, cancellationToken)
            ?? throw new InvalidOperationException("Bill not found.");

        if (bill.Status == BillStatus.Cancelled)
        {
            throw new InvalidOperationException("This bill is cancelled and cannot receive payments.");
        }

        if (!await db.Bills.LatestBillPerConnection()
                .AnyAsync(candidate => candidate.Id == bill.Id, cancellationToken))
        {
            throw new InvalidOperationException("Record payments against the latest bill for this connection.");
        }

        if (bill.DueAmount <= 0)
        {
            throw new InvalidOperationException("This bill has no outstanding due amount.");
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

        bill.PaidAmount += model.Amount;
        bill.DueAmount = bill.TotalAmount + (bill.PreviousBalance - bill.PreviousPaidAmount) - bill.PaidAmount;
        bill.Status = bill.DueAmount <= 0
            ? BillStatus.Paid
            : bill.PaidAmount > 0 ? BillStatus.PartiallyPaid : BillStatus.Unpaid;

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }
}
