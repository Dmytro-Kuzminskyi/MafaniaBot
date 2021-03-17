using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

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
            string port = Environment.GetEnvironmentVariable("PORT");
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseUrls("http://*:" + port).Build();
        }
    }
}