# Nexos Network System — Complete Workflow Guide

> Ye file project ka poora map hai: project kaise chalta hai, har panel ka kaam kya hai,
> order kahan se aata hai, kaun kise receive karta hai, billing kaise banti hai aur payment
> kaise receive/verify hoti hai.

---

## 1) Project kya hai

Nexos Network System ek **ASP.NET Core MVC (.NET 10)** application hai jo internet / telephony
service business chalati hai:

- Customer registration aur self-service panel
- Service order (naya connection / plan change / equipment)
- Retail branch verification
- Technical feasibility check aur installation
- Connection activation
- Billing (12.24% service tax ke saath)
- Payments (online + branch counter) aur accounts verification
- Inventory / stock / equipment
- Contact messages aur customer feedback
- Role-based panels: **Admin, Retail, Technical, Accounts, Customer**

**Stack:** ASP.NET Core MVC, C# 13, Entity Framework Core 10, SQL Server, ASP.NET Identity
(roles), Razor views + shared panel design system, Resend (sirf contact-message reply email).

---

## 2) Project kaise chalayein

### Requirements
- .NET 10 SDK
- SQL Server (`localhost\SQLEXPRESS`)
- Database: `NexosNetworkSystem` (pehli baar app khud migrate + seed karta hai)

### Database connection
`Nexos Network System/appsettings.json` mein connection string hai:

```
Server=localhost\SQLEXPRESS;Database=NexosNetworkSystem;User Id=dbuser;Password=dbpassword;...
```

Agar `dbuser` login SQL Server par mojood na ho to app start par "Login failed" dega.
Do options hain:

**Option A — Windows authentication (simple):**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=NexosNetworkSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

**Option B — SQL login banao:**
```sql
CREATE LOGIN dbuser WITH PASSWORD='dbpassword', CHECK_POLICY=OFF;
USE NexosNetworkSystem;
CREATE USER dbuser FOR LOGIN dbuser;
ALTER ROLE db_owner ADD MEMBER dbuser;
```

### Run
```bash
cd "Nexos-Network-Marketing/Nexos Network System"
dotnet run
```

| Profile | URL |
|---|---|
| http | http://localhost:5051 |
| https | https://localhost:7135 |

Start hote hi `DbSeeder`:
1. Migrations apply karta hai
2. Roles banata hai: Admin, Retail, Technical, Accounts, Customer
3. Demo cities, retail shops, plans, products, stock, equipment, customers seed karta hai
4. 5 demo logins banata hai
5. Sample orders, feasibility reports, connections, bills aur payments seed karta hai

### Demo logins (password sab ka: `Nexos@123`)

| Role | Email | Login ke baad panel |
|---|---|---|
| Admin | `admin@nexos.local` | `/Admin` |
| Retail | `retail@nexos.local` | `/Retail` |
| Technical | `technical@nexos.local` | `/Technical` |
| Accounts | `accounts@nexos.local` | `/Accounts` |
| Customer | `customer@nexos.local` | `/Customer` |

Login rules:
- Employee roles (Admin/Retail/Technical/Accounts) ke liye **active Employee record** zaroori hai.
- Retail ke liye employee ki **active retail shop** bhi zaroori hai.
- Customer ke liye login ka **active Customer record** se link hona zaroori hai.
- Naya customer public site ke **Register** page se banta hai (Customer record + login dono).

---

## 3) Folder structure

```
Nexos-Network-Marketing/                 <- git repo root
├── README.md
├── PROJECT-WORKFLOW.md                  <- ye file
├── Nexos Network System.slnx
├── .gitignore
└── Nexos Network System/                <- ASP.NET project
    ├── Program.cs                       <- DI, auth, routing, seeding
    ├── appsettings.json                 <- connection string + email config
    ├── Areas/
    │   ├── Admin/                       <- Admin panel (controllers + views)
    │   ├── Retail/                      <- Retail panel
    │   ├── Technical/                   <- Technical panel
    │   ├── Accounts/                    <- Accounts panel
    │   ├── Customer/                    <- Customer panel
    │   └── Identity/                    <- Login / Register / Profile / Logout
    ├── Controllers/                     <- public site: Home, profile base
    ├── Data/                            <- DbContext, migrations, seeder
    ├── Helpers/                         <- order workflow, status badges, paging
    ├── Interfaces/                      <- service contracts
    ├── Models/                          <- entities + enums
    ├── Services/                        <- order, billing, payment, stock, ids, email
    ├── ViewModels/                      <- form/index models
    ├── ViewComponents/                  <- panel bell notifications
    ├── Views/                           <- public site + shared panel layout
    └── wwwroot/                         <- CSS, JS, icons, images
```

---

## 4) Roles aur unka scope

| Role | Panel URL | Kya kar sakta hai | Data scope |
|---|---|---|---|
| Admin | `/Admin` | Poora system manage | Sab kuch |
| Retail | `/Retail` | Apni shop ke customers/orders verify | Sirf apni retail shop |
| Technical | `/Technical` | Retail ke forward kiye orders, feasibility, connections, equipment | Forwarded orders |
| Accounts | `/Accounts` | Bills, payments, dues verify | Sab billing data (read-only customers/connections) |
| Customer | `/Customer` | Apne orders, connection, bills, payments, feedback | Sirf apna record |

Har area `[Authorize(Roles = "...")]` se protected hai; customer apna data customerId se filter
karke dekhta hai, retail apni shopId se.

---

## 5) Public website (bina login)

| Page | Route | Kaam |
|---|---|---|
| Home | `/` | Company intro, services, CTA |
| Services | `/Home/Services` | Services list |
| Plans | `/Home/Plans` | Active plans, price, cycle |
| Plan Details | `/Home/PlanDetails/{id}` | Plan detail + order CTA |
| About | `/Home/About` | Company info |
| Contact | `/Home/Contact` | Contact form -> Admin Messages |
| Track Order | `/Home/TrackOrder` | Order number se status |
| Connection Status | `/Home/ConnectionStatus` | Account number se connection status |
| Register | `/Identity/Account/Register` | Naya customer + login |
| Login | `/Identity/Account/Login` | Sab roles ke liye common login |

Track Order aur Connection Status public hain — customer apna order/connection bina login bhi
check kar sakta hai.

---

## 6) Har panel ka kaam (detail)

### 6.1 Admin — `/Admin`

| Menu | Kaam |
|---|---|
| Dashboard | Orders, revenue, connections, stock, messages ka overview |
| Employees | Admin/Retail/Technical/Accounts staff create/edit/delete; retail employee ko shop assign |
| Customers | Customer + addresses create/edit/delete |
| Retail Shops | City-wise branches manage |
| Vendors | Equipment/product vendors |
| Products | Products (price, category, active/inactive) |
| Stock | Stock adjust (In/Out/Adjustment), movement history, low-stock alert |
| Plans | Plans create/edit/activate; connection type, cycle, recurring charge, deposit, installation charge |
| Orders | Sab orders ki list + details + status update |
| Connections | Sab connections + status change |
| Billing | Kisi bhi eligible connection ka bill generate + bill details |
| Payments | Sab payments, filters, **export** |
| Messages | Public contact form ke messages; reply email (Resend) |
| Advanced Search | Customer / order / account ek jagah search |

Admin **order create nahi karta** — orders customer ya retail se aate hain. Admin unhe monitor
karta hai aur zaroorat par status change karta hai.

### 6.2 Retail — `/Retail`

| Menu | Kaam |
|---|---|
| Dashboard | Apni shop ke customers, orders, connections, dues, payments |
| Customers | Apne city ke customers add/list/details |
| Orders | Apni shop ke orders: **Verify**, **Reject**, **Request Info**, **Track**, **Create (walk-in)** |
| Plans | Active plans (read-only) |
| Connections | Apni shop ki connections (read-only list/details) |
| Bills | Apni shop ke bills (read-only) |
| Payments | Apni shop ki payments (read-only) |

Retail order par 3 decisions:
- **Verify** -> order technical ko forward (`VerifiedByRetail`)
- **Reject** -> wajah ke saath (`RetailRejected`)
- **Request Info** -> customer se correction (`AwaitingCustomerInformation`)

### 6.3 Technical — `/Technical`

| Menu | Kaam |
|---|---|
| Dashboard | Forwarded jobs, pending site checks, pending installations, equipment |
| Orders | Sirf retail ke forward kiye hue orders (queue) |
| Feasibility | Technician assign + site check record (Feasible / Not Feasible) |
| Connections | Feasible order se connection create, install, **activate**, status update |
| Equipment | Available equipment assign, return, repair bhejna, repair complete |

Technical ko retail ke **bagair verify kiye order nahi milta**. Feasibility ke bina connection
nahi banta. Connection activate hone ke baad hi accounts bill bana sakta hai.

### 6.4 Accounts — `/Accounts`

| Menu | Kaam |
|---|---|
| Dashboard / bell | Bills with dues + **online payments to verify** |
| Bills | Eligible connection ka bill generate, bill details, payments history |
| Payments | Manual payment record + customer online payments **Confirm / Reject** |
| Outstanding Dues | Customer-wise baqi amount, overdue filter |
| Customers | Billing view (read-only) |
| Connections | Billing view (read-only) |

### 6.5 Customer — `/Customer`

| Menu | Kaam |
|---|---|
| Dashboard | Connections, outstanding dues (Pay now alert), pending orders, payments |
| Profile | Apni details edit |
| My Orders | Naya order (plan + notes) aur order details |
| My Connection | Connection status, plan, dates |
| Current Plan | Plan details |
| My Equipment | Assigned equipment |
| My Bills | Sab bills + **Pay now** |
| Payment History | Har payment + status (Recorded / PendingVerification / Cancelled) |
| Pay a Bill | Bill select -> amount -> method -> reference -> submit |
| Feedback | Rating + message (connection ke saath optional) |

---

## 7) Order kahan se aata hai (order methods)

### Method 1 — Customer self-service (customer panel)
1. Customer login -> **My Orders -> New Order**
2. Active plan select + installation notes
3. Submit

System:
- Order `Pending` banata hai
- Customer ke primary address ke **city wali retail shop** ko auto-assign karta hai
- Order number banata hai: `B`/`T`/`D` + 10-digit sequence (e.g. `B0000000001`)

Customer order mein products select nahi hote — sirf plan + notes.

### Method 2 — Retail walk-in order (retail panel)
1. Retail login -> **Orders -> Create**
2. Customer, plan, connection type, optional products select
3. Submit

System:
- Order us shop ko assign karta hai
- Selected products ka stock **reserve** karta hai
- Order `Pending` banata hai

### Method 3 — Public plan page
Public **Plan Details** page se order CTA customer ko login/register par le jata hai, phir
Method 1 wala flow chalta hai.

> Admin order create nahi karta. Admin sirf monitor/status update karta hai.

---

## 8) Order statuses aur transitions

| Status | Matlab |
|---|---|
| `Pending` | Order aaya, retail verification baqi |
| `AwaitingCustomerInformation` | Retail ne customer se correction mangi |
| `RetailRejected` | Retail ne order reject kiya (wajah recorded) |
| `VerifiedByRetail` | Retail verified, technical ke paas |
| `FeasibilityPending` | Technical ne site visit assign ki, check baqi |
| `Feasible` | Site check pass, connection ban sakta hai |
| `NotFeasible` | Site check fail |
| `ConnectionProvided` | Connection ban gaya / install ho gaya (final) |
| `Cancelled` | Order cancel (stock release) |

Allowed transitions:

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

---

## 9) Complete end-to-end workflow (kaun kise kya bhejta hai)

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
                              Confirm -> bill Paid/Due update + receipt
                              Reject  -> bill waisa hi, reason record
```

### Step-by-step

| # | Kaun | Kahan | Kya karta hai | System kya karta hai |
|---|---|---|---|---|
| 1 | Customer | Customer -> My Orders -> New Order | Plan + notes | Order `Pending`; city wali retail shop auto-assign |
| 2 | Admin | Admin -> Orders | Order dekh sakta hai | Order list/details |
| 3 | Retail | Retail -> Orders -> Details | Customer/address check karke **Verify** | `VerifiedByRetail`; technical queue mein |
| 4 | Technical | Technical -> Orders / Feasibility | **Assign & check**: technician select + result | `FeasibilityPending` (agar status Pending) ya direct `Feasible` |
| 5 | Technical | Feasibility -> Edit | Distance, server available, remarks update | Order status report ke mutabiq |
| 6 | Technical | Connections -> Create | Feasible order se connection | Account number generate; order `ConnectionProvided` |
| 7 | Technical | Connection Details | Equipment assign + **Connection secured - activate** | Connection `Active`, `ActivatedAt` set |
| 8 | Accounts | Bills -> Generate | Period, charges, discount | Bill + items; 12.24% tax; due date = period end + 14 din |
| 9 | Customer | Dashboard / My Bills | Due amount + **Pay now** | Outstanding due dikhta hai |
| 10 | Customer | Pay a Bill | Bill, amount, method, reference | Payment `PendingVerification`; bill abhi change nahi |
| 11 | Accounts | Payments -> Review | **Confirm** ya **Reject** | Confirm: bill paid/due/status update + receipt. Reject: bill unchanged |
| 12 | Customer | Payment History | Status + receipt | `Recorded` payment, updated due |

### Handoff table (kis panel ko kya receive hota hai)

| Sender | Receiver | Kya receive hota hai | Receiver kya karta hai |
|---|---|---|---|
| Customer | Retail (+ Admin view) | Naya order | Verify / Reject / Request info |
| Retail | Technical | Verified order | Technician assign + site check |
| Technical | Technical | Feasibility result | Feasible -> connection create |
| Technical | Accounts | Active connection | Bill generate |
| Accounts | Customer | Bill + due amount | Online payment |
| Customer | Accounts | Payment (`PendingVerification`) | Confirm / Reject |
| Accounts | Customer | Confirmation / rejection | Payment history update |
| Public visitor | Admin | Contact message | Reply / mark read |
| Customer | Customer panel (DB) | Feedback | — |

---

## 10) Technical workflow (detail)

### 10.1 Queue
Technical panel sirf in statuses wale orders dikhata hai:
`VerifiedByRetail`, `FeasibilityPending`, `Feasible`, `ConnectionProvided`.

Retail ke paas pade (`Pending`), reject hue, ya `AwaitingCustomerInformation` orders technical
ko nahi dikhte.

### 10.2 Feasibility (site check)
Technical order par **Assign & check** karta hai:
- **Assigned technician** select (active Technical employees)
- **Status**: `Pending` (visit scheduled), `Feasible`, ya `NotFeasible`
- Distance (meters), server/network available (yes/no), remarks

Save hone par:
- Feasibility report banti hai (technician ke saath)
- Order status report ke mutabiq: `FeasibilityPending` / `Feasible` / `NotFeasible`
- Status history entry banti hai

`Feasible` report ke baad hi connection create ho sakta hai.

### 10.3 Connection create
Form:
- Feasible order select (jis ka connection pehle se na ho)
- `Activate immediately` (optional)
- Installation notes

System:
- Account number: `B`/`T`/`D` + 3-digit city code + 12-digit serial = 16 chars
  (e.g. `B001000000000001`)
- Connection status: `Pending` (ya `Active` agar immediately activate)
- `InstalledAt` set
- Order `ConnectionProvided`
- Connection plan history entry

### 10.4 Activation
Connection details par:
- Pending connection -> **Connection secured - activate**
- Notes optional
- Activate hone par status `Active`, `ActivatedAt` set

**Accounts sirf Active ya TemporarilyInactive connection ka bill bana sakta hai** — is liye
activation hi technical se accounts ka handoff hai.

### 10.5 Equipment
- Available equipment ko connection par assign
- Return -> wapas Available
- Repair bhejna -> `UnderRepair`
- Repair complete -> `Available` / `Retired`

Equipment statuses: `Available`, `Assigned`, `UnderRepair`, `Retired`.

---

## 11) Billing rules (Accounts)

### Eligible connections
- `Active`
- `TemporarilyInactive`

Pending / permanently inactive / cancelled connection ka bill nahi banta.

### Bill calculation
```
subtotal   = monthly rental + equipment charges + replacement charges
discounts  = discount + scheme discount
taxable    = max(subtotal - discounts, 0)
service tax = round(taxable * 12.24%, 2)
total      = taxable + service tax
due        = total + previous balance
```

- **Service tax:** 12.24% (constant)
- **Due date:** billing period end + 14 din
- **Previous balance:** pichle bill ka due amount carry forward hota hai
- **Bill number:** `B-{connectionId}-{start:yyyyMMdd}-{end:yyyyMMdd}`
- Naya bill period pichle bill ke period ke baad hi start ho sakta hai

Demo (seed) bill: `BILL-B001000000000001-202609` total 1683.60, paid 1000, due 683.60.

### Bill generate karna
1. Accounts -> Bills -> **Generate**
2. Connection select
3. Billing period start/end
4. Equipment / replacement charges, discount, scheme discount
5. Generate

Bill statuses: `Unpaid`, `PartiallyPaid`, `Paid`, `Cancelled`.

Customer ko bill turant us ke panel/dashboard par dikhta hai (dashboard alert + bell +
My Bills). Alag "send notice" button nahi hai; bill generate hona hi notification hai.

---

## 12) Payment methods aur verification

### 12.1 Customer online payment (Customer panel)

Route: `/Customer/Payments/Pay`

| Method | Reference zaroori? |
|---|---|
| Card (Debit / Credit) | Haan |
| Bank Transfer | Haan |
| Mobile Wallet (JazzCash / EasyPaisa) | Haan |
| Cheque (branch deposit) | Haan |

- Bill sirf apna aur **latest bill** ho sakta hai
- Amount due se zyada nahi
- Ek bill par ek waqt mein **ek hi pending online payment**
- Same transaction reference dobara use nahi ho sakta

Submit hone par payment `PendingVerification` banta hai. **Bill abhi change nahi hota.**

### 12.2 Manual payment (Accounts counter)

Route: `/Accounts/Payments/Create`

| Method |
|---|
| Cash |
| Card |
| Bank Transfer |
| Cheque |
| Other |
| Mobile Wallet |

Manual record karte waqt:
- Amount > 0 aur due se zyada nahi
- Reference duplicate nahi
- Sirf latest bill par
- Agar us bill par online payment pending ho to manual record block hota hai (pehle confirm/reject karo)

Record hone par bill foran update hota hai (Paid/PartiallyPaid), receipt number `RCP-...`.

### 12.3 Payment statuses

| Status | Matlab |
|---|---|
| `Recorded` | Paisa confirm, bill par apply |
| `PendingVerification` | Customer ne submit kiya, accounts confirmation baqi |
| `Cancelled` | Reject/cancel, bill par apply nahi |
| `Reversed` | Reversal (model mein mojood) |

### 12.4 Confirm / Reject (Accounts)

Route: `/Accounts/Payments` (filter: PendingVerification) ya payment details

- **Confirm**:
  - Re-check latest bill aur due
  - Amount bill par apply
  - Bill `Paid` / `PartiallyPaid`
  - Payment `Recorded`, naya receipt number `RCP-...`
  - Verification note save
- **Reject**:
  - Wajah required
  - Payment `Cancelled`
  - Bill ka due/paid bilkul waisa hi
  - Rejection note save

Bell par "Online payments to verify" count aata hai.

---

## 13) Customer panel ka payment workflow

1. Login -> Dashboard: outstanding amount alert + **Pay now**
2. **My Bills** -> due bill par **Pay now** (ya **Pay a Bill** page)
3. Bill select -> amount -> method -> reference -> submit
4. Payment History mein `PendingVerification`
5. Bill details par "awaiting confirmation" banner
6. Accounts confirm kare -> Payment History mein `Recorded`, bill due kam
7. Reject ho -> `Cancelled`, bill waisa hi, customer dobara pay kar sakta hai

---

## 14) Notifications

Panel ke top-right bell (role ke hisaab se):

| Panel | Bell items |
|---|---|
| Admin | Pending orders, low stock, unread messages |
| Retail | Pending orders, bills with dues |
| Technical | New jobs from retail, feasibility pending, connections awaiting installation |
| Accounts | Online payments to verify, bills with dues, unpaid bills |
| Customer | Unpaid bills, orders in progress, payments awaiting confirmation |

Email:
- Sirf Admin -> Messages -> **Reply** par Resend se email jata hai
- Config: `Email:ApiKey` ya env `RESEND_API_KEY`; `Email:From`, `Email:FromName`
- Key na ho to reply fail hota hai aur admin ko error dikhta hai; baqi system chalta rehta hai

---

## 15) Stock / inventory rules

- Order create par selected products ka stock **reserve** hota hai
- Order cancel par reserved stock **release** hota hai
- Available stock = `QuantityOnHand - QuantityReserved`
- Stock movement types: `In`, `Out`, `Adjustment`, `AdjustmentDecrease`
- Out movement available stock se zyada nahi ho sakta
- Adjustment reserved quantity se neeche nahi le ja sakta
- Physical issue/deduct Admin -> Stock -> **Adjust** se hota hai (automatic deduction order
  complete par nahi hoti — ye ek known limitation hai)

---

## 16) URL cheat sheet

### Public
```
/                              Home
/Home/Services                 Services
/Home/Plans                    Plans
/Home/PlanDetails/{id}         Plan details
/Home/About                    About
/Home/Contact                  Contact form
/Home/TrackOrder               Order tracking
/Home/ConnectionStatus         Connection status
/Identity/Account/Register     Customer registration
/Identity/Account/Login        Login
```

### Panels
```
/Admin/Dashboard  /Admin/Employees  /Admin/Customers  /Admin/RetailShops
/Admin/Vendors    /Admin/Products   /Admin/Stock      /Admin/Plans
/Admin/Orders     /Admin/Connections /Admin/Bills     /Admin/Payments
/Admin/Messages   /Admin/Search

/Retail/Dashboard /Retail/Customers /Retail/Orders    /Retail/Plans
/Retail/Connections /Retail/Bills   /Retail/Payments

/Technical/Dashboard /Technical/Orders /Technical/Feasibility
/Technical/Connections /Technical/Equipment

/Accounts/Dashboard /Accounts/Bills /Accounts/Payments /Accounts/Dues
/Accounts/Customers /Accounts/Connections

/Customer/Dashboard /Customer/Profile /Customer/Orders /Customer/Orders/Create
/Customer/Connection /Customer/Plan /Customer/Equipment /Customer/Bills
/Customer/Payments /Customer/Payments/Pay /Customer/Feedback
```

---

## 17) 5-minute demo script

1. `customer@nexos.local` login -> **Pay a Bill** -> method + reference -> submit
   -> Payment History mein `PendingVerification`
2. `accounts@nexos.local` login -> bell / Payments -> **Review online payments** -> **Confirm**
3. Customer panel -> **My Bills** -> due kam, history mein `Recorded`
4. `retail@nexos.local` -> Orders -> `Pending` order -> **Verify**
5. `technical@nexos.local` -> Feasibility -> order -> technician assign + **Feasible**
   -> Connections -> **Install & activate** -> connection details -> **Connection secured - activate**
6. `accounts@nexos.local` -> Bills -> **Generate** us connection ka bill
7. Customer panel -> Dashboard par naya due + **Pay now**

---

## 18) System-enforced rules (short list)

- Technical ko retail-verified order ke bagair kaam nahi milta
- Feasibility `Feasible` ke bagair connection nahi banta
- Sirf Active/TemporarilyInactive connection billable hai
- Bill sirf latest bill par pay hota hai; due se zyada amount reject
- Ek bill par ek hi pending online payment
- Payment reference duplicate nahi
- Customer sirf apne bill/connection/order dekh sakta hai
- Retail sirf apni shop ka data dekh sakta hai
- Cancel par reserved stock release
- Har payment/order status change history mein record hota hai (order status history)

---

## 19) Limitations / notes

1. **Payment gateway nahi hai** — online payment submit hoti hai, accounts manually confirm karta
   hai. Real gateway (JazzCash/Stripe) integrate karna ho to `IPaymentService` ke
   `SubmitOnlinePaymentAsync` ko gateway callback se replace karna hoga.
2. **Email sirf contact reply** ke liye hai (Resend). Bill/payment emails nahi jate.
3. **Admin feedback page nahi** — customer feedback DB mein save hota hai aur customer panel mein
   dikhta hai; admin ke liye dedicated list nahi hai.
4. **Stock auto-deduct nahi hota** connection/installation par; manually Stock -> Adjust karna
   hota hai.
5. **Technician assignment manual** hai (dropdown); employee-city mapping nahi hai.
6. **Database login** (`dbuser`) is machine par missing ho sakta hai — Section 2 dekho.
7. Demo data seeded hai; test karne se bills/payments/connections add ho jate hain. Fresh start ke
   liye database drop karke app dobara chalao (seeder phir bana dega).

---

## 20) Troubleshooting

| Problem | Solution |
|---|---|
| App start par `Login failed for user 'dbuser'` | Section 2 ka Option A/B apply karo |
| Login "Invalid email or password" | Employee active hai? Retail shop active hai? Customer active hai? |
| Page 404 | Panel ke andar unknown URL dashboard par fallback karta hai; record na ho to asli 404 |
| Payment button nahi dikh raha | Bill ka due 0 hai, ya us bill par pending payment hai |
| Accounts bill generate nahi kar raha | Connection Active/TemporarilyInactive nahi hai |
| Technical ko order nahi dikh raha | Retail ne verify nahi kiya |
| Connection create nahi ho raha | Feasibility `Feasible` nahi hai |
| Email reply fail | `RESEND_API_KEY` / `Email:ApiKey` set nahi hai |

---

**End of guide.**
