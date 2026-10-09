using System.Data;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Services;

/// <summary>
/// The single place where bills are calculated. Service tax is 12.24 percent of the current
/// charges, all money is decimal and rounding uses midpoint away from zero.
/// </summary>
public class BillingService(ApplicationDbContext db) : IBillingService
{
    public async Task<Bill> GenerateBillAsync(BillGenerateViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.BillingPeriodEnd < model.BillingPeriodStart)
        {
            throw new InvalidOperationException("The billing period end date must not be before the start date.");
        }

        if (model.DiscountAmount < 0 || model.SchemeDiscountAmount < 0 ||
            model.EquipmentCharges < 0 || model.ReplacementCharges < 0)
        {
            throw new InvalidOperationException("Bill charges and discounts cannot be negative.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var connection = await db.Connections
            .Include(c => c.Plan)
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == model.ConnectionId, cancellationToken)
            ?? throw new InvalidOperationException("Connection not found.");

        var plan = connection.Plan;

        var previousBill = await db.Bills
            .Where(b => b.ConnectionId == connection.Id && b.Status != BillStatus.Cancelled)
            .OrderByDescending(b => b.BillingPeriodEnd)
            .ThenByDescending(b => b.GeneratedAt)
            .ThenByDescending(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousBill is not null && model.BillingPeriodStart <= previousBill.BillingPeriodEnd)
        {
            throw new InvalidOperationException("The new billing period must start after the latest bill period.");
        }

        var previousBalance = previousBill?.DueAmount ?? 0m;

        var monthlyRental = plan.RecurringCharge;
        var equipmentCharges = model.EquipmentCharges;
        var replacementCharges = model.ReplacementCharges;
        var discountAmount = model.DiscountAmount;
        var schemeDiscountAmount = model.SchemeDiscountAmount;

        var subtotal = monthlyRental + equipmentCharges + replacementCharges;
        var discounts = discountAmount + schemeDiscountAmount;
        var taxable = subtotal - discounts;
        if (taxable < 0)
        {
            taxable = 0;
        }

        var serviceTaxAmount = Math.Round(taxable * IBillingService.ServiceTaxRate, 2, MidpointRounding.AwayFromZero);
        var totalAmount = taxable + serviceTaxAmount;
        var dueAmount = totalAmount + previousBalance;

        var billNumber = $"B-{connection.Id}-{model.BillingPeriodStart:yyyyMMdd}-{model.BillingPeriodEnd:yyyyMMdd}";

        var bill = new Bill
        {
            BillNumber = billNumber,
            Status = BillStatus.Unpaid,
            CustomerId = connection.CustomerId,
            ConnectionId = connection.Id,
            BillingPeriodStart = model.BillingPeriodStart,
            BillingPeriodEnd = model.BillingPeriodEnd,
            DueDate = model.BillingPeriodEnd.AddDays(14),
            PreviousBalance = previousBalance,
            PreviousPaidAmount = 0m,
            SecurityDeposit = plan.SecurityDeposit,
            MonthlyRental = monthlyRental,
            EquipmentCharges = equipmentCharges,
            ReplacementCharges = replacementCharges,
            DiscountAmount = discountAmount,
            SchemeDiscountAmount = schemeDiscountAmount,
            ServiceTaxRate = IBillingService.ServiceTaxRate,
            ServiceTaxAmount = serviceTaxAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            DueAmount = dueAmount,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        void AddItem(string description, decimal amount)
        {
            bill.Items.Add(new BillItem { Description = description, Amount = amount, IsTaxable = amount > 0 });
        }

        AddItem($"Plan charge - {plan.Name} ({plan.BillingCycle})", monthlyRental);
        if (equipmentCharges > 0) AddItem("Equipment charges", equipmentCharges);
        if (replacementCharges > 0) AddItem("Equipment replacement charges", replacementCharges);
        if (discountAmount > 0) AddItem("Discount", -discountAmount);
        if (schemeDiscountAmount > 0) AddItem("Scheme discount", -schemeDiscountAmount);
        AddItem($"Service tax (12.24%)", serviceTaxAmount);
        if (previousBalance > 0) AddItem("Previous balance carried forward", previousBalance);

        db.Bills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return bill;
    }
}
