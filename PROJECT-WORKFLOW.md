# Nexos Network System — Complete Workflow Guide

> Ye file project ka poora map hai: har panel, har page, kaun kya karta hai, kaun kise kya bhejta
> hai, aur **kis page par kya hota hai / kya nahi hota** — sab detail mein.
>
> Doc **asli code ke against audit** kiya gaya hai (controllers + Razor views + services).
> Jahan koi cheez UI mein dikhti hai lekin kaam nahi karti, wo yahan saaf likha hai.

**Is file mein:**

| # | Section | Kya milega |
|---|---|---|
| 1–3 | Project, run, folder | stack, config, demo logins |
| 4–5 | Roles + common shell | kaun kis panel mein, shared pages/bell/profile |
| 6–7 | Public + Identity | bina login wale pages, login/register |
| 8–12 | **Har panel page-by-page** | Admin (15 menu), Retail, Technical, Accounts, Customer |
| 13–16 | Order workflow | order kahan se aata hai, statuses, handoff, technical detail |
| 17–21 | Billing, payments, notifications, stock, support | rules + kya nahi hai |
| 22–23 | URL cheat sheet + coverage matrix | har page: chalta hai / partial / nahi |
| 24–27 | Rules, limitations, demo, troubleshooting | audit notes |

---

## 1) Project kya hai

Nexos Network System ek **ASP.NET Core MVC (.NET 10)** application hai jo internet / telephony
service business chalati hai:

- Customer registration aur self-service panel
- Service order (naya connection / plan + equipment)
- Retail branch verification
- Technical feasibility check aur installation
- Connection activation
- Billing (12.24% service tax ke saath)
- Payments (online + branch counter) aur accounts verification
- Inventory / stock / equipment
- Contact messages, customer feedback, aur Resend se email (reply + order notification)
- **5 role-based panels:** Admin, Retail, Technical, Accounts, Customer

**Stack:** ASP.NET Core MVC, C# 13, EF Core 10, SQL Server (LocalDB), ASP.NET Identity (roles),
Razor views + shared panel design system, Resend HTTP API (koi extra mail package nahi).

**Important technical facts:**

| Cheez | Value |
|---|---|
| Auth | ASP.NET Identity cookie, 8 ghante (sliding) |
| Password policy | min 8 chars, 1 digit, 1 uppercase, 1 lowercase (symbol zaroori nahi) |
| Lockout | 5 ghalat attempts → 15 minute lock |
| Auth pages cache | signed-in user ke responses `no-store` (back button se purana page nahi) |
| Area fallback | panel ke andar unknown URL → us panel ka dashboard (404 sirf asli missing record par) |
| Paging | har list 10 rows/page (Admin Payments bhi 10) |
| Email | `Services/ResendEmailSender.cs` — sirf 2 emails: contact reply + order received |
| Tests | repo mein koi test project nahi hai |

---

## 2) Project kaise chalayein

### Requirements
- .NET 10 SDK
- SQL Server: **LocalDB** (`(localdb)\MSSQLLocalDB`) — Express / Developer bhi chalega
- Database: `NexosNetworkSystem` (pehli baar app khud migrate + seed karta hai)

### Database connection

Poori configuration **ek** file mein hai: `Nexos Network System/appsettings.json`
(koi `appsettings.Development.json` override nahi hai):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=NexosNetworkSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

SQL Server Express par chalana ho to sirf `Server=` badal do:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=NexosNetworkSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

SQL login (`User Id=...;Password=...`) use karna ho to woh login pehle server par banana hoga,
warna app start par "Login failed" dega.

### Run
```bash
cd "Nexos Network System"
dotnet run
```

| Profile | URL |
|---|---|
| http | http://localhost:5051 |
| https | https://localhost:7135 |

### Start par seeder kya karta hai (`Data/DbSeeder.cs`)

1. Migrations apply (`Database.MigrateAsync`) — agar model mein pending changes hon to
   warning ke saath `EnsureCreated` fallback
2. Roles banata hai: **Admin, Retail, Technical, Accounts, Customer**
3. Demo data: cities, retail shops, plans (6), products (4), stock, equipment (6), customers (13)
4. 5 demo logins (5 employees + customer logins)
5. Sample orders, feasibility reports, connections, bills aur payments

Seeder **idempotent** hai: dobara chalane par duplicate nahi banata. Test karne se data barhta
hai (naye orders/payments), ghatta nahi.

### Demo logins (password sab ka: `Nexos@123`)

| Role | Email | Login ke baad panel |
|---|---|---|
| Admin | `admin@nexos.local` | `/Admin` |
| Retail | `retail@nexos.local` | `/Retail` |
| Technical | `technical@nexos.local` | `/Technical` |
| Accounts | `accounts@nexos.local` | `/Accounts` |
| Customer | `customer@nexos.local` | `/Customer` |

### Login rules (code-enforced)

- Employee roles (Admin/Retail/Technical/Accounts) ke liye **active Employee record** zaroori hai
- Retail ke liye employee ki **active retail shop** bhi zaroori hai
- Customer ke liye login ka **active Customer record** se link hona zaroori hai
- Ghalat email/password par ek hi generic message: *"Invalid email or password."*
- Login ke baad seedha us role ka dashboard (returnUrl ho to wahi)
- Naya customer public site ke **Register** page se banta hai (Customer + login dono)

---

## 3) Folder structure

```
Nexos-Network-Marketing/                 <- git repo root
├── README.md
├── PROJECT-WORKFLOW.md                  <- ye file
├── Nexos Network System.slnx
├── .gitignore
└── Nexos Network System/                <- ASP.NET project
    ├── Program.cs                       <- DI, Identity, routing, seeding
    ├── appsettings.json                 <- connection string (Email block nahi)
    ├── Areas/
    │   ├── Admin/                       <- 15 menu, controllers + views
    │   ├── Retail/                      <- 7 menu
    │   ├── Technical/                   <- 5 menu
    │   ├── Accounts/                    <- 6 menu
    │   ├── Customer/                    <- 9 menu
    │   └── Identity/                    <- Login / Register / Profile / Logout
    ├── Controllers/                     <- public site (Home) + profile base class
    ├── Data/                            <- DbContext, migrations, seeder
    ├── Helpers/                         <- order workflow, paging, badges, charts
    ├── Interfaces/                      <- service contracts
    ├── Models/                          <- entities + enums
    ├── Services/                        <- order, billing, payment, stock, ids, email
    ├── ViewModels/                      <- form/index models
    ├── ViewComponents/                  <- panel bell notifications, panel user
    ├── Views/                           <- public site + shared panel layout
    └── wwwroot/                         <- CSS, JS, icons, images
```

---

## 4) Roles aur unka scope

| Role | Panel URL | Kya kar sakta hai | Data scope |
|---|---|---|---|
| Admin | `/Admin` | Poora system manage (staff, customers, catalog, stock, orders, billing, payments, messages, feedback) | Sab kuch |
| Retail | `/Retail` | Apni shop ke customers/orders verify + walk-in order create | Sirf apni retail shop |
| Technical | `/Technical` | Forwarded orders, feasibility, connections, equipment | Retail-verified orders + sab connections |
| Accounts | `/Accounts` | Bills, payments, dues, online payment verification | Sab billing data (customers/connections read-only) |
| Customer | `/Customer` | Apne orders, connection, bills, payments, feedback | Sirf apna record |

Har area `[Authorize(Roles = "...")]` se protected hai. Scope code se lagta hai:

- Customer: har query `.Where(x => x.CustomerId == customerId)`
- Retail: har query `.Where(x => x.RetailShopId == shopId)`; shop assign na ho to `Forbid()`
- Technical: koi city/shop filter nahi — sab forwarded orders
- Accounts/Admin: poora data

Bina login koi panel page nahi khulta — login page par redirect hota hai.

---

## 5) Har panel ka common shell (shared pages)

Sab panels ek hi layout use karte hain (`Views/Shared/_PanelLayout.cshtml`).

| Cheez | Kya hota hai | Kya nahi hota |
|---|---|---|
| Sidebar | Role ke hisaab se menu (Admin 15, Retail 7, Technical 5, Accounts 6, Customer 9) + My Profile + Logout | Menu ko configure nahi kiya ja sakta |
| Topbar search | Sirf **Admin** mein (customer/order/plan/search page par le jata hai) | Retail/Technical/Accounts/Customer mein topbar search nahi |
| Bell (notifications) | Role ke hisaab se counts (section 19) | Click par sirf list page khulta hai; "mark all read" nahi |
| Alerts | Success (green) / Error (red) TempData messages har page par | Purane messages refresh ke baad gayab (TempData) |
| Breadcrumbs | Har page par (Home/Dashboard → current page) | — |
| Pagination | 10 rows/page, `?page=N` (page out of range ho to last page) | Page size user change nahi kar sakta |

### Profile pages (sab 5 panels mein)

Har panel ka apna `ProfileController` hai jo `Controllers/AccountProfileControllerBase.cs` se
inherit karta hai — pages same views use karte hain:

| Page | URL | Kya hota hai |
|---|---|---|
| My Profile | `/{Panel}/Profile` | Naam, email, phone, role, linked customer/shop, account info |
| Edit Profile | `/{Panel}/Profile/Edit` | Naam / phone / email update (email unique rehta hai) + Identity record update |
| Change Password | `/{Panel}/Profile/ChangePassword` | Purana password + naya password (policy apply) |

`/Identity/Account/Profile` khud koi page render nahi karta — signed-in user ko us ke
`/{Panel}/Profile` par redirect kar deta hai.

**Kya nahi:** profile se apna role change nahi hota, customer apni address edit nahi kar sakta
(address sirf Admin/Retail manage karte hain).

---

## 6) Public website (bina login) — page by page

| Page | Route | Kya hota hai | Kya nahi hota |
|---|---|---|---|
| Home | `/` | Hero, active broadband plans (speed/price ke order mein), services, CTA | Login zaroori nahi; koi form nahi |
| Services | `/Home/Services` | Services list (static marketing content) | Koi data-driven list nahi |
| Plans | `/Home/Plans` | Sab active plans (Broadband/Dial-Up/Telephone), price + cycle | Inactive plans nahi dikhte |
| Plan Details | `/Home/PlanDetails/{id}` | Ek plan ki detail + "order karo" CTA → login/register | Inactive plan par **404**; order yahan submit nahi hota |
| About | `/Home/About` | Company info (static) | — |
| Contact | `/Home/Contact` | Form: name, email, subject, message → DB `ContactMessages` | Email **nahi** jata; sirf Admin inbox mein jama hota hai |
| Track Order | `/Home/TrackOrder` | Order number se status/plan/feasibility/city | Login ke bagair bhi chalta hai; customer ka naam/address nahi dikhata |
| Connection Status | `/Home/ConnectionStatus` | Account number se connection status, plan, city, activation date | Login ke bagair bhi chalta hai |
| Privacy | `/Home/Privacy` | Policy text | — |
| Terms | `/Home/Terms` | Terms text | — |
| Status code page | `/Home/StatusCode?code=...` | 404/403 wagera ke liye friendly error page | — |
| Error page | `/Home/Error` | Unhandled exception par request id ke saath error | Production mein exception detail nahi dikhti |
| Register | `/Identity/Account/Register` | Naya Customer + login (Name, email, contact, address, city, password) | Email duplicate (Identity ya Customer) reject; city zaroori |
| Login | `/Identity/Account/Login` | Sab roles ke liye common login | 5 ghalat attempt par 15 min lock |

**Sanitisation:** contact form aur registration dono `.Trim()` karte hain; validation
`DataAnnotations` se (Required, EmailAddress, StringLength).

---

## 7) Identity — page by page

| Page | Route | Method | Kya hota hai |
|---|---|---|---|
| Login (form) | `/Identity/Account/Login` | GET | Email + password + Remember me |
| Login (submit) | `/Identity/Account/Login` | POST | Employee active + retail shop active + customer active check, phir sign-in |
| Logout | `/Identity/Account/Logout` | POST (form) | Sign out → public Home |
| Register | `/Identity/Account/Register` | GET/POST | Customer + Address + Identity user + "Customer" role ek transaction mein; success par auto sign-in |
| Access Denied | `/Identity/Account/AccessDenied` | GET | 403 page (galat role se koi panel kholne par) |
| Profile | `/Identity/Account/Profile` | GET | Signed-in user ko us ke panel profile par redirect |

**Kya nahi hota:**

- "Forgot password" / password reset email — nahi hai
- Email confirmation link — nahi hai (`EmailConfirmed = true` seedha set hota hai)
- Social login (Google/Facebook) — nahi
- Register page par sirf **Customer** banta hai; employee accounts Admin banata hai

---

## 8) Admin panel — `/Admin` (15 menu)

Admin **order create nahi karta** — orders customer ya retail se aate hain. Admin monitor karta
hai, status badal sakta hai, aur customer ko email bhej sakta hai.

### 8.1 Dashboard — `/Admin/Dashboard`

| Kya hota hai | Detail |
|---|---|
| KPI cards | Total Customers, Active Connections, Pending Orders, Revenue (This Month) — har card par pichle mahine ke against % trend |
| Connection Growth chart | 12 mahine: naye connections + total (running) |
| Service Distribution | Donut: Broadband / Dial-Up / Telephone counts + % |
| Recent Orders | **Saare** orders, newest first (customer, plan, items) — list complete hai, paging nahi |

**Kya nahi:** date range filter nahi, export nahi, chart interactive nahi (server-rendered SVG).

### 8.2 Employees — `/Admin/Employees`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Employees` | Search (naam / employee number / email) + role filter, 10/page |
| Details | `/Admin/Employees/Details/{id}` | Employee info, role, shop, hire date, active |
| Create | `/Admin/Employees/Create` | Employee number (auto next), naam, email, contact, role, retail shop (retail role par laazmi), hire date, active, **initial password** → Identity user + role ek transaction mein |
| Edit | `/Admin/Employees/Edit/{id}` | Sab fields + optional new password (blank = password change nahi) |
| Delete | `/Admin/Employees/Delete/{id}` | Confirm page |

**Rules / kya nahi hota:**

- Admin role **assign nahi ho sakta** employee management se (`"Admin accounts cannot be assigned..."`)
- Employee number aur email **unique** (Identity + Customers ke saath bhi cross-check)
- Retail role ke bagair shop assign nahi ho sakta; shop **active** honi chahiye
- "Delete" = **soft delete** — employee inactive + login lock (history safe)
- Customer account ko employee account mein convert nahi kiya ja sakta

### 8.3 Customers — `/Admin/Customers`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Customers` | Search (naam/email/contact), active/inactive filter, city filter, 10/page |
| Details | `/Admin/Customers/Details/{id}` | Personal + addresses + **Outstanding** summary + us ke orders/connections/bills |
| Create | `/Admin/Customers/Create` | Naam, email (unique), national id, primary/alternate contact, address line 1/2, postal code, city, active |
| Edit | `/Admin/Customers/Edit/{id}` | Wahi fields; email duplicate reject |
| Delete | `/Admin/Customers/Delete/{id}` | Confirm page |

**Kya nahi:** "Delete" customer ko **deactivate** karta hai, delete nahi — orders/connections/
bills/payments preserve rehte hain. Ek customer ke multiple addresses app se add nahi hote
(create/edit par ek primary address).

### 8.4 Retail Shops — `/Admin/RetailShops`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/RetailShops` | Search + 10/page |
| Details | `/Admin/RetailShops/Details/{id}` | Shop info, city, contact, employees/orders summary |
| Create | `/Admin/RetailShops/Create` | Naam, address, contact, city, active |
| Edit | `/Admin/RetailShops/Edit/{id}` | Wahi fields |
| Delete | `/Admin/RetailShops/Delete/{id}` | Confirm |

**Kya nahi:** employees/orders wali shop delete nahi hoti — **deactivate** hoti hai. Naya shop
kisi city mein add ho sakta hai (city list se).

### 8.5 Vendors — `/Admin/Vendors`

List (search), Details, Create/Edit (naam, contact, email, address, active), Delete.
Products/equipment wale vendor delete nahi hote — deactivate hote hain.

### 8.6 Products — `/Admin/Products`

List (search), Details, Create/Edit (naam, description, unit of measure, cost, sale price,
vendor, active), Delete.

- Order items ya equipment se linked product **delete nahi**, deactivate hota hai
- Product ka stock record alag `Stock` table mein hai (section 20)

### 8.7 Stock — `/Admin/Stock`

| Page | Route | Kya hota hai |
|---|---|---|
| Stock list | `/Admin/Stock` | Sab products ka on-hand / reserved / available, low-stock filter, 10/page |
| Adjust | `/Admin/Stock/Adjust/{stockId}` | Movement type (In / Out / Adjustment / Adjustment Decrease), quantity, reason |
| Movements | `/Admin/Stock/Movements/{stockId}` | Us stock ki poori movement history |

**Rules:** out quantity available se zyada nahi; adjustment reserved quantity se neeche nahi;
quantity > 0; reason limit. **Kya nahi:** order complete/installation par stock **auto-deduct
nahi** hota — physical issue Admin manually Adjust karta hai.

### 8.8 Plans — `/Admin/Plans`

List (search), Details, Create/Edit (naam, connection type, billing cycle, recurring charge,
security deposit, installation charge, description, active), Delete.

- Ek connection type ke andar plan naam **unique** hai
- Jis plan par connections/history hain wo **delete nahi**, deactivate hota hai
- Plans list public site (Home/Plans) aur customer/retail order forms dono par feed karti hai

### 8.9 Orders — `/Admin/Orders`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Orders` | Search (order number / customer), status filter, connection type filter, from/to date, 10/page + 4 KPI tiles |
| Details | `/Admin/Orders/Details/{id}` | Customer + address, shop, processed by, retail verification, items, plan, feasibility, connection, remarks, **Workflow History**, status update form, **Notify Customer** card |

**Status update** (`UpdateStatus` POST): status + remarks (max 2000). Transition rules
`OrderWorkflow` + `OrderService` se enforce hote hain (section 14) — galat move par error
message dikhta hai, change nahi hota.

**Notify Customer** (`NotifyCustomer` POST):

- Email **Resend** se jata hai order ke customer ke address par
- Subject: `Order {OrderNumber} received - your work will start shortly`
- Body: greeting, "order receive ho gaya, work start ho raha hai", order number, service, plan,
  submitted time, current status, + optional admin note
- Optional note max 2000 chars
- Success par `Orders.CustomerNotifiedAt` set hota hai; card par **Sent + last sent time**
- Email fail (key missing / provider reject) par friendly error, order notified mark **nahi** hota

**Kya nahi:** Admin order **create/edit/delete** nahi kar sakta; customer ka address edit nahi;
items add/remove nahi; email **automatic nahi** (admin button dabata hai); resend par
duplicate check nahi (jitni baar chahein bhej sakte hain).

### 8.10 Connections — `/Admin/Connections`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Connections` | Search (account/customer), status filter, type filter, 10/page |
| Details | `/Admin/Connections/Details/{id}` | Connection info, plan, city, order link, outstanding, bills table, equipment, **Update Status** form |

**Status options:** Pending, Active, TemporarilyInactive, PermanentlyInactive, Cancelled.

- PermanentlyInactive / Cancelled connection **reactivate nahi** hota
- Active karne par `ActivatedAt` set (agar pehle set nahi)
- Admin connection **create nahi** karta (sirf technical banata hai)

### 8.11 Billing — `/Admin/Bills`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Bills` | Search (bill/account/customer), status filter, KPI: Total Bills + Outstanding, 10/page |
| Details | `/Admin/Bills/Details/{id}` | Bill items, totals, tax, payments history |
| Generate | `/Admin/Bills/Generate?connectionId=` | Eligible connection + period + equipment/replacement charges + discounts → bill |

Billing rules section 17 mein. Admin aur Accounts dono bill generate kar sakte hain (same service).

**Kya nahi:** bill **cancel** karne ka koi button nahi (enum `Cancelled` mojood hai, action nahi);
bill **edit/delete** nahi; bill email/post nahi hota — customer panel mein dikhna hi notification hai.

### 8.12 Payments — `/Admin/Payments`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Payments` | Search (receipt/bill/reference/naam), method filter, status filter, from/to date, 10/page + summary cards (collected, pending, refunded, success rate) |
| Export | `/Admin/Payments/Export?same-filters` | CSV (`payments-yyyyMMdd-HHmm.csv`), 8 columns, screen ke filters ke mutabiq, **paging nahi** (sab matching rows) |
| Details | `/Admin/Payments/Details/{id}` | Payment, bill, connection+plan, recorded by, notes |

**Kya nahi:** Admin payment **record / confirm / reject** nahi karta (wo Accounts ka kaam hai);
payment reversal/refund ka koi action nahi (`Reversed` enum sirf display ke liye).

### 8.13 Messages — `/Admin/Messages`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Admin/Messages` | Contact form ke messages, 4 KPI tiles (Total/Unread/Replied/Pending), table + actions |
| Details | `/Admin/Messages/Details/{id}` | Poora message, sender, received time, mark read/unread, Reply button |
| Reply | `/Admin/Messages/Reply/{id}` | Subject (prefilled `Re: ...`) + body → **Resend** se email, message `Replied` + read mark hota hai |

**Reply rules:** sender email/naam hamesha **DB se** aata hai (form se nahi); `Email:ApiKey` na ho
ya provider reject kare to error dikhta hai aur message `Replied` mark nahi hota.

**⚠️ Jo UI mein hai lekin kaam nahi karta (verified):**

| UI element | Status |
|---|---|
| Search box (`name="q"`) | **Filter nahi karta** — action `query` param padhta hai |
| Status dropdown (`name="status"`) | **Filter nahi karta** — action `unread/replied/pending` bools padhta hai |
| Date range dropdown (`name="dateRange"`) | **Filter nahi karta** — action mein param hi nahi |
| Tabs (All / Unread / Read / Pending) | Links `?status=...` bhejte hain jo bind nahi hota → **hamesha All** |
| Sort dropdown | `sort` param bhejta hai jo controller ignore karta hai |
| "New Message" button | `#newMessageModal` kholta hai jo **page par mojood hi nahi** → kuch nahi hota |
| Table checkbox / "select all" | Koi bulk action nahi |

Working params (agar manually URL se diye jayein): `?query=`, `?unread=true`, `?replied=true`,
`?pending=true`. Abhi UI inhe use nahi karta.

### 8.14 Feedback — `/Admin/Feedback`

| Cheez | Detail |
|---|---|
| List | Sab customers ka feedback, newest first, 10/page |
| KPI tiles | Total Feedback, Pending Review, Reviewed, Average Rating (rated submissions ka average) |
| Filters | Search (customer naam/email/message) + status (All / Pending review / Reviewed) — **ye filters kaam karte hain** |
| Row actions | **Mark reviewed / Mark pending** (DB turant update) |
| Columns | Submitted, Customer (naam+email), Connection account, Rating ★/5, Message, Status |

**Kya nahi:** feedback ka reply email yahan se nahi jata (sirf review flag); feedback delete nahi
hota; rating edit nahi hoti.

### 8.15 Advanced Search — `/Admin/Search`

Ek form se: free text, order number, account number, customer name, contact number, connection
type, order status, connection status, from/to date.

| Kya hota hai | Kya nahi hota |
|---|---|
| Matching **Orders + Connections + Customers** ek page par (teen tables) | Payment / bill / message search nahi |
| Topbar ka search box bhi isi page par bhejta hai (`Query=`) | Result export nahi |

---

## 9) Retail panel — `/Retail` (7 menu)

Retail sirf **apni shop** ka data dekhta hai. Employee ka active shop na ho to har page `403`
(Forbid) deta hai.

### 9.1 Dashboard — `/Retail/Dashboard`

7 KPI: Customers, Orders, Pending Orders, Active Connections, Bills With Dues, Payments Received,
Active Plans + **Recent Orders** table (apni shop ke).

### 9.2 Customers — `/Retail/Customers`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Retail/Customers` | Search + 10/page, sirf apni shop ke city ke customers |
| Details | `/Retail/Customers/Details/{id}` | Customer + address + orders/connections summary |
| Create | `/Retail/Customers/Create` | Customer + address (city = shop ki city) |

**Rules:** customer sirf **shop ke service area (city)** mein bana sakta hai; email unique.
**Kya nahi:** retail customer **edit/delete** nahi kar sakta (sirf Admin).

### 9.3 Orders — `/Retail/Orders`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Retail/Orders` | Search, status filter, connection type filter, 10/page |
| Details | `/Retail/Orders/Details/{id}` | Customer/address, plan, items, feasibility, connection, history + **3 decision forms** |
| Create (walk-in) | `/Retail/Orders/Create?customerId=` | Customer + connection type + plan + optional products + notes |
| Track | `/Retail/Orders/Track?orderNumber=` | Order number se apni shop ka order dhoondna |

**3 decisions (POST):**

| Button | Result status | Rules |
|---|---|---|
| **Verify** | `VerifiedByRetail` | Notes optional (2000); order cancel/feasible/connection ho chuka ho to block |
| **Reject** | `RetailRejected` | Reason **laazmi** (2000) |
| **Request Info** | `AwaitingCustomerInformation` | Notes optional (2000) |

**Create rules:** customer active + shop ke city mein; plan active aur connection type match;
products ka stock available hona chahiye (warna error); stock turant **reserve** hota hai.

**Kya nahi:** retail order **cancel** nahi kar sakta; customer ko email/notification nahi jata;
edit/delete nahi.

### 9.4 Plans — `/Retail/Plans`

Sirf active plans ki read-only list (search). Nahi: create/edit.

### 9.5 Connections — `/Retail/Connections`

Read-only list (search, 10/page) + details. Nahi: status change, create.

### 9.6 Bills — `/Retail/Bills`

Read-only list (search, status filter) + details (items, payments). Nahi: generate, record payment.

### 9.7 Payments — `/Retail/Payments`

Read-only list (search) — apni shop ki connections ki payments. Nahi: record/verify.

---

## 10) Technical panel — `/Technical` (5 menu)

Technical ko retail ke **verify kiye bagair** order nahi milta.

### 10.1 Dashboard — `/Technical/Dashboard`

8 KPI: Orders Awaiting Action, Pending Feasibility, Approved Feasibility, Not Feasible Orders,
Pending Connections, Active Connections, Equipment Units, Available Equipment + do tables
(orders needing feasibility, pending connections).

### 10.2 Orders — `/Technical/Orders`

| Page | Route | Kya hota hai |
|---|---|---|
| List (queue) | `/Technical/Orders` | Forwarded orders (retail-verified), search, 10/page |
| Details | `/Technical/Orders/Details/{id}` | Order + customer/address + feasibility + connection + history + "Update Status" form |

**Status update restrictions (code-enforced):**

- `Cancelled` **nahi** kar sakta
- Seedha `ConnectionProvided` **nahi** kar sakta (connection create se hota hai)
- Feasibility report ho to order status report se match karna chahiye
- Baqi transitions `OrderWorkflow` ke mutabiq

### 10.3 Feasibility — `/Technical/Feasibility`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Technical/Feasibility` | Sab feasibility reports + **pending orders** (jinke report nahi) |
| Details | `/Technical/Feasibility/Details/{id}` | Report ka result, technician, distance, server available, remarks, order info |
| Create | `/Technical/Feasibility/Create?orderId=` | Order (sirf `VerifiedByRetail` / `FeasibilityPending`, report na ho), technician, status, distance, server available, remarks |
| Edit | `/Technical/Feasibility/Edit/{id}` | Result update — **connection ban jane ke baad block** |

**Create/Edit ka asar:** order status `Feasible` / `NotFeasible` / `FeasibilityPending` set hota
hai + status history entry banti hai.

### 10.4 Connections — `/Technical/Connections`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Technical/Connections` | Search + status filter, 10/page |
| Details | `/Technical/Connections/Details/{id}` | Connection + customer + plan + order + equipment + bills + **Activate** / **Update Status** |
| Create | `/Technical/Connections/Create?orderId=` | Sirf `Feasible` order (jis ka connection na ho) + optional "activate immediately" + installation notes |

**Create ka result:** account number (`B/T/D` + city code + serial), connection `Pending` (ya
`Active`), `InstalledAt`, order `ConnectionProvided`, plan history entry.

**Activate rules:** already active → error; linked order `ConnectionProvided` na ho → error.
Activate hone par `ActivatedAt` set — yehi Accounts ka billing handoff hai.

### 10.5 Equipment — `/Technical/Equipment`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Technical/Equipment` | Search (asset tag/serial) + status filter, 10/page |
| Assign | `/Technical/Equipment/Assign?equipmentId=` | Available equipment + active/pending connection select |

Row actions (POST): **Return** (Assigned → Available), **Set Repair** (Available/Assigned →
UnderRepair), **Complete Repair** (UnderRepair → Available).

**Rules:** sirf Available equipment assign hoti hai; sirf Active/Pending connection par.
**Kya nahi:** equipment create/delete Technical nahi kar sakta (seed data/DB se aata hai);
`Retired` status ka koi action nahi.

---

## 11) Accounts panel — `/Accounts` (6 menu)

### 11.1 Dashboard — `/Accounts/Dashboard`

8 KPI: Total Bills, Paid Bills, Unpaid Bills, Partially Paid, Payments Received, Outstanding
Amount, Customers, Active Connections + "Bills With Outstanding Dues" + "Recent Payments".

### 11.2 Bills — `/Accounts/Bills`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Accounts/Bills` | Search (bill/account/customer), status filter, 10/page + Total Outstanding |
| Details | `/Accounts/Bills/Details/{id}` | Bill items/tax/totals + payments; due ho to **Record payment** button |
| Generate | `/Accounts/Bills/Generate?connectionId=` | Eligible connection + period + charges + discounts → bill (tax 12.24%) |
| **Notify Customer** | `/Accounts/Bills/NotifyCustomer/{id}` (POST) | Us bill ke **customer ke apne panel** mein message bhejta hai (bell + Notifications page) + Pay now link |

**Notify Customer rules:**

- Message sirf us bill ke **customer** ko jata hai (`CustomerId` bill se aata hai, form se nahi)
- Title: `Bill {BillNumber} is ready - PKR {due} due`; body: period, amount due, due date,
  "customer panel → My Bills → Pay now" hidayat + optional note (max 1000 chars)
- `CustomerNotifications` row banti hai (`BillId`, `Amount`, `CreatedByEmployeeId`, `IsRead = false`)
- Bill details card par **Sent / Not sent yet**, last sent time, customer ne parhi ya nahi, aur kitni
  baar bheja gaya — sab dikhta hai
- Due 0 ho (bill clear) to button hide ho jata hai
- Resend allowed hai (har baar nayi row banti hai)

**Kya nahi:** bill cancel/edit/delete nahi; notification **email/SMS nahi** jata — sirf customer panel
mein (section 12.9); customer us message ka jawab nahi de sakta.

### 11.3 Payments — `/Accounts/Payments`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Accounts/Payments` | Search, method filter, status filter + pending count/amount banner |
| Details | `/Accounts/Payments/Details/{id}` | Payment + bill + connection + recorded by + **Confirm / Reject** forms |
| Create (manual) | `/Accounts/Payments/Create?billId=` | Cash/card/bank/cheque/other/wallet + amount + reference + notes |

| Action | Kya hota hai | Rules |
|---|---|---|
| **Confirm** (`Verify`) | Payment `Recorded`, naya receipt `RCP-...`, bill par amount apply, notes save | Sirf `PendingVerification`; amount due se zyada ho to reject karna hoga |
| **Reject** | Payment `Cancelled`, bill bilkul unchanged, reason notes mein save | Reason laazmi (1000) |
| **Record** (manual) | Payment turant `Recorded`, bill update | Sirf latest bill; due se zyada nahi; reference duplicate nahi; us bill par pending online payment ho to block |

### 11.4 Outstanding Dues — `/Accounts/Dues`

Latest bill per connection jis par due > 0: Total Outstanding, Overdue Count, Overdue Amount;
search + "overdue only" filter (due date < aaj), 10/page.

### 11.5 Customers (read-only) — `/Accounts/Customers`

List (search) + Details (customer, outstanding, bills/payments summary). Nahi: create/edit.

### 11.6 Connections (read-only) — `/Accounts/Connections`

List (search, status filter) + Details (connection, plan, bills, outstanding). Nahi: create/status
change.

---

## 12) Customer panel — `/Customer` (9 menu)

Customer sirf apna data dekhta hai. Login kisi active Customer se linked na ho to error message
ke saath khaali list milti hai.

### 12.1 Dashboard — `/Customer/Dashboard`

KPI: Connections, Outstanding Dues, Pending Orders, Payments Made.
Sections: outstanding alert + **Pay now**, Recent Orders, Recent Payments, My Connection,
Latest Bill (due + Pay now).

### 12.2 Profile — `/Customer/Profile`

Dekhna + Edit (naam/phone/email) + Change Password. Address edit nahi.

### 12.3 My Orders — `/Customer/Orders`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Customer/Orders` | Apne sab orders (status, plan, connection, feasibility) |
| New Order | `/Customer/Orders/Create?planId=` | Active plan select + notes (plan `?planId=` se preselect ho jata hai) |
| Details | `/Customer/Orders/Details/{id}` | Order status, plan, items, feasibility, connection |

**New order par system:** order `Pending`; customer ke **primary address ke city wali retail shop**
auto-assign; order number `B/T/D` + 10-digit sequence.

**Kya nahi:** customer order **cancel** nahi kar sakta; products select nahi kar sakta (sirf plan);
order edit nahi.

### 12.4 My Connection — `/Customer/Connection`

Apni sab connections: account number, status, plan, city, installed/activated dates, equipment,
plan history. Nahi: koi action (read-only).

### 12.5 Current Plan — `/Customer/Plan`

Apni connections ke plans (unique list) + plan detail. Nahi: plan change request (naya order hi
raasta hai).

### 12.6 My Equipment — `/Customer/Equipment`

Apni connections par assigned equipment (asset tag, product, status, dates). Read-only.

### 12.7 My Bills — `/Customer/Bills`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Customer/Bills` | Sab bills (newest first), due ho to **Pay now** |
| Details | `/Customer/Bills/Details/{id}` | Items, tax, totals, payments; pending online payment ho to "awaiting confirmation" banner |

### 12.8 Payment History / Pay a Bill — `/Customer/Payments`

| Page | Route | Kya hota hai |
|---|---|---|
| History | `/Customer/Payments` | Sab payments + status (Recorded / PendingVerification / Cancelled / Reversed) |
| Pay | `/Customer/Payments/Pay?billId=` | Open bills select → amount → method → transaction reference → notes → submit |

**Payment rules:** sirf apni **latest** bill; amount due se zyada nahi; methods: Card,
BankTransfer, MobileWallet, Cheque (sab par reference laazmi); ek bill par ek waqt mein ek hi
pending online payment; reference duplicate nahi. Submit par `PendingVerification` — **bill abhi
change nahi hota**.

### 12.9 Notifications — `/Customer/Notifications`

Accounts ke **Notify Customer** button se bheje gaye bill messages yahan aate hain.

| Cheez | Detail |
|---|---|
| List | Apni notifications, newest first (title, time, amount, bill number, poora message) |
| Unread | Bell par **New notifications** count; page kholne par sab **read** mark ho jati hain |
| Pay now | Sirf us bill par jo **abhi payable** hai (latest bill + due > 0) — seedha `Payments/Pay?billId=` par |
| View bill | Bill details par le jata hai |
| Purana bill | Latest na ho to **Pay now nahi** milta — note: "Ye bill ab latest nahi hai, latest bill My Bills se pay karein" |

**Kya nahi:** customer notification delete nahi kar sakta; yahan se reply nahi hota; ye list
sirf usi customer ki rows dikhati hai (`CustomerId` filter).

### 12.10 Feedback — `/Customer/Feedback`

| Page | Route | Kya hota hai |
|---|---|---|
| List | `/Customer/Feedback` | Apna feedback + reviewed/pending status |
| Create | `/Customer/Feedback/Create` | Rating (1–5, optional) + message + optional connection (sirf apni) |

Feedback save hote hi **Admin → Feedback** inbox mein aa jata hai (bell count bhi barhta hai).
**Kya nahi:** customer apna feedback edit/delete nahi kar sakta; feedback par email reply nahi
jata.

---

## 13) Order kahan se aata hai

### Method 1 — Customer self-service (customer panel)
1. Login → **My Orders → New Order**
2. Active plan select + notes
3. Submit

System: order `Pending`; primary address ke city wali retail shop auto-assign; order number
`B`/`T`/`D` + 10-digit sequence (e.g. `B0000000001`). Products nahi — sirf plan + notes.

### Method 2 — Retail walk-in order (retail panel)
1. Retail login → **Orders → Create**
2. Customer + connection type + plan + optional products + notes
3. Submit

System: order us shop ko assign; products ka stock **reserve**; order `Pending`.

### Method 3 — Public plan page
**Plan Details** page ka CTA customer ko login/register par le jata hai, phir Method 1 chalta hai.

> Admin order create nahi karta. Admin sirf monitor, status update, aur customer notification
> (email) karta hai.

---

## 14) Order statuses aur transitions

| Status | Matlab | Kahan se set hota hai |
|---|---|---|
| `Pending` | Order aaya, retail verification baqi | Order create |
| `AwaitingCustomerInformation` | Retail ne customer se correction mangi | Retail → Request Info |
| `RetailRejected` | Retail ne reject kiya (reason recorded) | Retail → Reject |
| `VerifiedByRetail` | Retail verified, technical ke paas | Retail → Verify |
| `FeasibilityPending` | Site check schedule, result baqi | Technical feasibility (Pending) |
| `Feasible` | Site check pass — connection ban sakta hai | Technical feasibility (Feasible) |
| `NotFeasible` | Site check fail | Technical feasibility (NotFeasible) |
| `ConnectionProvided` | Connection ban gaya/install ho gaya (final) | Connection create |
| `Cancelled` | Cancel (reserved stock release) | Admin/Retail/Technical (limits ke saath) |

Allowed transitions (`Helpers/OrderWorkflow.cs`):

```
Pending ──────────────> VerifiedByRetail ──> FeasibilityPending ──> Feasible ──> ConnectionProvided
   │                          │                      │                  │
   ├─> AwaitingCustomerInfo   ├─> Feasible            └─> NotFeasible   └─> NotFeasible
   ├─> RetailRejected         ├─> NotFeasible
   └─> Cancelled              └─> Cancelled
NotFeasible ──> Pending
RetailRejected ──> Pending
Cancelled / ConnectionProvided = final
```

Extra guards (service layer):

- Cancelled order **reopen nahi** hota
- Feasibility report ho to order status report se **match** karna chahiye
- Connection ban jaye to status `ConnectionProvided` hi rehna chahiye
- Har real change `OrderStatusHistory` mein (from, to, employee, notes, time) record hota hai —
  same status par sirf notes update ho to nayi history entry nahi banti

---

## 15) End-to-end workflow (kaun kise kya bhejta hai)

```
Customer order
   |
   v
Retail (apni shop) --verify--> Technical --site check--> Connection install
   |                                                        |
   | reject / request info                                  v
Customer <------------------------------------------- Activate (Active)
                                                            |
                                                            v
Accounts bill generate --> Customer panel/dashboard par due
                                                            |
                                     Customer online payment (PendingVerification)
                                                            |
                                                            v
                                          Accounts Confirm / Reject
                                                            |
                              Confirm -> bill Paid/Partial + receipt
                              Reject  -> bill waisa hi, reason record
```

### Step-by-step

| # | Kaun | Kahan | Kya karta hai | System kya karta hai |
|---|---|---|---|---|
| 1 | Customer | Customer → My Orders → New Order | Plan + notes | `Pending`; city wali shop auto-assign |
| 2 | Admin | Admin → Orders | Order dekh sakta hai | List/details |
| 3 | Admin *(optional)* | Admin → Orders → Details | **Notify Customer** | Resend email "order receive ho gaya, work start ho raha hai" |
| 4 | Retail | Retail → Orders → Details | Customer/address check + **Verify** | `VerifiedByRetail`; technical queue |
| 5 | Technical | Technical → Feasibility → Create | Technician + result | `FeasibilityPending` / `Feasible` / `NotFeasible` |
| 6 | Technical | Technical → Connections → Create | Feasible order + notes | Account number, order `ConnectionProvided` |
| 7 | Technical | Connection Details | Equipment assign + **Activate** | `Active`, `ActivatedAt` |
| 8 | Accounts | Accounts → Bills → Generate | Period + charges | Bill + items, 12.24% tax, due date = period end + 14 |
| 8b | Accounts | Accounts → Bill Details → **Notify Customer** | Bill-ready message + optional note | Customer panel notification + bell count (sirf usi customer ko) |
| 9 | Customer | Dashboard / My Bills | Due + **Pay now** | Outstanding due dikhta hai |
| 10 | Customer | Pay a Bill | Bill, amount, method, reference | Payment `PendingVerification`; bill unchanged |
| 11 | Accounts | Payments → Review | **Confirm** ya **Reject** | Confirm: bill update + receipt. Reject: bill unchanged |
| 12 | Customer | Payment History | Status + receipt | `Recorded` payment, kam due |
| 13 | Customer | Feedback | Rating + message | Admin → Feedback inbox + bell count |

### Handoff table

| Sender | Receiver | Kya receive hota hai | Receiver kya karta hai |
|---|---|---|---|
| Customer | Retail (+ Admin view) | Naya order | Verify / Reject / Request info |
| Retail | Technical | Verified order | Technician assign + site check |
| Technical | Technical | Feasibility result | Feasible → connection create |
| Technical | Accounts | Active connection | Bill generate |
| Accounts | Customer | Bill + due | Online payment |
| Customer | Accounts | Payment (`PendingVerification`) | Confirm / Reject |
| Accounts | Customer | Confirmation / rejection | Payment history + due update |
| Public visitor | Admin | Contact message | Read / Reply (email) |
| Admin | Customer | Order received email | Customer ko confirmation |
| Customer | Admin | Feedback | Review mark |
| Accounts | Customer | Bill notification (panel) | Bell dekhe → Notifications → **Pay now** |

---

## 16) Technical workflow (detail)

### 16.1 Queue
Technical panel in statuses wale orders dikhata hai: `VerifiedByRetail`, `FeasibilityPending`,
`Feasible`, `ConnectionProvided`. Retail ke paas pade (`Pending`), reject hue, ya
`AwaitingCustomerInformation` orders technical ko nahi dikhte.

### 16.2 Feasibility (site check)
Form: order, assigned technician (Technical role employees), status (Pending/Feasible/
NotFeasible), distance (meters), server/network available (yes/no), remarks.

Save par: report banti hai, order status report ke mutabiq set, history entry.

### 16.3 Connection create
Form: feasible order, "activate immediately" (optional), installation notes.

System: account number `B/T/D` + 3-digit city code + 12-digit serial = 16 chars
(e.g. `B001000000000001`), status `Pending`/`Active`, `InstalledAt`, order `ConnectionProvided`,
plan history entry.

### 16.4 Activation
Pending connection → **Connection secured - activate** (notes optional). Activate = `Active` +
`ActivatedAt`. **Accounts sirf Active / TemporarilyInactive connection ka bill bana sakta hai.**

### 16.5 Equipment
Available → assign (connection par) → Return / Repair → Available.
Statuses: `Available`, `Assigned`, `UnderRepair`, `Retired` (`Retired` ka koi UI action nahi).

---

## 17) Billing rules (Admin + Accounts)

### Eligible connections
`Active`, `TemporarilyInactive`. Pending / permanently inactive / cancelled connection ka bill
nahi banta.

### Formula
```
subtotal    = monthly rental + equipment charges + replacement charges
discounts   = discount + scheme discount
taxable     = max(subtotal - discounts, 0)
service tax = round(taxable * 12.24%, 2)      <- midpoint away from zero
total       = taxable + service tax
due         = total + previous balance (latest non-cancelled bill ka due)
```

- **Service tax:** 12.24% (constant, `IBillingService.ServiceTaxRate`)
- **Due date:** billing period end + 14 din
- **Bill number:** `B-{connectionId}-{start:yyyyMMdd}-{end:yyyyMMdd}`
- Naya bill period pichle bill ke period ke **baad** hi start ho sakta hai
- Ek connection + period par ek hi bill (duplicate reject)
- Charges/discounts negative nahi

### Bill statuses
`Unpaid`, `PartiallyPaid`, `Paid`, `Cancelled` (cancel karne ka UI action nahi hai).

Demo (seed) bill: `BILL-B001000000000001-202609` total 1683.60, paid 1000, due 683.60.

Customer ko bill turant panel/dashboard/bell par dikhta hai — alag "send notice" nahi hai.

---

## 18) Payment methods aur verification

### 18.1 Customer online payment — `/Customer/Payments/Pay`

| Method | Reference zaroori? |
|---|---|
| Card | Haan |
| Bank Transfer | Haan |
| Mobile Wallet (JazzCash/Easypaisa) | Haan |
| Cheque | Haan |

- Bill sirf apna aur **latest** ho
- Amount > 0, <= due, max 9,999,999
- Referenceduplicate nahi
- Ek bill par ek pending online payment

Submit = `PendingVerification`, **bill untouched**.

### 18.2 Manual payment — `/Accounts/Payments/Create`

Methods: Cash, Card, BankTransfer, Cheque, Other, MobileWallet.
Rules: amount > 0 <= due; sirf latest bill; reference duplicate nahi; us bill par pending online
payment ho to manual record block (pehle confirm/reject).

Record = `Recorded` + receipt `RCP-...` + bill foran update.

### 18.3 Statuses

| Status | Matlab |
|---|---|
| `Recorded` | Paisa confirm, bill par apply |
| `PendingVerification` | Customer ne submit kiya, accounts confirmation baqi |
| `Cancelled` | Reject, bill par apply nahi |
| `Reversed` | Enum mein mojood, **koi action nahi** (refund/reversal flow nahi) |

### 18.4 Confirm / Reject (Accounts)

- **Confirm:** latest bill + due re-check; amount apply; bill `Paid`/`PartiallyPaid`; payment
  `Recorded` + naya receipt; verification note save
- **Reject:** reason laazmi; payment `Cancelled`; bill bilkul unchanged; reason note

Bell par "Online payments to verify" count.

---

## 19) Notifications

### 19.1 Panel bell (`ViewComponents/PanelNotificationsViewComponent.cs`)

| Panel | Bell items |
|---|---|
| Admin | Pending orders, low stock items, unread messages, **feedback awaiting review** |
| Retail | Pending orders (apni shop), bills with dues |
| Technical | New jobs from retail, feasibility reports awaiting check, connections awaiting installation |
| Accounts | Online payments to verify, bills with dues, unpaid bills |
| Customer | Unpaid bills, orders in progress, payments awaiting confirmation, **new notifications** (accounts ke bheje bill messages) |

Count 0 ho to item show nahi hota. Bell page-specific list par le jata hai.

### 19.2 Email (Resend) — 2 use cases

| Kahaan se | Kab | Kis ko |
|---|---|---|
| Admin → Messages → **Reply** | Admin reply button dabaye | Contact form bhejne wala |
| Admin → Orders → Details → **Notify Customer** | Admin "order received" email bheje | Order ka customer |

Config:

```
Email:ApiKey      <- required (ya env RESEND_API_KEY)
Email:From        <- verified sender (default onboarding@resend.dev)
Email:FromName    <- display name
Email:ApiBaseUrl  <- optional, default https://api.resend.com
```

- Development: **User Secrets** se key
  ```bash
  cd "Nexos Network System"
  dotnet user-secrets set "Email:ApiKey" "re_your_key"
  ```
- Production: env var `Email__ApiKey` ya `RESEND_API_KEY` (User Secrets wahan load nahi hoti)
- Key missing / provider reject → friendly error admin ko, kaam adhoora nahi hota (message
  `Replied`/`CustomerNotifiedAt` mark nahi hota)
- `onboarding@resend.dev` sirf Resend account owner ke email par deliver karta hai; asli customer
  domain ke liye domain verify + `Email:From` set karna hoga

**Kya nahi:** bill/payment/order-status automatic emails nahi; SMS/WhatsApp nahi; email queue
nahi (request ke andar bhejta hai).

---

## 20) Stock / inventory rules

- Order create par selected products ka stock **reserve** (`QuantityReserved += qty`)
- Order cancel par reserved stock **release**
- Available = `QuantityOnHand - QuantityReserved`
- Movement types: `In`, `Out`, `Adjustment`, `AdjustmentDecrease`
- Out available se zyada nahi; adjustment reserved se neeche nahi
- **Auto-deduct nahi:** installation/connection par stock khud kam nahi hota — Admin `Stock → Adjust`
  se physical issue record karta hai

---

## 21) Support flow (messages, feedback, email)

```
Public visitor --Contact form--> ContactMessages (Admin inbox)
                                      |-- Reply → Resend email → sender
                                      |-- Mark read/unread

Customer --Feedback--> FeedbackEntries --> Admin → Feedback inbox (bell count)
                                             |-- Mark reviewed / pending
                                             └-- koi email reply nahi

Admin --Orders → Notify Customer--> Customer email (order received)

Accounts --Bills → Notify Customer--> Customer panel notification (bill ready)
                                            |-- Pay now → Payments/Pay?billId=
                                            |-- Accounts confirm/reject (normal payment flow)
```

---

## 22) URL cheat sheet (complete)

### Public
```
/                              Home
/Home/Services                 Services
/Home/Plans                    Plans
/Home/PlanDetails/{id}         Plan details
/Home/About                    About
/Home/Contact                  Contact form (GET/POST)
/Home/TrackOrder               Order tracking (GET/POST)
/Home/ConnectionStatus         Connection status (GET/POST)
/Home/Privacy                  Privacy
/Home/Terms                    Terms
/Home/StatusCode?code=404      Friendly status page
/Home/Error                    Error page
```

### Identity
```
/Identity/Account/Login                  GET/POST
/Identity/Account/Register               GET/POST
/Identity/Account/Logout                 POST
/Identity/Account/AccessDenied
/Identity/Account/Profile                redirect to panel profile
```

### Admin — `/Admin`
```
/Dashboard
/Employees          /Employees/Details/{id}      /Employees/Create
/Employees/Edit/{id} /Employees/Delete/{id}
/Customers          /Customers/Details/{id}      /Customers/Create
/Customers/Edit/{id} /Customers/Delete/{id}
/RetailShops        /RetailShops/Details/{id}    /RetailShops/Create
/RetailShops/Edit/{id} /RetailShops/Delete/{id}
/Vendors            /Vendors/Details/{id}        /Vendors/Create
/Vendors/Edit/{id}  /Vendors/Delete/{id}
/Products           /Products/Details/{id}       /Products/Create
/Products/Edit/{id} /Products/Delete/{id}
/Stock              /Stock/Adjust/{id}           /Stock/Movements/{id}
/Plans              /Plans/Details/{id}          /Plans/Create
/Plans/Edit/{id}    /Plans/Delete/{id}           /Plans/ToggleActive
/Orders             /Orders/Details/{id}         /Orders/UpdateStatus
/Orders/NotifyCustomer/{id}
/Connections        /Connections/Details/{id}    /Connections/UpdateStatus
/Bills              /Bills/Details/{id}          /Bills/Generate
/Payments           /Payments/Details/{id}       /Payments/Export
/Messages           /Messages/Details/{id}       /Messages/Reply/{id}
/Messages/ToggleRead
/Feedback           /Feedback/ToggleReviewed/{id}
/Search
/Profile            /Profile/Edit                /Profile/ChangePassword
```

### Retail — `/Retail`
```
/Dashboard
/Customers      /Customers/Details/{id}      /Customers/Create
/Orders         /Orders/Details/{id}         /Orders/Create
/Orders/Verify  /Orders/Reject               /Orders/RequestInfo
/Orders/Track
/Plans
/Connections    /Connections/Details/{id}
/Bills          /Bills/Details/{id}
/Payments
/Profile        /Profile/Edit                /Profile/ChangePassword
```

### Technical — `/Technical`
```
/Dashboard
/Orders         /Orders/Details/{id}         /Orders/UpdateStatus
/Feasibility    /Feasibility/Details/{id}    /Feasibility/Create
/Feasibility/Edit/{id}
/Connections    /Connections/Details/{id}    /Connections/Create
/Connections/Activate/{id}                   /Connections/UpdateStatus
/Equipment      /Equipment/Assign
/Equipment/Return/{id}                       /Equipment/SetRepair/{id}
/Equipment/CompleteRepair/{id}
/Profile        /Profile/Edit                /Profile/ChangePassword
```

### Accounts — `/Accounts`
```
/Dashboard
/Bills          /Bills/Details/{id}          /Bills/Generate
/Bills/NotifyCustomer/{id}
/Payments       /Payments/Details/{id}       /Payments/Create
/Payments/Verify /Payments/Reject
/Dues
/Customers      /Customers/Details/{id}
/Connections    /Connections/Details/{id}
/Profile        /Profile/Edit                /Profile/ChangePassword
```

### Customer — `/Customer`
```
/Dashboard
/Profile        /Profile/Edit                /Profile/ChangePassword
/Orders         /Orders/Create               /Orders/Details/{id}
/Connection
/Plan
/Equipment
/Bills          /Bills/Details/{id}
/Payments       /Payments/Pay
/Notifications
/Feedback       /Feedback/Create
```

---

## 23) Page coverage matrix (audit result)

### Public + Identity

| Page | Kaam karta hai? | Note |
|---|---|---|
| Home / Services / Plans / PlanDetails | ✅ | data-driven plans, baqi static |
| Contact | ✅ | DB save; email nahi |
| Track Order / Connection Status | ✅ | public lookup |
| Register / Login / Logout | ✅ | validation + lockout |
| Forgot password / email confirm | ❌ | feature nahi hai |

### Admin

| Page | Status | Kya nahi / note |
|---|---|---|
| Dashboard | ✅ | filters/export nahi |
| Employees (list/details/create/edit/delete) | ✅ | Admin role assign nahi; delete = deactivate + login lock |
| Customers (list/details/create/edit/delete) | ✅ | delete = deactivate; ek address |
| Retail Shops | ✅ | delete = deactivate if used |
| Vendors | ✅ | delete = deactivate if used |
| Products | ✅ | delete = deactivate if used |
| Stock (list/adjust/movements) | ✅ | auto-deduct nahi |
| Plans | ✅ | delete = deactivate if used |
| Orders (list/details/status) | ✅ | create/edit/delete nahi |
| Orders → Notify Customer | ✅ | manual; test sender sirf owner email par |
| Connections (list/details/status) | ✅ | create nahi |
| Bills (list/details/generate) | ✅ | cancel/edit nahi |
| Payments (list/details/export) | ✅ | confirm/reject sirf Accounts |
| Messages (list/details/reply) | ⚠️ **partial** | reply ✅; search/status/date/tabs/sort/modal ❌ (section 8.13) |
| Feedback (list/toggle reviewed) | ✅ | reply/delete nahi |
| Advanced Search | ✅ | orders/connections/customers only |

### Retail / Technical / Accounts / Customer

| Page | Status | Note |
|---|---|---|
| Retail dashboard/customers/orders/plans/connections/bills/payments/track | ✅ | sab read-only except customers create + orders |
| Retail customer edit/delete | ❌ | sirf Admin |
| Technical dashboard/orders/feasibility/connections/equipment | ✅ | cancel / direct ConnectionProvided block |
| Technical equipment create/manage | ❌ | sirf assign/return/repair |
| Accounts dashboard/bills/payments/dues/customers/connections | ✅ | bill cancel nahi, payment reversal nahi |
| Accounts → Bill → **Notify Customer** | ✅ | customer panel mein message jata hai (email nahi) |
| Customer dashboard/profile/orders/connection/plan/equipment/bills/payments/feedback | ✅ | order cancel/equipment request/plan change nahi |
| Customer → **Notifications** | ✅ | sirf apni rows; pay link latest payable bill par hi |

---

## 24) System-enforced rules (short list)

- Technical ko retail-verified order ke bagair kaam nahi milta
- Feasibility `Feasible` ke bagair connection nahi banta
- Sirf Active / TemporarilyInactive connection billable hai
- Bill sirf latest bill par pay hota hai; due se zyada amount reject
- Ek bill par ek hi pending online payment
- Payment reference duplicate nahi (dono taraf)
- Customer sirf apna bill/connection/order dekh sakta hai
- Retail sirf apni shop ka data; shop active na ho to Forbid
- Cancel par reserved stock release
- Har order status change history mein record
- PermanentlyInactive / Cancelled connection reactivate nahi hoti
- Cancelled order reopen nahi hota
- Admin role employee management se assign nahi hota
- Employee/customer delete = deactivate (history safe)
- Plan/product/vendor/shop delete = deactivate jab wo use ho rahe hon

---

## 25) Limitations / jo cheezein abhi nahi hain

1. **Payment gateway nahi** — online payment submit hoti hai, Accounts manually confirm karta hai.
   Real gateway chahiye to `IPaymentService.SubmitOnlinePaymentAsync` ko gateway callback se replace karo.
2. **Email sirf 2 kaam** ke liye: contact reply + order received. Bill notification **sirf customer
   panel** mein jata hai (email/SMS/WhatsApp nahi).
3. **Order notification manual** hai — admin `Notify Customer` dabata hai; order create par
   automatic email nahi jata.
4. **Resend test sender** (`onboarding@resend.dev`) sirf account owner ke email par deliver karta
   hai; customer ke asli address ke liye domain verify + `Email:From` chahiye.
5. **Admin Messages page ke filters kaam nahi karte** (section 8.13) — search, status, date, tabs,
   sort, "New Message" modal. Reply/details/mark-read theek chalte hain.
6. **Bill cancel / payment reversal** ka koi action nahi (`Cancelled` / `Reversed` sirf display).
7. **Stock auto-deduct** connection/installation par nahi hota.
8. **Technician assignment manual** hai (dropdown); employee-city mapping nahi.
9. **Admin feedback par reply** nahi jata — sirf "reviewed" flag.
10. **Password reset / email confirmation** flow nahi hai.
11. **No automated tests** — repo mein test project nahi; verification manual HTTP + DB se hoti hai.
12. **Config ek hi file** (`appsettings.json`) — doosri machine par connection string update karo.
13. Demo data seeded hai; test karne se bills/payments/orders barhte hain. Fresh start ke liye
    database drop karke app dobara chalao (seeder phir bana dega).

---

## 26) 5-minute demo script

1. `customer@nexos.local` → **Pay a Bill** → method + reference → submit → `PendingVerification`
2. `accounts@nexos.local` → bell / Payments → **Confirm** → bill due kam
3. Customer panel → **My Bills** → due kam, history mein `Recorded`
4. `retail@nexos.local` → Orders → `Pending` order → **Verify**
5. `technical@nexos.local` → Feasibility → order → technician + **Feasible**
   → Connections → **Create** → Details → **Activate**
6. `accounts@nexos.local` → Bills → **Generate** us connection ka bill
7. Customer panel → Dashboard par naya due + **Pay now**
8. `admin@nexos.local` → Orders → Details → **Notify Customer** (Resend email)
9. Admin → **Feedback** → naya feedback mark reviewed
10. Admin → **Messages** → reply (email)

---

## 27) Troubleshooting

| Problem | Solution |
|---|---|
| App start par `Login failed` ya instance not found | `appsettings.json` connection string check karo (LocalDB vs SQLEXPRESS) |
| Login "Invalid email or password" | Employee active hai? Retail shop active hai? Customer active hai? |
| Account locked | 5 ghalat attempts → 15 min wait |
| Page 404 | Panel ke andar unknown URL dashboard par fallback karta hai; record na ho to asli 404 |
| Payment button nahi dikh raha | Bill ka due 0 hai, ya us bill par pending payment hai |
| Accounts bill generate nahi kar raha | Connection Active/TemporarilyInactive nahi, ya period pichle bill ke baad nahi |
| Technical ko order nahi dikh raha | Retail ne verify nahi kiya |
| Connection create nahi ho raha | Feasibility `Feasible` nahi hai |
| Connection activate nahi ho raha | Linked order `ConnectionProvided` nahi hai |
| Email reply fail | `Email:ApiKey` / `RESEND_API_KEY` set nahi, ya app Production mode mein hai |
| Order notification par `422 Unprocessable Entity` | `onboarding@resend.dev` sirf owner ke email par jata hai; domain verify karke `Email:From` set karo |
| Admin Messages search/filters kaam nahi kar rahe | Known gap (section 8.13) — abhi sirf reply/details/mark-read chalta hai |

---

## Appendix — ye doc dobara audit kaise karein

Routes ki poori list nikalne ke liye:

```bash
cd "Nexos Network System"
for f in $(find Areas Controllers -name "*Controller.cs" | sort); do
  echo "=== $f"
  grep -nE "public (async )?(Task<)?IActionResult" "$f"
done
```

Har panel ke views:

```bash
find Areas -path "*/Views/*" -name "*.cshtml" | sort
```

Rules/validation messages (jo UI par dikhte hain):

```bash
grep -rn "AddModelError\|TempData\[\"Error\"\]\|InvalidOperationException(" Areas Services
```

---

**End of guide.**
