using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Data;
using Nexos_Network_System.Interfaces;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Services;

public class AdminSearchService(ApplicationDbContext db) : IAdminSearchService
{
    public async Task<SearchViewModel> SearchAsync(SearchViewModel model, CancellationToken cancellationToken = default)
    {
        model.HasSearched = true;

        var orderQuery = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Plan)
            .Include(o => o.Connection)
            .AsNoTracking()
            .AsQueryable();

        var connectionQuery = db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Plan)
            .Include(c => c.City)
            .AsNoTracking()
            .AsQueryable();

        var customerQuery = db.Customers.AsNoTracking().AsQueryable();

        // Free-text term from the panel topbar search: one box that covers identifiers and
        // people in a single pass, combined with (never overriding) the explicit filters below.
        if (!string.IsNullOrWhiteSpace(model.Query))
        {
            var term = model.Query.Trim();

            orderQuery = orderQuery.Where(o =>
                o.OrderNumber.Contains(term) ||
                (o.Customer.FirstName + " " + o.Customer.LastName).Contains(term) ||
                o.Customer.PrimaryContactNumber.Contains(term) ||
                (o.Connection != null && o.Connection.AccountNumber.Contains(term)) ||
                (o.Plan != null && o.Plan.Name.Contains(term)));

            connectionQuery = connectionQuery.Where(c =>
                c.AccountNumber.Contains(term) ||
                (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term) ||
                c.Customer.PrimaryContactNumber.Contains(term));

            customerQuery = customerQuery.Where(c =>
                (c.FirstName + " " + c.LastName).Contains(term) ||
                c.PrimaryContactNumber.Contains(term) ||
                c.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(model.OrderNumber))
        {
            var term = model.OrderNumber.Trim();
            orderQuery = orderQuery.Where(o => o.OrderNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(model.AccountNumber))
        {
            var term = model.AccountNumber.Trim();
            connectionQuery = connectionQuery.Where(c => c.AccountNumber.Contains(term));
            orderQuery = orderQuery.Where(o => o.Connection != null && o.Connection.AccountNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(model.CustomerName))
        {
            var term = model.CustomerName.Trim();
            customerQuery = customerQuery.Where(c => (c.FirstName + " " + c.LastName).Contains(term));
            orderQuery = orderQuery.Where(o => (o.Customer.FirstName + " " + o.Customer.LastName).Contains(term));
            connectionQuery = connectionQuery.Where(c => (c.Customer.FirstName + " " + c.Customer.LastName).Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(model.ContactNumber))
        {
            var term = model.ContactNumber.Trim();
            customerQuery = customerQuery.Where(c => c.PrimaryContactNumber.Contains(term) || (c.AlternateContactNumber != null && c.AlternateContactNumber.Contains(term)));
            orderQuery = orderQuery.Where(o => o.Customer.PrimaryContactNumber.Contains(term));
            connectionQuery = connectionQuery.Where(c => c.Customer.PrimaryContactNumber.Contains(term));
        }

        if (model.ConnectionType.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.ConnectionType == model.ConnectionType.Value);
            connectionQuery = connectionQuery.Where(c => c.ConnectionType == model.ConnectionType.Value);
        }

        if (model.OrderStatus.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.Status == model.OrderStatus.Value);
        }

        if (model.ConnectionStatus.HasValue)
        {
            connectionQuery = connectionQuery.Where(c => c.Status == model.ConnectionStatus.Value);
        }

        if (model.From.HasValue)
        {
            var from = model.From.Value;
            orderQuery = orderQuery.Where(o => o.SubmittedAt >= from);
        }

        if (model.To.HasValue)
        {
            var to = model.To.Value.AddDays(1);
            orderQuery = orderQuery.Where(o => o.SubmittedAt < to);
        }

        model.Orders = await orderQuery
            .OrderByDescending(o => o.SubmittedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        model.Connections = await connectionQuery
            .OrderByDescending(c => c.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        model.Customers = await customerQuery
            .OrderBy(c => c.LastName)
            .Take(50)
            .ToListAsync(cancellationToken);

        return model;
    }
}
