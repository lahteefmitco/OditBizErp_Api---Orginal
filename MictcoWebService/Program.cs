using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MictcoWebService
{
    public class Program
    {
        //
        public static void Main(string[] args)
        {
            // Prevent thread-pool starvation under burst load.
            // IIS in-process needs enough threads to handle concurrent requests
            // while some threads are blocked on synchronous DB calls in the
            // UserSqlServer constructor. 500 is generous but safe.
            ThreadPool.SetMinThreads(500, 500);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(15);
                    });

                });
    }
}
