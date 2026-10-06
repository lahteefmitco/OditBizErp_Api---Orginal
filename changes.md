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
