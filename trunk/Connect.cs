using System;

using EnvDTE;
using EnvDTE80;
using Extensibility;

namespace WordLight
{
    public class Connect : IDTExtensibility2
    {
        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private WindowWatcher _watcher;

        public Connect()
        {
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            _watcher = new WindowWatcher(_applicationObject);
        }

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            _watcher.Dispose();
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }
        
        public void OnStartupComplete(ref Array custom)
        {            
        }

        public void OnBeginShutdown(ref Array custom)
        {            
        }
    }
}