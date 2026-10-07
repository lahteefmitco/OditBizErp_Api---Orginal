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
        public static void Main(string[] args)
        {
            // Prevent thread-pool starvation under burst load.
            // Default is ~4 (CPU cores) — too few when synchronous DB calls
            // block threads before async code can take over.
            ThreadPool.SetMinThreads(200, 200);

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
