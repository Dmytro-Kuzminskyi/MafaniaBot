using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;

namespace MafaniaBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            Logger.InitLogger();
            string port = Environment.GetEnvironmentVariable("PORT");
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseUrls("http://*:" + port).Build();
        }
    }
}