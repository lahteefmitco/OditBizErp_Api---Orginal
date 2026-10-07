# Changes

Connection pooling is enabled for SQL Server. Each pool allows at most 100 connections. Opening a connection now waits up to 15 seconds (was 120 seconds).

## Files changed (7 October 2026)

- MictcoWebService/Common/SqlConnectionPool.cs
- MictcoWebService/Common/UserSqlServer.cs
- MictcoWebService/Common/ClientSqlServer.cs
- MictcoWebService/Controllers/ServiceAppController.cs
- MictcoWebService/Controllers/DashboardController.cs
- MictcoWebService/Controllers/DeliveryBoyController.cs
- MictcoWebService/Controllers/CRMController.cs
- MictcoWebService/Controllers/SaleController.cs
- MictcoWebService/Controllers/TestController.cs

## Files changed (6 October 2026)

- MictcoWebService/Common/SqlConnectionPool.cs
- MictcoWebService/appsettings.json
- MictcoWebService/Startup.cs
- MictcoWebService/Common/ClientSqlServer.cs
- MictcoWebService/Common/UserSqlServer.cs
- MictcoWebService/Controllers/EcommerceController.cs
- MictcoWebService/Controllers/EcartController.cs
- MictcoWebService/Controllers/EcommerceLoginController.cs
- MictcoWebService/Controllers/TestConnectionController.cs
- MictcoWebService/Controllers/TestController.cs
- MictcoWebService/Controllers/ServiceAppController.cs
- MictcoWebService/Controllers/LoginController.cs

---

## Connection pool exhaustion fix (7 October 2026)

Load test with 100 parallel `get-tickets?status=Assigned` requests: 35 succeeded, 65 failed with `Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool. This may have occurred because all pooled connections were in use and max pool size was reached.`

Root cause: connections were held too long (infinite command timeout), not released on exceptions (no `finally` block), and the 120-second connect timeout made every blocked request hold a thread for 2 minutes.

### CommandTimeout = 0 (infinite) changed to 60 seconds

`UserSqlServer.cs` utility methods `dbExecute`, `dbReaderFill`, `dbScalar`, `dbreadDataset`, and all stored procedure helpers used `CommandTimeout = 0` (infinite wait). A slow SP or deadlock held a pool connection forever. Changed to 60 seconds everywhere.

Files: `UserSqlServer.cs` (10 sites), `ClientSqlServer.cs` (2 sites), `CRMController.cs`, `SaleController.cs`, `ServiceAppController.cs`, `TestController.cs`

### ConnectTimeout reduced from 120 to 15 seconds

`SqlConnectionPool.ConnectionTimeoutSeconds` changed from 120 to 15. When the pool is full, new requests now fail in 15 seconds instead of blocking a thread for 2 minutes.

File: `SqlConnectionPool.cs`

### finally { close() } added to ServiceAppController (56 methods)

Audit found ~37 endpoints with `OpenConnection()` but no `finally` block. Connection was only returned to the pool by `Response.OnCompleted` — not guaranteed when the client aborts or an exception occurs before close() is called.

Every endpoint that uses `UserSqlServer` + `OpenConnection()` now follows:

```csharp
UserSqlServer usqlre = null;
try
{
    usqlre = new UserSqlServer(this);
    usqlre.OpenConnection();
    // ... DB work ...
    return Ok(...);
}
catch (Exception ex)
{
    return StatusCode(500, new { status = false, message = ex.Message });
}
finally
{
    usqlre?.close();
}
```

56 methods updated. Old `usqlre.close()` calls removed from inside `try` bodies (the `finally` handles it).

Additional fixes in ServiceAppController:
- `ReadServiceTableAsync` now passes `validateUser: false` to skip redundant `IsUserValid` per call.
- `InsertSpare` `SqlCommand` wrapped in `using`.

Endpoints already safe (skipped): `GetTickets`, `ApproveBillwiseReceipt`, `BulkApproveBillwiseReceipt`, `ServiceCollectionReport`, `ServiceOutstandingReport`, `PerformanceReportForm`, and all `ReadServiceTableAsync`-based endpoints.

### finally { close() } added to DashboardController (18 methods)

All 18 methods had `close()` only on the success path inside `try`. Exception path never closed the connection. Added `finally { usqlre?.close(); }` to all 18.

### finally { close() } added to DeliveryBoyController (23 methods)

All 23 methods had the same pattern. Added `finally { usqlre?.close(); }` to all 23.

### Recommended SQL indexes for customer databases

Run on each customer database to speed up ServiceAppController queries. Faster queries = connections held for less time = less pool pressure.

**High priority (get-tickets):**

```sql
-- 12a: get-tickets subquery on status history (removes full table scan)
CREATE NONCLUSTERED INDEX IX_inv_service_status_history_status_ticket_date
ON dbo.inv_service_status_history (ssh_status, ssh_ticket_id, ssh_changed_date);

-- 12b: get-tickets main filter (covering index)
CREATE NONCLUSTERED INDEX IX_inv_sales_inf_service_tickets
ON dbo.inv_sales_inf (si_str_id, si_finish)
INCLUDE (si_coupon_no, si_date, si_entryno, si_cust_name, si_company,
         si_model, si_assign_to, si_remarks, si_deliverydate,
         si_return_entryno, si_color, si_acc_id);

-- 12c: get-tickets lend items join
CREATE NONCLUSTERED INDEX IX_inv_lend_item_transactions_entryno_form
ON dbo.inv_lend_item_transactions (li_entryno, li_form)
INCLUDE (li_in, li_out, li_remarks, li_ir_id, li_ir_mrp, li_ift_id);
```

**Medium priority (insert/update delivery, collection report):**

```sql
-- 12d: delivery save per-item lookup
CREATE NONCLUSTERED INDEX IX_inv_sales_par_str_entry_item
ON dbo.inv_sales_par (sp_str_id, sp_entryno, sp_ir_id)
INCLUDE (sp_uniquecode, sp_rate, sp_realrate, sp_gross_value, sp_disc,
         sp_net_amount, sp_tax, sp_total, sp_igst, sp_cgst, sp_sgst,
         sp_mrp, sp_srate_multiunit, sp_prate, sp_realprate, sp_cost, sp_taxper);

-- 12e: outstanding balance SUM
CREATE NONCLUSTERED INDEX IX_acc_account_transactions_asid_date
ON dbo.acc_account_transactions (at_as_id, at_date)
INCLUDE (at_Dr, at_Cr);

-- 12f: collection report date range
CREATE NONCLUSTERED INDEX IX_inv_verify_billwise_reciept_inf_date
ON dbo.inv_verify_billwise_reciept_inf (vbri_date)
INCLUDE (vbri_id, vbri_cash_acc, vbri_party_acc, vbri_total,
         vbri_remarks, vbri_salesman, vbri_location_id, vbri_user_id, vbri_verified);

-- 12g: collection report detail join
CREATE NONCLUSTERED INDEX IX_inv_verify_billwise_reciept_pur_vbriid
ON dbo.inv_verify_billwise_reciept_pur (vbrp_vbri_id)
INCLUDE (vbrp_billno, vbrp_form, vbrp_bill_amount, vbrp_amount, vbrp_balance);
```

### Pending (not yet applied)

- `CommonController.cs` (87 of 95 methods unsafe), `RPVController`, `WebErpController`, `SaleController`, `PurchaseController`, `InvoiceController`, `ProductController`, and remaining controllers still need `finally { close() }`.
- `CRMController.cs` line 253: `using (csqlre.shop)` disposes the connection object early. Needs replacement with `try/finally`.
- `RegistrationController.cs` line 21: same `using (csqlre.shop)` pattern.

---

## Connection release (6 October 2026)

Repeated calls were leaving SQL connections checked out, so later requests waited the full pool timeout. Connections are now returned to the pool when the request finishes. `get-tickets` also closes its connection as soon as the query completes.

- MictcoWebService/Common/UserSqlServer.cs
- MictcoWebService/Controllers/ServiceAppController.cs

## get-tickets was slower than the Flutter timeout

`GET /get-tickets?status=Assigned` stayed inside `Sp_Service_Complaint_App` (`getTickets`) for more than 5 minutes. Flutter aborted with `DioException [receive timeout]` and no HTTP status, because this action does not send a response until the database call finishes.

The procedure looked up the latest Assigned or Relocate date once per ticket on `inv_service_status_history`. That held a pooled connection long enough for later calls to wait as well.

`GetTickets` (lines 2314–2581) no longer calls that procedure.

- Lines 2337–2408: one grouped query for the ticket list. `status` still filters `si_finish`. Optional date, ticket, customer, technician, and route filters are applied when the app sends them.
- Lines 2410–2426: complaint lines for those same tickets, from `inv_lend_item_transactions` where `li_form` is `WORKORDER QUOTATION`.
- Lines 2619–2635: `AddTicketFilters` binds those query parameters.
- The connection is released as soon as both queries finish, before the JSON is built.

## Closed connection on service list endpoints

`GET /get-service-color` and `GET /get-service-brand` returned:

`BeginExecuteReader requires an open and available Connection. The connection's current state is closed.`

`OpenConnection()` swallowed the real open failure, then the reader ran on `UserSqlServer.shop` after that shared connection was already closed.

`ReadServiceTableAsync` (lines 2587–2617) opens a connection for that request only, runs the command, and disposes the connection when the read finishes. These actions use it:

| Endpoint | Line |
| --- | --- |
| `get-service-complaint` | 192 |
| `get-service-complaint-by-id/{id}` | 230 |
| `get-service-complaint-by-mobile/{mobile}` | 269 |
| `get-service-category` | 308 |
| `insert-brand` | 343 |
| `get-service-brand` | 379 |
| `insert-route` | 414 |
| `get-service-route` | 450 |
| `insert-color` | 485 |
| `get-service-color` | 520 |
| `insert-model` | 551 |
| `get-service-model` | 587 |
| `get-service-cash` | 623 |
| `get-service-card` | 658 |
| `get-qc-list` | 1677 |
| `get-qc-list-by-id/{id}` | 1716 |
| `get-items-collected` | 1893 |
| `get-items-collected-by-id/{id}` | 1927 |
| `description-list` | 1959 |
| `technicians` | 2036 |
| `technician-list` | 2695 |
| `get-service-item` | 5036 |

`get-service-color` (line 520) reads `inv_color` (`clr_id`, `clr_name`, `clr_remarks`) directly. The others still call the same stored procedures as before (`Sp_Service_Complaint_App` or `Sp_acc_reg` for cash and card).

File: `MictcoWebService/Controllers/ServiceAppController.cs`

## Login endpoints held the connection across several queries

`GET /user-locations` from Flutter aborted with `DioException [connection timeout]` after 5 minutes. The action closed the SQL connection after the location query, then opened it again for the area and route settings. Each reopen could wait the full pool timeout before any response was sent.

`LoginController` now opens one connection for the whole action and runs each query with a 30-second command timeout.

File: `MictcoWebService/Controllers/LoginController.cs`

- `user-auth` (lines 39–320): one connection from `OpenLoginConnectionAsync` (lines 392–409). Queries use `Fill` (lines 423–433).
- `user-locations` (lines 324–356): locations, `AREAWISE LOGIN`, and `ROUTEWISE LOGIN` share that connection. Response keys stay `loactions`, `area`, and `route`.
- `check-route-wise-login` (lines 357–390): same connection pattern for `ENABLE SINGLE ROUTEWISE LOGIN`.
- `SettingIsOn` (lines 411–421): a missing or blank settings row means the flag is off.

## user-locations null reference

The published `getUserLocations` was still synchronous. After `close()`, `dbReaderFill` returned null, and line 327 read `dt_settings.Rows`, which threw `System.NullReferenceException`.

- `user-locations` (lines 324–356) is async, checks the setting with `SettingIsOn`, and returns HTTP 500 with the error message if the database call fails.
- `UserSqlServer.dbReaderFill` (lines 293–304) returns an empty `DataTable` when the connection does not open, so `.Rows` is no longer called on null.

## JWT checks

A bearer token is checked for issuer, audience, signature, and expiry (2-minute clock skew) before its claims are used.

File: `MictcoWebService/Common/UserSqlServer.cs`

- Constructor (lines 93–167): `ValidateToken` through `JwtValidationParameters` (lines 171–192). Database claims still select the company database. If the name identifier is a numeric user id greater than 0, `IsUserValid` (lines 2789–2840) checks `gnl_users.gu_active` on its own connection.
- `user-auth` and `user-locations` pass `validateUser: false`. The company token's name identifier is an email (`cellcraft@rak`), not `gu_user_id`, so that database user check is skipped. Signature, issuer, audience, and expiry are still checked.
- `user-auth` (LoginController lines 174–183) validates the incoming company token again before copying the database claims into the user token.

Publish this build to IIS before calling these URLs on `mictcoserver2`. The site at `D:\OditBizErp_Api\New folder\...` is still running the old synchronous `getUserLocations`.
