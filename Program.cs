using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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

            var port = Environment.GetEnvironmentVariable("PORT");

            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseUrls("http://*:" + port).Build();
        }
    }

    public sealed class GenericEventArgs<T> : EventArgs
    {
        public GenericEventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
