using System.Data.SqlClient;

namespace MictcoWebService.Common
{
    /// <summary>
    /// ADO.NET SQL Server connection pool settings.
    /// Pooling is enabled, the pool holds at most 50 connections,
    /// and opening a connection waits up to 5 minutes.
    /// </summary>
    public static class SqlConnectionPool
    {
        public const int MaxPoolSize = 50;
        public const int ConnectionTimeoutSeconds = 4 * 60;

        public static string Apply(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                MaxPoolSize = MaxPoolSize,
                ConnectTimeout = ConnectionTimeoutSeconds
            };

            return builder.ConnectionString;
        }
    }
}
