using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevServer.Service.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //创建数据库
            SqliteContext dbContext = new SqliteContext();
            dbContext.Database.EnsureCreated();
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                
                webBuilder.UseStartup<Startup>().UseKestrel(opa =>
                {
                    opa.Limits.MaxConcurrentConnections         = 50000;
                    opa.Limits.MaxConcurrentUpgradedConnections = 50000;
                    opa.Limits.MaxRequestBodySize               = 2000 * 1024 * 1024; //2G
                    opa.Limits.MinRequestBodyDataRate           = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    opa.Limits.MinResponseDataRate              = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                    opa.Listen(IPAddress.Loopback, 20000);
                });
            });
    }
}