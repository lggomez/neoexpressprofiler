namespace ExpressProfiler.DatabaseConnection
{
    using System;
    using System.Data.SqlClient;

    public static class Connections
    {
        public static SqlConnection GetConnection(string server, string user, string password, int authType)
        {
            return new SqlConnection
            {
                ConnectionString =
                authType == 0 ? String.Format(@"Data Source = {0}; Initial Catalog = master; Integrated Security=SSPI;Application Name=Express Profiler", server)
                : String.Format(@"Data Source={0};Initial Catalog=master;User Id={1};Password='{2}';;Application Name=Express Profiler", server, user, password)
            };
        }
    }
}