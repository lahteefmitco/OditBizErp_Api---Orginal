# Changes

Connection pooling is enabled for SQL Server. Each pool allows at most 50 connections. Opening a connection waits up to 5 minutes (300 seconds).

## Files

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

## Connection release

Repeated calls were leaving SQL connections checked out, so later requests waited the full 5-minute pool timeout. Connections are now returned to the pool when the request finishes. `get-tickets` also closes its connection as soon as the query completes.

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

If the list is still slow after publish, add this index on the company database:

```sql
CREATE NONCLUSTERED INDEX IX_inv_service_status_history_status_ticket_date
ON dbo.inv_service_status_history (ssh_status, ssh_ticket_id, ssh_changed_date);
```

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

Publish this build to IIS before calling these URLs on `mictcoserver2`.
