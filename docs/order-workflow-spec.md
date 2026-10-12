# NEXOS NETWORK MARKETING SYSTEM — COMPLETE ORDER WORKFLOW & DEPARTMENT PANELS IMPLEMENTATION

## 1. Project Context

I have an existing ASP.NET Core MVC project named **Nexos Network Marketing System**.

The system includes multiple departments, such as:

- Admin
- Retail
- Technical
- Accounts
- Inventory / Stock Management
- Installation / Field Technicians
- Customer Portal

Your task is to inspect the existing source code, understand its current architecture, and implement a complete, integrated customer order workflow beginning immediately after Retail accepts and verifies a customer order.

**Important:** Do not create a separate application or duplicate existing modules. Reuse and improve the existing controllers, models, services, database entities, views, authentication, and authorization wherever possible.

Before making changes, inspect the existing implementation and identify which features already exist, which are incomplete, and which need to be created.

---

## 2. Overall Business Workflow

The standard order workflow should be:

Customer Order → Retail Verification → Technical Feasibility → Accounts & Billing → Payment Verification → Inventory Allocation → Installation Assignment → Installation & Testing → Connection Activation → Customer Notification → Order Completion.

The exact order of billing, payment, and stock allocation must be configurable or clearly defined by business rules. Do not mark an order completed or a connection active prematurely.

Each department must have its own dashboard, permitted actions, and responsibilities.

## 3. Retail Department — Order Verification

Retail is responsible for accepting an assigned order and verifying customer information.

Retail responsibilities:

1. View orders assigned to its retail shop.
2. Accept or reject an assigned order with a reason.
3. Verify customer name, contact number, installation address, service area, selected service, and plan.
4. Review required application forms and address documents.
5. Identify missing or incorrect information.
6. Request corrections from the customer.
7. Submit verified orders to Technical for feasibility checking.
8. Track technical, financial, inventory, and installation progress.
9. Communicate order updates to the customer.
10. View the final outcome without accessing unauthorized financial or technical controls.

Retail must not approve its own technical feasibility, fabricate payments, or activate connections without explicit authorization.

Once Retail submits an order, the system should record the submission timestamp and the responsible employee.

## 4. Technical Department — Feasibility Management

### Purpose

Technical determines whether the requested service can actually be delivered at the customer's installation address.

### Technical dashboard

Provide these sections:

- New Feasibility Requests
- Assigned Requests
- In Progress
- Awaiting Customer Information
- Feasible
- Not Feasible
- Overdue Requests
- Completed Assessments

### Technical responsibilities

1. Receive verified orders from Retail.
2. Assign each request to an eligible technical employee based on service area, workload, and availability.
3. Review the requested service, installation address, and plan.
4. Check network coverage and service availability.
5. Check relevant network capacity, ports, and infrastructure availability.
6. Identify equipment or cabling requirements.
7. Record any technical limitations, additional requirements, or site-visit requirements.
8. Schedule a site survey when remote verification is insufficient.
9. Record findings, notes, supporting documents, and the responsible employee.
10. Submit the feasibility result for the appropriate technical approval.

### Feasibility statuses

- Pending
- Assigned
- In Progress
- Awaiting Information
- Site Survey Required
- Feasible
- Not Feasible
- Requires Reassessment

### Feasibility decision rules

**Feasible:** The requested connection can be provided, subject to the recorded conditions.

**Not Feasible:** The service cannot currently be provided. A reason must be recorded.

**Pending:** Assessment is not complete. Pending must never be treated as approval.

If a site survey or higher-level approval is required, the request must remain pending until that step is completed.

### Technical permissions

Technical employees may update assessments assigned to them. Only authorized technical supervisors may approve assessments where a separate approval is required.

Technical employees must not record customer payments, modify billing records, or activate connections unless separately authorized.

### After feasibility

- If Not Feasible: return the order to Retail for customer communication and the configured cancellation or alternative-service process.
- If Pending: keep the order open and assign follow-up responsibility.
- If Feasible: send the order to the next configured stage, normally Accounts and billing.

Save the decision, reason, employee, timestamps, and any approval history.

## 5. Accounts Department — Charges, Bills and Payments

### Purpose

Accounts manages the financial side of an order.

### Accounts dashboard

Include:

- Orders Awaiting Financial Review
- Bills to Generate
- Unpaid Bills
- Partially Paid Bills
- Paid Bills
- Overdue Payments
- Refund Requests
- Payment Verification
- Outstanding Customer Balances

### Accounts responsibilities

1. Receive technically feasible orders.
2. Verify the selected plan and applicable pricing.
3. Calculate installation charges, plan charges, security deposits, equipment charges, applicable taxes, and approved discounts.
4. Apply corporate or bulk-connection discounts only when eligibility has been verified.
5. Generate an itemized bill.
6. Provide the bill to the customer through the customer portal.
7. Record payments through the approved payment workflow.
8. Verify payment references or gateway confirmations.
9. Maintain payment history and outstanding balances.
10. Issue receipts.
11. Handle authorized adjustments and refunds.
12. Forward financially cleared orders to the next stage.

### Payment statuses

- Unpaid
- Pending Verification
- Partially Paid
- Paid
- Failed
- Cancelled
- Refund Pending
- Refunded

A bill must not be marked Paid simply because a customer submits a payment reference or uploads a screenshot. Verify the payment through the configured gateway or an authorized Accounts employee.

Prevent duplicate payment processing and ensure bill balances cannot become negative.

Use database transactions and appropriate validation for payment and bill updates.

### Financial clearance

The system must define which charges must be paid before installation.

If the order requires advance payment, installation cannot proceed until the configured payment condition is satisfied or an authorized exception is recorded.

Accounts must not perform technical feasibility assessments or activate network connections.

## 6. Inventory / Stock Management Department

### Purpose

Inventory ensures the required equipment is available and properly allocated before installation.

### Inventory dashboard

Include:

- Available Stock
- Reserved Stock
- Low Stock
- Out-of-Stock Items
- Equipment Required for Orders
- Stock Reservations
- Issued Equipment
- Returned Equipment
- Damaged Equipment
- Purchase Requests
- Vendor Purchase Orders
- Stock Movement History

### Inventory responsibilities

1. Receive approved equipment requirements from Technical or Installation.
2. Check available, reserved, and usable stock.
3. Reserve the required items against a specific order.
4. Prevent the same stock from being reserved for multiple orders.
5. Record equipment issued to the installation technician.
6. Track serial numbers and other identifiers where applicable.
7. Record equipment installation, return, damage, and replacement.
8. Create a purchase request when stock is insufficient.
9. Route purchase requests to authorized management or procurement staff.
10. Update inventory after verified receipts and stock movements.

### Stock rules

Available Stock = Usable Physical Stock − Reserved Stock, subject to the existing inventory model.

Never allow a reservation or issue that exceeds available stock.

If required equipment is unavailable, set the order to an appropriate waiting state such as Awaiting Stock. Notify the responsible department and provide an expected follow-up date where available.

Do not automatically declare an order impossible merely because stock is temporarily unavailable.

Inventory must not record customer payments or independently approve feasibility.

## 7. Installation / Field Technician Department

### Purpose

Installation personnel physically install and test the approved service at the customer’s premises.

### Installation dashboard

Include:

- Installation Jobs Awaiting Assignment
- Assigned Jobs
- Scheduled Visits
- Today’s Installations
- In Progress
- Awaiting Customer
- Awaiting Equipment
- Failed Installation Attempts
- Completed Installations
- Revisit Requests

### Installation responsibilities

1. Receive an installation job only when feasibility is approved.
2. Check financial clearance where required by business rules.
3. Confirm required equipment is reserved or issued.
4. Assign an authorized technician based on service area, skill, and availability.
5. Schedule an installation appointment.
6. Notify the customer of the appointment.
7. Record arrival, work performed, and installation results.
8. Record equipment serial numbers and actual items installed.
9. Complete the required technical tests.
10. Record signal, connectivity, and service verification results as applicable.
11. Upload authorized installation evidence or completion documents where required.
12. Record failed visits, reasons, and rescheduling requirements.
13. Submit the completed job for activation approval.

### Installation statuses

- Awaiting Assignment
- Assigned
- Scheduled
- In Progress
- Awaiting Customer
- Awaiting Equipment
- Failed Visit
- Revisit Required
- Installation Completed
- Awaiting Activation

Installation Completed does not automatically mean the customer connection is Active. Required tests and activation authorization must be completed first.

## 8. Connection Activation

### Purpose

Connection activation turns an approved, installed service into an operational customer connection.

### Activation conditions

Before activation, verify that:

1. Technical feasibility has been approved.
2. Required financial clearance is satisfied or an authorized exception exists.
3. Required equipment has been installed and recorded.
4. Installation tests have passed.
5. Required activation approval has been granted.
6. The customer account and selected plan are valid.

### Activation responsibilities

1. Generate or confirm the customer account and connection identifiers.
2. Link the connection to the correct customer, order, and plan.
3. Record the activation timestamp and responsible employee.
4. Set the connection status to Active only after all mandatory checks pass.
5. Start billing from the configured service commencement date.
6. Notify the customer that the connection is active.
7. Make the connection visible in the customer portal.

If network activation requires an external system or manual network configuration, integrate with that system where supported. Otherwise, provide a controlled manual activation workflow with audit records. Never falsely claim that the physical network has been activated when only a database status was changed.

## 9. Admin Panel — Central Oversight

Admin supervises the complete workflow but must not bypass validation or erase the history of departmental actions.

### Admin dashboard

Display:

- Total Orders
- Unassigned Orders
- Orders by Current Stage
- Pending Feasibility
- Not Feasible Orders
- Outstanding Bills
- Payments Awaiting Verification
- Stock Shortages
- Pending Installations
- Failed Installation Jobs
- Active Connections
- Overdue Tasks
- Department Workload

### Admin responsibilities

1. Monitor all orders and their current stages.
2. View the full order timeline.
3. Configure service areas and retail-shop coverage.
4. Manage staff, roles, permissions, and department assignments.
5. Configure escalation rules and workflow deadlines.
6. Resolve unassigned orders and workload imbalances.
7. Approve only those exceptions explicitly allowed by policy.
8. Review cancellations, reassignment requests, and disputes.
9. Monitor audit logs and department performance.
10. Manage approved service plans and product catalogues.
11. Monitor inventory and purchase requests.
12. View reports without modifying historical transactions improperly.

Any override must require permission, a reason, an audit entry, and appropriate validation. Admin must not be able to silently convert an unverified payment into a verified payment or bypass mandatory activation checks without an explicit, auditable exception policy.

## 10. Customer Portal — Order Visibility

Customers must be able to:

1. View submitted orders.
2. View the current order status and understandable status description.
3. See whether additional information is required.
4. Submit requested documents or corrections securely.
5. View feasibility outcomes and appropriate explanations.
6. View bills, due amounts, receipts, and payment history.
7. View installation appointment details.
8. View active connections, service plans, and account details.
9. Receive status notifications.
10. Submit a cancellation or support request according to business rules.

Customers must never see another customer’s personal details, documents, bills, or order history.

## 11. Standard Order Status Model

Implement a consistent, validated workflow. Adapt existing enums and entities instead of creating duplicate status systems.

Suggested statuses:

1. Submitted
2. Awaiting Retail Acceptance
3. Retail Verification
4. Awaiting Customer Information
5. Pending Feasibility
6. Feasibility In Progress
7. Awaiting Site Survey
8. Not Feasible
9. Feasible
10. Awaiting Financial Review
11. Awaiting Payment
12. Payment Verification Pending
13. Financially Cleared
14. Awaiting Stock
15. Equipment Reserved
16. Awaiting Installation Assignment
17. Installation Scheduled
18. Installation In Progress
19. Installation Failed / Revisit Required
20. Installation Completed
21. Awaiting Activation
22. Active
23. Completed

Cancellation, rejection, and reassessment should be handled through explicit states and documented transition rules.

Not every order needs every status. For example, if no site survey is required, skip that stage. The system must prevent invalid transitions, such as moving directly from Submitted to Active.

Each transition must record who changed the status, when it changed, and why where applicable.

## 12. Department Responsibilities and Access Control

Use role-based and, where needed, policy-based authorization.

- Retail: Assigned orders, customer verification, customer communication.
- Technical: Feasibility assignments, assessments, site surveys, technical findings.
- Accounts: Bills, payments, receipts, financial clearance.
- Inventory: Reservations, stock issues, returns, purchase requests.
- Installation: Assigned installation jobs, scheduling, test results.
- Activation staff: Validated connection activation.
- Admin: System configuration, oversight, approved exceptions.
- Customer: Own orders, own documents, own bills, own connections.

A role must not automatically receive access to every department’s write actions.

Enforce permissions on the server-side controllers and services, not just by hiding buttons in the interface.

## 13. Data Model and Audit Requirements

Inspect the current database schema before adding entities.

Reuse existing entities where suitable. Implement only missing relationships and fields.

The workflow must support, where needed:

- Order and customer relationship
- Retail assignment and reassignment history
- Technical feasibility assessment and findings
- Site-survey records
- Bill line items and financial clearance
- Payment records and verification
- Inventory reservations and movements
- Equipment issued to an order or installation
- Installation jobs and test results
- Connection activation records
- Customer notifications
- Status history and audit trail

Use foreign keys, appropriate indexes, database constraints, and transactions to protect data integrity.

Do not store sensitive customer documents in publicly accessible folders. Apply secure access controls and validate uploaded file types, sizes, and permissions.

## 14. Notifications and Escalations

Notify the relevant employee or department when:

- A new order is assigned.
- Retail submits a verified order.
- Feasibility is completed or requires more information.
- A bill is generated.
- A payment requires verification.
- Stock is insufficient.
- Installation is assigned or scheduled.
- Installation fails or requires a revisit.
- Activation is completed.
- An order remains overdue.

Notifications should be generated from actual workflow events. Do not show success notifications when the underlying database operation fails.

## 15. Required Implementation and Testing

Before declaring the project complete:

1. Inspect the existing codebase and specification.
2. Map each requirement to its existing or planned implementation.
3. Reuse existing services and avoid duplicated business logic.
4. Implement missing controllers, services, views, and database migrations as required.
5. Validate each status transition on the server.
6. Verify role-based authorization for every department.
7. Test the complete order journey from Retail acceptance through activation.
8. Test infeasible orders, missing documents, unpaid bills, duplicate payments, insufficient stock, failed installations, reassignment, and cancellations.
9. Test concurrent orders to ensure inventory cannot be double-reserved.
10. Verify customer data isolation.
11. Run the available build, migration, automated, and integration tests.
12. Document anything that cannot be tested because of unavailable infrastructure.

Do not claim a feature works unless it has been implemented and verified. Clearly distinguish existing functionality, newly implemented functionality, and unverified functionality.

## 16. Final Deliverables

Provide:

- Updated working source code.
- Updated database migrations where necessary.
- Department-specific dashboards and workflows.
- A documented order-status transition diagram.
- A role and permission matrix.
- A requirement-to-code mapping.
- Test results and unresolved defects.
- A prioritized list of remaining work.

**Final objective:** Every order must have a clear owner, a traceable status, a responsible department, validated next steps, and an auditable history from Retail acceptance to successful connection activation or a documented final outcome.