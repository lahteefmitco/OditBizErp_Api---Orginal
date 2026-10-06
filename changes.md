# Changes

Connection pooling is enabled for SQL Server. Each pool allows at most 100 connections. Opening a connection waits up to 2 minutes (120 seconds).

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
- `user-auth` and `user-locations` pass `validateUser: false`. The company token’s name identifier is an email (`cellcraft@rak`), not `gu_user_id`, so that database user check is skipped. Signature, issuer, audience, and expiry are still checked.
- `user-auth` (LoginController lines 174–183) validates the incoming company token again before copying the database claims into the user token.

Publish this build to IIS before calling these URLs on `mictcoserver2`. The site at `D:\OditBizErp_Api\New folder\...` is still running the old synchronous `getUserLocations`.
