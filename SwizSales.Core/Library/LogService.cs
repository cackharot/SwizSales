using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]
namespace SwizSales.Core.Library
{
    public static class LogService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("SwizSalesLog");

        public static void Info(string message, params object[] args)
        {
            log.InfoFormat(message, args);
        }

        public static void Warn(string message, params object[] args)
        {
            log.WarnFormat(message, args);
        }

        public static void Error(string message, Exception ex)
        {
            log.Error(message, ex);
        }

        public static void Debug(string message, params object[] args)
        {
            log.DebugFormat(message, args);
        }

        public static void Debug(string message, Exception ex = null)
        {
            log.Debug(message, ex);
        }

        public static void Fatal(string message, Exception ex)
        {
            log.Fatal(message, ex);
        }
    }
}
