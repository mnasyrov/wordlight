using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using EnvDTE;
using EnvDTE80;
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

		private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider _serviceProvider;
		private static object _serviceProviderSyncRoot = new object();
		private static string _packageName;
		private static DTE2 _application;

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

		public static void Initialize(DTE2 application, string packageName)
		{
			if (application == null) throw new ArgumentNullException("application");
			if (string.IsNullOrEmpty(packageName)) throw new ArgumentException("packageName");

			_application = application;
			_packageName = packageName;

			if (_serviceProvider == null)
			{
				lock (_serviceProviderSyncRoot)
				{
					if (_serviceProvider == null)
						_serviceProvider = application as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
				}
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

            //Guid SID = typeof(SVsActivityLog).GUID;
            //Guid IID = typeof(IVsActivityLog).GUID;
            //IntPtr output = IntPtr.Zero;

			lock (_serviceProviderSyncRoot)
			{
                if (_serviceProvider != null)
                {
                    //_serviceProvider.QueryService(ref SID, ref IID, out output);
                    using (ServiceProvider wrapperSP = new ServiceProvider(_serviceProvider))
                    {
                        log = (IVsActivityLog)wrapperSP.GetService(typeof(SVsActivityLog));
                    }
                }
			}

            //if (output != IntPtr.Zero)
            //{
            //    log = (IVsActivityLog)Marshal.GetObjectForIUnknown(output);
            //}
			
			return log;
		}

		private static OutputWindowPane GetLogPane()
		{
			string title = _packageName;

			OutputWindowPanes panes = _application.ToolWindows.OutputWindow.OutputWindowPanes;
			OutputWindowPane logPane;
			try
			{
				logPane = panes.Item(title); // If the pane exists already, return it.
			}
			catch (ArgumentException)
			{
				// Create a new pane.				
				logPane = panes.Add(title);
				logPane.WriteLine("Activity log for " + _packageName);
				logPane.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().FullName);
				logPane.WriteLine("----------------------------------------------------");
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
