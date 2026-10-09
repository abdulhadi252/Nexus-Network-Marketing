using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nexos_Network_System.Models;
using Nexos_Network_System.Models.Enums;

namespace Nexos_Network_System.Data;

/// <summary>
/// Development/demo seed data. Demo credentials (all users share the same password, change before any real use):
/// admin@nexos.local / Nexos@123       -> Admin
/// retail@nexos.local / Nexos@123      -> Retail
/// technical@nexos.local / Nexos@123   -> Technical
/// accounts@nexos.local / Nexos@123    -> Accounts
/// customer@nexos.local / Nexos@123    -> Customer (linked to customer Ali Khan)
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Nexos@123";

    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            await SeedRolesAsync(services);
            await SeedCatalogAsync(db);
            await SeedCustomersAsync(db);
            await SeedUsersAsync(services, db);
            await SeedOrdersAndBillingAsync(db);
            await SeedFeedbackAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration or required seed data setup failed.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Retail", "Technical", "Accounts", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create the required '{role}' role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
                }
            }
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db)
    {
        if (!await db.Cities.AnyAsync())
        {
            db.Cities.AddRange(
                new City { Name = "Karachi", Code = "001" },
                new City { Name = "Lahore", Code = "002" },
                new City { Name = "Islamabad", Code = "003" });
            await db.SaveChangesAsync();
        }

        if (!await db.RetailShops.AnyAsync())
        {
            var cities = await db.Cities.ToDictionaryAsync(c => c.Code);
            db.RetailShops.AddRange(
                new RetailShop { Name = "Nexos Karachi Main Branch", AddressLine1 = "12 Shahrah-e-Faisal", ContactNumber = "02134567890", CityId = cities["001"].Id },
                new RetailShop { Name = "Nexos Lahore Gulberg Branch", AddressLine1 = "45 Main Boulevard Gulberg", ContactNumber = "04235678901", CityId = cities["002"].Id },
                new RetailShop { Name = "Nexos Islamabad Blue Area Branch", AddressLine1 = "8 Jinnah Avenue Blue Area", ContactNumber = "05123456789", CityId = cities["003"].Id });
            await db.SaveChangesAsync();
        }

        if (!await db.Employees.AnyAsync())
        {
            var shops = await db.RetailShops.OrderBy(s => s.Id).ToListAsync();
            db.Employees.AddRange(
                new Employee { EmployeeNumber = "EMP-0001", FirstName = "System", LastName = "Administrator", Email = "admin@nexos.local", ContactNumber = "03000000001", Role = EmployeeRole.Admin, HireDate = DateTimeOffset.UtcNow.AddYears(-2) },
                new Employee { EmployeeNumber = "EMP-0002", FirstName = "Hamza", LastName = "Retail", Email = "retail@nexos.local", ContactNumber = "03000000002", Role = EmployeeRole.Retail, RetailShopId = shops[0].Id, HireDate = DateTimeOffset.UtcNow.AddYears(-1) },
                new Employee { EmployeeNumber = "EMP-0003", FirstName = "Usman", LastName = "Technical", Email = "technical@nexos.local", ContactNumber = "03000000003", Role = EmployeeRole.Technical, HireDate = DateTimeOffset.UtcNow.AddYears(-1) },
                new Employee { EmployeeNumber = "EMP-0004", FirstName = "Ayesha", LastName = "Accounts", Email = "accounts@nexos.local", ContactNumber = "03000000004", Role = EmployeeRole.Accounts, HireDate = DateTimeOffset.UtcNow.AddMonths(-8) });
            await db.SaveChangesAsync();
        }

        if (!await db.Vendors.AnyAsync())
        {
            db.Vendors.AddRange(
                new Vendor { Name = "Huawei Technologies", ContactNumber = "02111122233", Email = "sales@huawei-demo.local", Address = "Karachi, Pakistan" },
                new Vendor { Name = "TP-Link Distribution", ContactNumber = "04211133344", Email = "orders@tplink-demo.local", Address = "Lahore, Pakistan" });
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            var vendors = await db.Vendors.OrderBy(v => v.Id).ToListAsync();
            db.Products.AddRange(
                new Product { Name = "ADSL Modem", Description = "Dial-up/ADSL compatible modem", UnitOfMeasure = "Piece", UnitCost = 3200m, UnitSalePrice = 4500m, VendorId = vendors[0].Id },
                new Product { Name = "Fiber ONU", Description = "GPON optical network unit", UnitOfMeasure = "Piece", UnitCost = 5500m, UnitSalePrice = 7200m, VendorId = vendors[0].Id },
                new Product { Name = "Wi-Fi Router", Description = "Dual band wireless router", UnitOfMeasure = "Piece", UnitCost = 2800m, UnitSalePrice = 3800m, VendorId = vendors[1].Id },
                new Product { Name = "Drop Cable", Description = "Outdoor drop cable sold per meter", UnitOfMeasure = "Meter", UnitCost = 35m, UnitSalePrice = 60m, VendorId = vendors[1].Id });
            await db.SaveChangesAsync();

            foreach (var product in await db.Products.ToListAsync())
            {
                db.StockItems.Add(new Stock
                {
                    ProductId = product.Id,
                    QuantityOnHand = product.UnitOfMeasure == "Meter" ? 500 : 25,
                    QuantityReserved = 0,
                    ReorderLevel = product.UnitOfMeasure == "Meter" ? 100 : 5
                });
            }
            await db.SaveChangesAsync();

            var stocks = await db.StockItems.Include(s => s.Product).ToListAsync();
            foreach (var stock in stocks)
            {
                db.StockMovements.Add(new StockMovement
                {
                    StockId = stock.Id,
                    MovementType = StockMovementType.In,
                    QuantityChange = stock.QuantityOnHand,
                    QuantityAfter = stock.QuantityOnHand,
                    Reason = "Opening stock",
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.Equipment.AnyAsync())
        {
            var products = await db.Products.ToDictionaryAsync(p => p.Name);
            var vendors = await db.Vendors.OrderBy(v => v.Id).ToListAsync();

            db.Equipment.AddRange(
                new Equipment { AssetTag = "AST-0001", SerialNumber = "SN-MDM-1001", ProductId = products["ADSL Modem"].Id, VendorId = vendors[0].Id, Status = EquipmentStatus.Available, ReplacementCharge = 4500m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-6) },
                new Equipment { AssetTag = "AST-0002", SerialNumber = "SN-MDM-1002", ProductId = products["ADSL Modem"].Id, VendorId = vendors[0].Id, Status = EquipmentStatus.Available, ReplacementCharge = 4500m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-6) },
                new Equipment { AssetTag = "AST-0003", SerialNumber = "SN-ONU-2001", ProductId = products["Fiber ONU"].Id, VendorId = vendors[0].Id, Status = EquipmentStatus.Available, ReplacementCharge = 7200m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-4) },
                new Equipment { AssetTag = "AST-0004", SerialNumber = "SN-RTR-3001", ProductId = products["Wi-Fi Router"].Id, VendorId = vendors[1].Id, Status = EquipmentStatus.Available, ReplacementCharge = 3800m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-3) },
                new Equipment { AssetTag = "AST-0005", SerialNumber = "SN-RTR-3002", ProductId = products["Wi-Fi Router"].Id, VendorId = vendors[1].Id, Status = EquipmentStatus.Available, ReplacementCharge = 3800m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-3) },
                new Equipment { AssetTag = "AST-0006", SerialNumber = "SN-MDM-1003", ProductId = products["ADSL Modem"].Id, VendorId = vendors[0].Id, Status = EquipmentStatus.UnderRepair, ReplacementCharge = 4500m, PurchasedAt = DateTimeOffset.UtcNow.AddMonths(-8), Notes = "Faulty power adapter, sent for repair." });
            await db.SaveChangesAsync();
        }

        if (!await db.Plans.AnyAsync())
        {
            db.Plans.AddRange(
                new Plan { Name = "Dial-Up Basic", Description = "Entry level dial-up internet with 100 free hours.", ConnectionType = ConnectionType.DialUp, BillingCycle = PlanBillingCycle.Monthly, ValidityDays = 30, IncludedUsageHours = 100, RecurringCharge = 499m, SecurityDeposit = 1000m, InstallationCharge = 500m },
                new Plan { Name = "Broadband Home 10 Mbps", Description = "Unlimited home broadband up to 10 Mbps.", ConnectionType = ConnectionType.Broadband, BillingCycle = PlanBillingCycle.Monthly, ValidityDays = 30, SpeedMbps = 10, RecurringCharge = 1500m, SecurityDeposit = 2000m, InstallationCharge = 1500m },
                new Plan { Name = "Broadband Home 25 Mbps", Description = "Unlimited home broadband up to 25 Mbps.", ConnectionType = ConnectionType.Broadband, BillingCycle = PlanBillingCycle.Monthly, ValidityDays = 30, SpeedMbps = 25, RecurringCharge = 2500m, SecurityDeposit = 2000m, InstallationCharge = 1500m },
                new Plan { Name = "Broadband Business 50 Mbps", Description = "Business grade broadband up to 50 Mbps with priority support.", ConnectionType = ConnectionType.Broadband, BillingCycle = PlanBillingCycle.Monthly, ValidityDays = 30, SpeedMbps = 50, RecurringCharge = 5000m, SecurityDeposit = 5000m, InstallationCharge = 2500m },
                new Plan { Name = "Landline Standard", Description = "Standard telephone line with free local calls.", ConnectionType = ConnectionType.Telephone, BillingCycle = PlanBillingCycle.Monthly, ValidityDays = 30, RecurringCharge = 800m, SecurityDeposit = 1000m, InstallationCharge = 500m });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCustomersAsync(ApplicationDbContext db)
    {
        if (await db.Customers.AnyAsync())
        {
            return;
        }

        var cities = await db.Cities.ToDictionaryAsync(c => c.Code);

        var ali = new Customer { FirstName = "Ali", LastName = "Khan", NationalId = "42101-1234567-1", Email = "ali.khan@example.com", PrimaryContactNumber = "03001234567" };
        var sara = new Customer { FirstName = "Sara", LastName = "Ahmed", NationalId = "35202-7654321-2", Email = "sara.ahmed@example.com", PrimaryContactNumber = "03007654321" };
        var bilal = new Customer { FirstName = "Bilal", LastName = "Hussain", NationalId = "61101-1112223-3", Email = "bilal.hussain@example.com", PrimaryContactNumber = "03331239876" };

        db.Customers.AddRange(ali, sara, bilal);
        await db.SaveChangesAsync();

        db.CustomerAddresses.AddRange(
            new CustomerAddress { CustomerId = ali.Id, CityId = cities["001"].Id, AddressLine1 = "House 14, Block B, Gulshan-e-Iqbal", PostalCode = "75300", IsPrimary = true },
            new CustomerAddress { CustomerId = sara.Id, CityId = cities["002"].Id, AddressLine1 = "Flat 3, Liberty Market", PostalCode = "54660", IsPrimary = true },
            new CustomerAddress { CustomerId = bilal.Id, CityId = cities["003"].Id, AddressLine1 = "House 22, Street 7, F-11 Markaz", PostalCode = "44000", IsPrimary = true });
        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(IServiceProvider services, ApplicationDbContext db)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var firstCustomer = await db.Customers.OrderBy(c => c.Id).FirstAsync();

        var users = new (string Email, string FullName, string Role, int? CustomerId)[]
        {
            ("admin@nexos.local", "System Administrator", "Admin", null),
            ("retail@nexos.local", "Hamza Retail", "Retail", null),
            ("technical@nexos.local", "Usman Technical", "Technical", null),
            ("accounts@nexos.local", "Ayesha Accounts", "Accounts", null),
            ("customer@nexos.local", "Ali Khan", "Customer", firstCustomer.Id)
        };

        foreach (var (email, fullName, role, customerId) in users)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                if (!await userManager.IsInRoleAsync(existing, role))
                {
                    var assignResult = await userManager.AddToRoleAsync(existing, role);
                    if (!assignResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Unable to assign the seeded '{role}' role: {string.Join("; ", assignResult.Errors.Select(error => error.Description))}");
                    }
                }

                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                CustomerId = customerId
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the seeded '{role}' account: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the seeded '{role}' role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task SeedOrdersAndBillingAsync(ApplicationDbContext db)
    {
        if (await db.Orders.AnyAsync())
        {
            return;
        }

        var cities = await db.Cities.ToDictionaryAsync(c => c.Code);
        var shops = await db.RetailShops.OrderBy(s => s.Id).ToListAsync();
        var plans = await db.Plans.ToDictionaryAsync(p => p.Name);
        var customers = await db.Customers.OrderBy(c => c.Id).ToListAsync();
        var products = await db.Products.ToDictionaryAsync(p => p.Name);
        var employees = await db.Employees.ToDictionaryAsync(e => e.EmployeeNumber);
        var ali = customers[0];
        var sara = customers[1];
        var bilal = customers[2];

        var order1 = new Order
        {
            OrderNumber = "B0000000001", SequenceNumber = 1, ConnectionType = ConnectionType.Broadband, Status = OrderStatus.ConnectionProvided,
            CustomerId = ali.Id, RetailShopId = shops[0].Id, PlanId = plans["Broadband Home 10 Mbps"].Id, ProcessedByEmployeeId = employees["EMP-0002"].Id,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-25), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-20), Remarks = "New home broadband installation."
        };
        var order2 = new Order
        {
            OrderNumber = "T0000000001", SequenceNumber = 1, ConnectionType = ConnectionType.Telephone, Status = OrderStatus.ConnectionProvided,
            CustomerId = sara.Id, RetailShopId = shops[1].Id, PlanId = plans["Landline Standard"].Id, ProcessedByEmployeeId = employees["EMP-0002"].Id,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-18), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-15), Remarks = "Landline connection request."
        };
        var order3 = new Order
        {
            OrderNumber = "B0000000002", SequenceNumber = 2, ConnectionType = ConnectionType.Broadband, Status = OrderStatus.Feasible,
            CustomerId = bilal.Id, RetailShopId = shops[2].Id, PlanId = plans["Broadband Home 25 Mbps"].Id, ProcessedByEmployeeId = employees["EMP-0002"].Id,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-6), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3), Remarks = "Feasibility approved, awaiting connection creation."
        };
        var order4 = new Order
        {
            OrderNumber = "D0000000001", SequenceNumber = 1, ConnectionType = ConnectionType.DialUp, Status = OrderStatus.Pending,
            CustomerId = bilal.Id, RetailShopId = shops[2].Id, PlanId = plans["Dial-Up Basic"].Id, ProcessedByEmployeeId = employees["EMP-0002"].Id,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2), Remarks = "Backup dial-up line."
        };

        order1.Items.Add(new OrderItem { ProductId = products["ADSL Modem"].Id, Quantity = 1, UnitPrice = products["ADSL Modem"].UnitSalePrice ?? 0m });
        order3.Items.Add(new OrderItem { ProductId = products["Fiber ONU"].Id, Quantity = 1, UnitPrice = products["Fiber ONU"].UnitSalePrice ?? 0m });

        db.Orders.AddRange(order1, order2, order3, order4);
        await db.SaveChangesAsync();

        db.FeasibilityReports.AddRange(
            new FeasibilityReport { OrderId = order1.Id, TechnicalEmployeeId = employees["EMP-0003"].Id, Status = FeasibilityStatus.Feasible, DistanceMeters = 120m, ServerAvailable = true, Remarks = "Feasible, nearest node 120m away.", CheckedAt = DateTimeOffset.UtcNow.AddDays(-22) },
            new FeasibilityReport { OrderId = order2.Id, TechnicalEmployeeId = employees["EMP-0003"].Id, Status = FeasibilityStatus.Feasible, DistanceMeters = 60m, ServerAvailable = true, Remarks = "Existing copper pair available.", CheckedAt = DateTimeOffset.UtcNow.AddDays(-16) },
            new FeasibilityReport { OrderId = order3.Id, TechnicalEmployeeId = employees["EMP-0003"].Id, Status = FeasibilityStatus.Feasible, DistanceMeters = 210m, ServerAvailable = true, Remarks = "Fiber available in the street.", CheckedAt = DateTimeOffset.UtcNow.AddDays(-3) });
        await db.SaveChangesAsync();

        var connection1 = new Connection
        {
            AccountNumber = "B001000000000001", AccountSerialNumber = 1, ConnectionType = ConnectionType.Broadband, Status = ConnectionStatus.Active,
            CustomerId = ali.Id, CityId = cities["001"].Id, PlanId = plans["Broadband Home 10 Mbps"].Id, OrderId = order1.Id,
            InstalledAt = DateTimeOffset.UtcNow.AddDays(-20), ActivatedAt = DateTimeOffset.UtcNow.AddDays(-19), InstallationNotes = "Modem installed and configured."
        };
        var connection2 = new Connection
        {
            AccountNumber = "T002000000000001", AccountSerialNumber = 1, ConnectionType = ConnectionType.Telephone, Status = ConnectionStatus.Active,
            CustomerId = sara.Id, CityId = cities["002"].Id, PlanId = plans["Landline Standard"].Id, OrderId = order2.Id,
            InstalledAt = DateTimeOffset.UtcNow.AddDays(-15), ActivatedAt = DateTimeOffset.UtcNow.AddDays(-14), InstallationNotes = "Line activated after wiring."
        };

        db.Connections.AddRange(connection1, connection2);
        await db.SaveChangesAsync();

        db.ConnectionPlanHistories.AddRange(
            new ConnectionPlanHistory { ConnectionId = connection1.Id, PlanId = connection1.PlanId, EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-19), Reason = "Initial plan assigned with connection." },
            new ConnectionPlanHistory { ConnectionId = connection2.Id, PlanId = connection2.PlanId, EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-14), Reason = "Initial plan assigned with connection." });
        await db.SaveChangesAsync();

        var assignedEquipment = await db.Equipment.FirstAsync(e => e.AssetTag == "AST-0001");
        assignedEquipment.Status = EquipmentStatus.Assigned;
        assignedEquipment.ConnectionId = connection1.Id;
        assignedEquipment.AssignedAt = DateTimeOffset.UtcNow.AddDays(-20);
        await db.SaveChangesAsync();

        var periodStart = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var periodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var bill1 = new Bill
        {
            BillNumber = $"BILL-{connection1.AccountNumber}-{periodStart:yyyyMM}", Status = BillStatus.PartiallyPaid,
            CustomerId = ali.Id, ConnectionId = connection1.Id, BillingPeriodStart = periodStart, BillingPeriodEnd = periodEnd,
            DueDate = periodEnd.AddDays(14), PreviousBalance = 0m, PreviousPaidAmount = 0m, SecurityDeposit = 2000m,
            MonthlyRental = 1500m, EquipmentCharges = 0m, ReplacementCharges = 0m, DiscountAmount = 0m, SchemeDiscountAmount = 0m,
            ServiceTaxRate = 0.1224m, ServiceTaxAmount = 183.60m, TotalAmount = 1683.60m, PaidAmount = 1000m, DueAmount = 683.60m,
            GeneratedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };
        bill1.Items.Add(new BillItem { Description = "Plan charge - Broadband Home 10 Mbps (Monthly)", Amount = 1500m, IsTaxable = true });
        bill1.Items.Add(new BillItem { Description = "Service tax (12.24%)", Amount = 183.60m, IsTaxable = true });

        var bill2 = new Bill
        {
            BillNumber = $"BILL-{connection2.AccountNumber}-{periodStart:yyyyMM}", Status = BillStatus.Unpaid,
            CustomerId = sara.Id, ConnectionId = connection2.Id, BillingPeriodStart = periodStart, BillingPeriodEnd = periodEnd,
            DueDate = periodEnd.AddDays(14), PreviousBalance = 0m, PreviousPaidAmount = 0m, SecurityDeposit = 1000m,
            MonthlyRental = 800m, EquipmentCharges = 0m, ReplacementCharges = 0m, DiscountAmount = 0m, SchemeDiscountAmount = 0m,
            ServiceTaxRate = 0.1224m, ServiceTaxAmount = 97.92m, TotalAmount = 897.92m, PaidAmount = 0m, DueAmount = 897.92m,
            GeneratedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };
        bill2.Items.Add(new BillItem { Description = "Plan charge - Landline Standard (Monthly)", Amount = 800m, IsTaxable = true });
        bill2.Items.Add(new BillItem { Description = "Service tax (12.24%)", Amount = 97.92m, IsTaxable = true });

        db.Bills.AddRange(bill1, bill2);
        await db.SaveChangesAsync();

        db.Payments.Add(new Payment
        {
            ReceiptNumber = "RCP-000001", BillId = bill1.Id, CustomerId = ali.Id, ConnectionId = connection1.Id,
            Amount = 1000m, PaidAt = DateTimeOffset.UtcNow.AddDays(-2), Method = PaymentMethod.Cash, Status = PaymentStatus.Recorded,
            RecordedByEmployeeId = employees["EMP-0004"].Id, Notes = "Partial payment received at branch counter."
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedFeedbackAsync(ApplicationDbContext db)
    {
        if (await db.FeedbackEntries.AnyAsync())
        {
            return;
        }

        var customer = await db.Customers.OrderBy(c => c.Id).FirstAsync();
        var connection = await db.Connections.OrderBy(c => c.Id).FirstOrDefaultAsync();

        db.FeedbackEntries.Add(new Feedback
        {
            CustomerId = customer.Id,
            ConnectionId = connection?.Id,
            Rating = 5,
            Message = "Installation was quick and the broadband speed is exactly as promised.",
            IsReviewed = false
        });
        await db.SaveChangesAsync();
    }
}
