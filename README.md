# Nexos Network System

A complete ASP.NET Core MVC (.NET 10) system for managing an internet / telephony service
business: customer registration, service orders, feasibility checks, connections, billing
(with 12.24% service tax), payments, inventory and role based panels.

## Technology

- ASP.NET Core MVC, .NET 10, Razor views
- Entity Framework Core 10 (SQL Server)
- ASP.NET Core Identity (authentication + roles)
- Custom responsive CSS and vanilla JavaScript (`wwwroot/css/site.css`, `wwwroot/js/site.js`)

## Panels & Roles

| Role | Route | Capabilities |
|---|---|---|
| Admin | `/Admin` | Employees, Customers, Retail Shops, Vendors, Products, Stock, Plans, Orders, Connections, Billing, Payments, Messages, Advanced Search |
| Retail | `/Retail` | Register customers, create orders (auto order number), track orders, view connections/bills/payments/plans |
| Technical | `/Technical` | Feasibility reports, create connections (auto account number), installation status, equipment assignment |
| Accounts | `/Accounts` | Generate bills (12.24% service tax), record payments, outstanding dues, customers/connections |
| Customer | `/Customer` | Own dashboard, profile, orders, connection, plan, equipment, bills, payments, feedback (data isolation enforced) |

Public website: Home, About, Services, Plans, Contact (saves messages to DB),
Track Order, Check Connection Status.

## Demo Credentials (development seed)

All demo users share the password `Nexos@123` (change before any real use):

| Email | Role |
|---|---|
| admin@nexos.local | Admin |
| retail@nexos.local | Retail |
| technical@nexos.local | Technical |
| accounts@nexos.local | Accounts |
| customer@nexos.local | Customer (Ali Khan) |

## Run

```bash
cd "Nexos Network System"
dotnet ef database update   # create/update SQL Server (LocalDB in Development)
dotnet run
```

Seed data (cities, shops, employees, users, vendors, products, stock, equipment, plans,
customers, orders, connections, bills, payments, feedback) is applied automatically on
startup via `Data/DbSeeder.cs` (runs migrations first).

## Key Domain Rules

- **Order number**: 11 chars, `D/B/T` + 10 digit sequence (e.g. `B0000000001`),
  generated centrally in `Services/OrderIdService.cs`.
- **Account number**: 16 chars, `D/B/T` + 3 digit city code + 12 digit serial
  (e.g. `B001000000000001`), unique index backed.
- **Billing**: plan charge + equipment/replacement charges - discounts, then 12.24%
  service tax, plus previous outstanding balance. All money is `decimal(18,2)`.
- **Stock**: orders reserve stock; movements (In/Out/Adjustment) are fully audited.
- **Security**: per-role panel authorization, anti-forgery on all forms, customer data
  isolation (a customer can only ever read their own records).

## Project Layout

```
Areas/{Admin,Retail,Technical,Accounts,Customer,Identity}   role panels + auth
Controllers/HomeController.cs                                public website
Data/{ApplicationDbContext,DbSeeder,Migrations}              EF Core + seeding
Services/                                                    business logic (IDs, billing, payments, stock)
Interfaces/                                                  service contracts
ViewModels/                                                  form + dashboard models
Helpers/                                                     pagination, badges, current-user resolution
Views/Shared/                                                shared public layout, panel layout, sidebars
```
