using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using WordLight.Extensions;

namespace WordLight
{
	public static class Log
	{
		private enum LogLevels
		{
			Info,
			Debug,
			Warning,
			Error
		}

		private static IServiceProvider _serviceProvider;
		private static object _serviceProviderSyncRoot = new object();
		private static string _packageName;
		private static Guid _outputWindowPaneId = Guid.NewGuid();
		private static bool _enabled;

		public static bool Enabled
		{
			get { return _enabled; }
			set 
			{
				_enabled = value;
				if (value) 
					Info("Enabled logging");
			}
		}

		public static void Initialize(string packageName, IServiceProvider serviceProvider)
		{
			if (string.IsNullOrEmpty(packageName)) throw new ArgumentException("packageName");
			if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");

			_packageName = packageName;			

			lock (_serviceProviderSyncRoot)
			{
				_serviceProvider = serviceProvider;
			}

#if DEBUG
			Enabled = true;
#endif
		}

		#region "Message methods"

		public static void Debug(string message)
		{
#if DEBUG
			LogMessage(LogLevels.Debug, message);
#endif
		}

		public static void Debug(string format, params object[] args)
		{
#if DEBUG
			LogMessage(LogLevels.Debug, string.Format(format, args));
#endif
		}

		public static void Info(string message)
		{
			LogMessage(LogLevels.Info, message);
		}

		public static void Info(string format, params object[] args)
		{
			LogMessage(LogLevels.Info, string.Format(format, args));
		}

		public static void Warning(string message)
		{
			LogMessage(LogLevels.Warning, message);
		}

		public static void Warning(string format, params object[] args)
		{
			LogMessage(LogLevels.Warning, string.Format(format, args));
		}

		public static void Error(string message)
		{
			LogMessage(LogLevels.Error, message);
		}

		public static void Error(string format, params object[] args)
		{
			LogMessage(LogLevels.Error, string.Format(format, args));
		}

		public static void Error(string message, Exception ex)
		{
			LogMessage(LogLevels.Error,
				string.Format("{0} -> Exception: {1}. Message: {2}. Stack trace: {3}", message,
				ex.GetType().ToString(), ex.Message, ex.StackTrace));
		}

		#endregion

		private static IVsActivityLog GetActivityLog()
		{
			IVsActivityLog log = null;

			lock (_serviceProviderSyncRoot)
			{
                if (_serviceProvider != null)
                {
					log = (IVsActivityLog)_serviceProvider.GetService(typeof(SVsActivityLog));
                }
			}
		
			return log;
		}

		private static IVsOutputWindowPane GetLogPane()
		{
			IVsOutputWindow outputWindow = null;

			lock (_serviceProviderSyncRoot)
			{
				if (_serviceProvider != null)
				{
					outputWindow = _serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
				}
			}

			if (outputWindow == null)
			{
				return null;
			}

            IVsOutputWindowPane logPane;

			outputWindow.GetPane(ref _outputWindowPaneId, out logPane);

			if (logPane == null)
			{
				if (
					ErrorHandler.Succeeded(outputWindow.CreatePane(_outputWindowPaneId, _packageName, 1, 0)) &&
					ErrorHandler.Succeeded(outputWindow.GetPane(ref _outputWindowPaneId, out logPane)) &&
					logPane != null
				)
				{
					logPane.WriteLine("Activity log for " + _packageName);
					logPane.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().FullName);
					logPane.WriteLine("----------------------------------------------------");
				}
			}

			return logPane;
		}

		private static void LogMessage(LogLevels level, string message)
		{
			if (!_enabled)
				return;

			message = string.Format("{0}\t{1}:\t{2}", DateTime.Now, level.ToString(), message);

			var outputPane = GetLogPane();
			if (outputPane != null)
			{
				outputPane.WriteLine(message);
			}

			var log = GetActivityLog();
			if (log != null)
			{
				var activityLogType = __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;
				if (level == LogLevels.Warning)
					activityLogType = __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;
				else if (level == LogLevels.Error)
					activityLogType = __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;

				log.LogEntry((UInt32)activityLogType, _packageName, message);
			}
		}
	}
}
