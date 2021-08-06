using System;
using System.IO;
using System.Xml;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace MafaniaBot
{	
	internal static class Logger
	{
		public static ILog Log { get; private set; }

		public static void InitLogger()
		{
			Log = LogManager.GetLogger(typeof(Logger));

			try
			{
				var log4netConfig = new XmlDocument();

				using (var fs = File.OpenRead("log4net.config"))
				{
					log4netConfig.Load(fs);

					var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
					XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

					Log.Info("Logger initialized!");
				}
			}
			catch (Exception ex)
			{
				Log.Error("Error while initializing logger!", ex);
			}
		}	
	}
}
