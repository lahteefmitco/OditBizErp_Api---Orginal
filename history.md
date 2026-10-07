# History — 6 October 2026

Work on the OditBiz ERP API (`MictcoWebService`) after production timeouts and connection errors on `http://mictcoserver2.mictco.com:3040`. Data access stays ADO.NET. Stored procedures on customer databases were not altered.

## 1. Connection pool

Each SQL connection string uses a pool of at most 100 connections. Opening a connection waits up to 2 minutes (120 seconds).

- `MictcoWebService/Common/SqlConnectionPool.cs` — `MaxPoolSize = 100`, `ConnectionTimeoutSeconds = 120`. `Apply` sets `Pooling`, `MaxPoolSize`, and `ConnectTimeout`.
- `MictcoWebService/appsettings.json` — `ConnStr` and `ERPConnection` use the same limits.
- JWT company databases get their own pool, because each decrypted connection string is distinct.

## 2. Connections left checked out

Repeated calls left SQL connections out of the pool, so later requests waited the full pool timeout.

- `UserSqlServer.RegisterConnectionRelease` returns `shop` to the pool when the HTTP response finishes.
- `get-tickets` also closes its connection as soon as both queries finish, before the JSON is built.

## 3. `GET /get-tickets?status=Assigned` timed out

Flutter aborted with `DioException [receive timeout]` after 5 minutes and no HTTP status. The action does not send a response until the database call finishes.

`Sp_Service_Complaint_App` (`getTickets`) looked up the latest Assigned or Relocate date once per ticket on `inv_service_status_history`.

`ServiceAppController.GetTickets` (lines 2314–2581) no longer calls that procedure.

- Lines 2337–2408: one grouped query for the ticket list. `status` still filters `si_finish`. Date, ticket, customer, technician, and route filters are applied when the app sends them.
- Lines 2410–2426: complaint lines from `inv_lend_item_transactions` where `li_form` is `WORKORDER QUOTATION`.
- Lines 2619–2635: `AddTicketFilters` binds those parameters.
- Command timeout is 60 seconds.

If the list is still slow after publish, add this index on the company database:

```sql
CREATE NONCLUSTERED INDEX IX_inv_service_status_history_status_ticket_date
ON dbo.inv_service_status_history (ssh_status, ssh_ticket_id, ssh_changed_date);
```

## 4. Closed connection on service list endpoints

`GET /get-service-color` and `GET /get-service-brand` returned:

`BeginExecuteReader requires an open and available Connection. The connection's current state is closed.`

`OpenConnection()` swallowed the open failure, then the reader used `UserSqlServer.shop` after that shared connection was already closed.

`ReadServiceTableAsync` (lines 2587–2617) opens a connection for that request, runs the command, and disposes it when the read finishes.

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

`get-service-color` reads `inv_color` (`clr_id`, `clr_name`, `clr_remarks`) directly. The others still call `Sp_Service_Complaint_App` or `Sp_acc_reg` (cash and card).

File: `MictcoWebService/Controllers/ServiceAppController.cs`

## 5. Login endpoints held the connection across several queries

`GET /user-locations` from Flutter aborted with `DioException [connection timeout]` after 5 minutes. The action closed the SQL connection after the location query, then opened it again for the area and route settings. Each reopen could wait the full pool timeout.

`LoginController` now opens one connection for the whole action. Each query uses a 30-second command timeout.

- `user-auth` (lines 39–320): `OpenLoginConnectionAsync` (lines 392–409) and `Fill` (lines 423–433).
- `user-locations` (lines 324–356): locations, `AREAWISE LOGIN`, and `ROUTEWISE LOGIN` share that connection. Response keys stay `loactions`, `area`, and `route`.
- `check-route-wise-login` (lines 357–390): same pattern for `ENABLE SINGLE ROUTEWISE LOGIN`.
- `SettingIsOn` (lines 411–421): a missing or blank settings row means the flag is off.

## 6. `GET /user-locations` null reference

The live curl with a company bearer token threw:

`System.NullReferenceException` at `LoginController.getUserLocations()` line 327.

The IIS site was still running the old synchronous method (`SyncActionResultExecutor`). After `close()`, `dbReaderFill` returned null, and line 327 read `dt_settings.Rows`.

- `user-locations` (lines 324–356) is async, checks the setting with `SettingIsOn`, and returns HTTP 500 with the error message if the database call fails.
- `UserSqlServer.dbReaderFill` (lines 293–304) returns an empty `DataTable` when the connection does not open.

## 7. JWT checks

A bearer token is checked for issuer, audience, signature, and expiry (2-minute clock skew) before its claims are used.

File: `MictcoWebService/Common/UserSqlServer.cs`

- Constructor (lines 93–167): `ValidateToken` through `JwtValidationParameters` (lines 171–192). Database claims still select the company database.
- If the name identifier is a numeric user id greater than 0, `IsUserValid` (lines 2789–2840) checks `gnl_users.gu_active` on its own connection.
- `user-auth` and `user-locations` pass `validateUser: false`. The company token’s name identifier is an email, not `gu_user_id`, so that user-row check is skipped. Signature, issuer, audience, and expiry are still checked.
- `user-auth` (LoginController lines 174–183) validates the incoming company token again before copying the database claims into the user token.

## Publish

These changes are in source. They take effect on `mictcoserver2` only after this build is published to IIS. The error path `D:\OditBizErp_Api\New folder\...` was still the old synchronous `getUserLocations`.

---

# History — 7 October 2026

Connection pool exhaustion fix. Load test with 100 parallel `get-tickets?status=Assigned` requests showed 65 failures: `Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool. This may have occurred because all pooled connections were in use and max pool size was reached.`

## 8. CommandTimeout = 0 changed to 60

`UserSqlServer.cs` utility methods `dbExecute`, `dbReaderFill`, `dbScalar`, and `dbreadDataset` all used `CommandTimeout = 0` (infinite). A slow stored procedure or deadlock held a pool connection forever. Changed to 60 seconds across all 10 sites.

Also fixed in controllers: `CRMController.cs`, `SaleController.cs`, `ServiceAppController.cs`, `TestController.cs`, `ClientSqlServer.cs`.

Files: `MictcoWebService/Common/UserSqlServer.cs`, `MictcoWebService/Common/ClientSqlServer.cs`, and above controllers.

## 9. ConnectTimeout reduced from 120 to 15

`SqlConnectionPool.ConnectionTimeoutSeconds` changed from `2 * 60` (120 seconds) to `15`. When the pool is full, new requests now fail in 15 seconds instead of blocking a thread for 2 minutes, preventing cascading failure.

File: `MictcoWebService/Common/SqlConnectionPool.cs`

## 10. finally { close() } added to ServiceAppController

Audit found ~37 endpoints with `OpenConnection()` but no `finally` block. Connection was only returned to the pool by `Response.OnCompleted` -- not guaranteed on client abort or pipeline shortcircuit.

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

56 methods updated. `ReadServiceTableAsync` now passes `validateUser: false` to avoid redundant `IsUserValid` per call. `InsertSpare` `SqlCommand` wrapped in `using`.

Endpoints already safe (skipped): `GetTickets` (had `finally` with `ReleaseConnection`), `ApproveBillwiseReceipt`, `BulkApproveBillwiseReceipt`, `ServiceCollectionReport`, `ServiceOutstandingReport`, `PerformanceReportForm`, and all `ReadServiceTableAsync`-based endpoints.

File: `MictcoWebService/Controllers/ServiceAppController.cs`

## 11. finally { close() } added to DashboardController and DeliveryBoyController

Same pattern applied to all 18 methods in `DashboardController.cs` and all 23 methods in `DeliveryBoyController.cs`. None had `finally` blocks before.

Files: `MictcoWebService/Controllers/DashboardController.cs`, `MictcoWebService/Controllers/DeliveryBoyController.cs`

## Pending

- `CommonController.cs` (87 of 95 methods unsafe), `RPVController`, `WebErpController`, `SaleController`, `PurchaseController`, `InvoiceController`, `ProductController`, and remaining controllers still need `finally { close() }`.
- `CRMController.cs` line 253: `using (csqlre.shop)` disposes the connection object early. Needs replacement with `try/finally`.
- `RegistrationController.cs` line 21: same `using (csqlre.shop)` pattern.
- SQL index on `inv_service_status_history` not yet applied to customer databases.

## 12. Recommended SQL indexes for ServiceAppController queries

Run these on each **customer database**. They target the inline SQL in `ServiceAppController` that bypasses stored procedures. Check whether each index already exists before creating.

### 12a. inv_service_status_history — get-tickets subquery

The `get-tickets` query does `GROUP BY ssh_ticket_id` + `MAX(ssh_changed_date)` filtered by `ssh_status IN ('Assigned','Relocate')`. Without this index every call table-scans.

```sql
CREATE NONCLUSTERED INDEX IX_inv_service_status_history_status_ticket_date
ON dbo.inv_service_status_history (ssh_status, ssh_ticket_id, ssh_changed_date);
```

### 12b. inv_sales_inf — get-tickets main filter

The ticket list filters on `si_str_id = 12`, `si_coupon_no <> 0`, and `si_finish`. The `INCLUDE` columns let the query read from the index alone without a table lookup.

```sql
CREATE NONCLUSTERED INDEX IX_inv_sales_inf_service_tickets
ON dbo.inv_sales_inf (si_str_id, si_finish)
INCLUDE (si_coupon_no, si_date, si_entryno, si_cust_name, si_company,
         si_model, si_assign_to, si_remarks, si_deliverydate,
         si_return_entryno, si_color, si_acc_id);
```

### 12c. inv_lend_item_transactions — get-tickets lend items

Joined on `li_entryno = si_entryno AND li_form = 'WORKORDER QUOTATION'`. Without an index, every get-tickets call scans the full lend table.

```sql
CREATE NONCLUSTERED INDEX IX_inv_lend_item_transactions_entryno_form
ON dbo.inv_lend_item_transactions (li_entryno, li_form)
INCLUDE (li_in, li_out, li_remarks, li_ir_id, li_ir_mrp, li_ift_id);
```

### 12d. inv_sales_par — insert-delivery / update-delivery line lookup

`InsertDelivery` and `UpdateDelivery` query `inv_sales_par` per item row: `WHERE sp_str_id = 12 AND sp_entryno = ? AND sp_ir_id = ?`.

```sql
CREATE NONCLUSTERED INDEX IX_inv_sales_par_str_entry_item
ON dbo.inv_sales_par (sp_str_id, sp_entryno, sp_ir_id)
INCLUDE (sp_uniquecode, sp_rate, sp_realrate, sp_gross_value, sp_disc,
         sp_net_amount, sp_tax, sp_total, sp_igst, sp_cgst, sp_sgst,
         sp_mrp, sp_srate_multiunit, sp_prate, sp_realprate, sp_cost, sp_taxper);
```

### 12e. acc_account_transactions — outstanding balance

`InsertDelivery` and `UpdateDelivery` compute the balance: `WHERE CAST(at_date AS DATE) <= ? AND at_as_id = ?` with `SUM(at_Dr) - SUM(at_Cr)`. The `CAST` prevents a direct date seek, but the `at_as_id` lead column still narrows the scan to one customer.

```sql
CREATE NONCLUSTERED INDEX IX_acc_account_transactions_asid_date
ON dbo.acc_account_transactions (at_as_id, at_date)
INCLUDE (at_Dr, at_Cr);
```

### 12f. inv_verify_billwise_reciept_inf — collection report date range

`ServiceCollectionReport` filters `WHERE CAST(vbri_date AS DATE) BETWEEN @from AND @to` with optional `vbri_party_acc` and `vbri_location_id`.

```sql
CREATE NONCLUSTERED INDEX IX_inv_verify_billwise_reciept_inf_date
ON dbo.inv_verify_billwise_reciept_inf (vbri_date)
INCLUDE (vbri_id, vbri_cash_acc, vbri_party_acc, vbri_total,
         vbri_remarks, vbri_salesman, vbri_location_id, vbri_user_id, vbri_verified);
```

### 12g. inv_verify_billwise_reciept_pur — collection report detail join

```sql
CREATE NONCLUSTERED INDEX IX_inv_verify_billwise_reciept_pur_vbriid
ON dbo.inv_verify_billwise_reciept_pur (vbrp_vbri_id)
INCLUDE (vbrp_billno, vbrp_form, vbrp_bill_amount, vbrp_amount, vbrp_balance);
```

### Priority

| Index | Endpoint(s) | Impact |
| --- | --- | --- |
| 12a `inv_service_status_history` | get-tickets | High — removes full table scan on every call |
| 12b `inv_sales_inf` | get-tickets, get-tickets lend | High — covering index avoids key lookups |
| 12c `inv_lend_item_transactions` | get-tickets | High — avoids full lend table scan |
| 12d `inv_sales_par` | insert-delivery, update-delivery | Medium — per-item lookup in a loop |
| 12e `acc_account_transactions` | insert-delivery, update-delivery | Medium — SUM on large ledger table |
| 12f `inv_verify_billwise_reciept_inf` | collection report | Medium — date range scan |
| 12g `inv_verify_billwise_reciept_pur` | collection report, approve | Low — usually small result set |
