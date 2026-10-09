using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Models;

namespace Nexos_Network_System.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<ConnectionPlanHistory> ConnectionPlanHistories => Set<ConnectionPlanHistory>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<FeasibilityReport> FeasibilityReports => Set<FeasibilityReport>();
    public DbSet<Feedback> FeedbackEntries => Set<Feedback>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RetailShop> RetailShops => Set<RetailShop>();
    public DbSet<Stock> StockItems => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Vendor> Vendors => Set<Vendor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCityAndPeople(modelBuilder);
        ConfigureCatalogAndPlans(modelBuilder);
        ConfigureOrdersAndConnections(modelBuilder);
        ConfigureBilling(modelBuilder);
        ConfigureSupport(modelBuilder);
    }

    private static void ConfigureCityAndPeople(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(3).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RetailShop>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(250);
            entity.Property(x => x.ContactNumber).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.City).WithMany(x => x.RetailShops).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(x => x.EmployeeNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.EmployeeNumber).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.RetailShop).WithMany(x => x.Employees).HasForeignKey(x => x.RetailShopId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NationalId).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PrimaryContactNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.AlternateContactNumber).HasMaxLength(30);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(250);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.HasOne(x => x.Customer).WithMany(x => x.Addresses).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.City).WithMany(x => x.CustomerAddresses).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.CustomerId, x.IsPrimary });
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.CustomerId);
        });
    }

    private static void ConfigureCatalogAndPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ContactNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(30).IsRequired();
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Property(x => x.UnitSalePrice).HasPrecision(18, 2);
            entity.HasOne(x => x.Vendor).WithMany(x => x.Products).HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(x => x.ProductId).IsUnique();
            entity.HasOne(x => x.Product).WithOne(x => x.Stock).HasForeignKey<Stock>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.HasOne(x => x.Stock).WithMany(x => x.Movements).HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PerformedByEmployee).WithMany().HasForeignKey(x => x.PerformedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.StockId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.Property(x => x.AssetTag).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(100);
            entity.Property(x => x.ReplacementCharge).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => x.AssetTag).IsUnique();
            entity.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("[SerialNumber] IS NOT NULL");
            entity.HasOne(x => x.Product).WithMany(x => x.EquipmentItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany(x => x.EquipmentItems).HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.RecurringCharge).HasPrecision(18, 2);
            entity.Property(x => x.SecurityDeposit).HasPrecision(18, 2);
            entity.Property(x => x.InstallationCharge).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.Name, x.ConnectionType }).IsUnique();
        });
    }

    private static void ConfigureOrdersAndConnections(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(x => x.OrderNumber).HasMaxLength(11).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(2000);
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => new { x.ConnectionType, x.SequenceNumber }).IsUnique();
            entity.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RetailShop).WithMany(x => x.Orders).HasForeignKey(x => x.RetailShopId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ProcessedByEmployee).WithMany(x => x.ProcessedOrders).HasForeignKey(x => x.ProcessedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Plan).WithMany(x => x.Orders).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeasibilityReport>(entity =>
        {
            entity.Property(x => x.DistanceMeters).HasPrecision(10, 2);
            entity.Property(x => x.Remarks).HasMaxLength(2000);
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasOne(x => x.Order).WithOne(x => x.FeasibilityReport).HasForeignKey<FeasibilityReport>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TechnicalEmployee).WithMany(x => x.FeasibilityReports).HasForeignKey(x => x.TechnicalEmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Connection>(entity =>
        {
            entity.Property(x => x.AccountNumber).HasMaxLength(16).IsRequired();
            entity.Property(x => x.InstallationNotes).HasMaxLength(2000);
            entity.HasIndex(x => x.AccountNumber).IsUnique();
            entity.HasIndex(x => new { x.ConnectionType, x.CityId, x.AccountSerialNumber }).IsUnique();
            entity.HasOne(x => x.Customer).WithMany(x => x.Connections).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.City).WithMany(x => x.Connections).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Plan).WithMany(x => x.Connections).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Order).WithOne(x => x.Connection).HasForeignKey<Connection>(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConnectionPlanHistory>(entity =>
        {
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.HasOne(x => x.Connection).WithMany(x => x.PlanHistory).HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Plan).WithMany(x => x.ConnectionPlanHistories).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Equipment>()
            .HasOne(x => x.Connection).WithMany(x => x.EquipmentItems).HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureBilling(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(x => x.BillNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.BillNumber).IsUnique();
            entity.HasIndex(x => new { x.ConnectionId, x.BillingPeriodStart, x.BillingPeriodEnd }).IsUnique();
            foreach (var property in new[]
            {
                nameof(Bill.PreviousBalance), nameof(Bill.PreviousPaidAmount), nameof(Bill.SecurityDeposit), nameof(Bill.MonthlyRental),
                nameof(Bill.EquipmentCharges), nameof(Bill.ReplacementCharges), nameof(Bill.DiscountAmount), nameof(Bill.SchemeDiscountAmount),
                nameof(Bill.ServiceTaxRate), nameof(Bill.ServiceTaxAmount), nameof(Bill.TotalAmount), nameof(Bill.PaidAmount), nameof(Bill.DueAmount)
            })
            {
                entity.Property(property).HasPrecision(18, 2);
            }
            entity.HasOne(x => x.Customer).WithMany(x => x.Bills).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Connection).WithMany(x => x.Bills).HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.Bill).WithMany(x => x.Items).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PaymentReference).HasMaxLength(150);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => x.ReceiptNumber).IsUnique();
            entity.HasOne(x => x.Bill).WithMany(x => x.Payments).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany(x => x.Payments).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Connection).WithMany(x => x.Payments).HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RecordedByEmployee).WithMany(x => x.RecordedPayments).HasForeignKey(x => x.RecordedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Feedback_Rating", "[Rating] IS NULL OR [Rating] BETWEEN 1 AND 5"));
            entity.HasOne(x => x.Customer).WithMany(x => x.FeedbackEntries).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Connection).WithMany().HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureSupport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => x.IsRead);
        });
    }
}
