﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace WordLight
{
    public static class ActivityLog
    {
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider _serviceProvider;
        private static object _serviceProviderSyncRoot = new object();
        private static bool _isLogAvailable;
        private static string _packageName;

        public static void Initialize(DTE2 application, string packageName)
        {
            if (application == null) throw new ArgumentNullException("application");
            if (string.IsNullOrEmpty(packageName)) throw new ArgumentException("packageName");

            _packageName = packageName;

            if (_serviceProvider == null)
            {
                lock (_serviceProviderSyncRoot)
                {
                    if (_serviceProvider == null)
                        _serviceProvider = application as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
                }
            }

            _isLogAvailable = GetActivityLog() != null;
        }

        private static IVsActivityLog GetActivityLog()
        {
            IVsActivityLog log = null;

            Guid SID = typeof(SVsActivityLog).GUID;
            Guid IID = typeof(IVsActivityLog).GUID;
            IntPtr output = IntPtr.Zero;

            lock (_serviceProviderSyncRoot)
            {
                if (_serviceProvider != null)
                    _serviceProvider.QueryService(ref SID, ref IID, out output);
            }

            if (output != IntPtr.Zero)
            {
                log = (IVsActivityLog)Marshal.GetObjectForIUnknown(output);
            }

            return log;
        }

        private static void LogMessage(__ACTIVITYLOG_ENTRYTYPE entryType, string message)
        {
            var log = GetActivityLog();
            if (log != null)
            {
                log.LogEntry((UInt32)entryType, _packageName, message);
            }
        }

        public static void Info(string message)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, message);
        }

        public static void Info(string format, params string[] args)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, string.Format(format, args));
        }

        public static void Warning(string message)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, message);
        }

        public static void Warning(string format, params string[] args)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, string.Format(format, args));
        }

        public static void Error(string message)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, message);
        }

        public static void Error(string format, params string[] args)
        {
            if (_isLogAvailable)
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, string.Format(format, args));
        }

        public static void Error(string message, Exception ex)
        {
            if (_isLogAvailable)
            {
                LogMessage(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, 
                    string.Format("{0} -> Exception: {1}. Message: {2}. Stack trace: {3}", message, 
                    ex.GetType().ToString(), ex.Message, ex.StackTrace));
            }
        }
    }
}
