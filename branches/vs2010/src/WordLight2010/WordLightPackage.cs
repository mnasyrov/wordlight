using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;

using EnvDTE;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

using WordLight;
using WordLight.Settings;

namespace WordLight2010
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the informations needed to show the this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string)]
	[Guid(GuidList.guidWordLightPackagePkgString)]
	public sealed class WordLightPackage : Package, IVsShellPropertyEvents
	{
		private WindowWatcher _watcher;
		private uint _cookie;
		private DTE _dte;

		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require 
		/// any Visual Studio service because at this point the package object is created but 
		/// not sited yet inside Visual Studio environment. The place to do all the other 
		/// initialization is the Initialize method.
		/// </summary>
		public WordLightPackage()
		{
			// Do nohting
		}

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initilaization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			// set an eventlistener for shell property changes

			IVsShell shellService = GetService(typeof(SVsShell)) as IVsShell;
			if (shellService != null)
			{
				ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out _cookie));
			}
		}

		public int OnShellPropertyChange(int propid, object var)
		{
			// when zombie state changes to false, finish package initialization
			if ((int)__VSSPROPID.VSSPROPID_Zombie == propid)
			{
				if ((bool)var == false)
				{
					// zombie state dependent code
					var dte = GetService(typeof(SDTE)) as DTE;

					// eventlistener no longer needed
					IVsShell shellService = GetService(typeof(SVsShell)) as IVsShell;
					if (shellService != null)
					{
						ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(this._cookie));
					}
					this._cookie = 0;

					if (dte != null)
					{
						_dte = dte;
						OnEnvironmentInitialized();
					}
				}
			}

			return VSConstants.S_OK;
		}

		private void OnEnvironmentInitialized()
		{
			Log.Initialize("WordLight", this);
			Log.Debug("Initializing the add-in...");

			try
			{
				var settingsRepository = new RegistrySettingRepository(UserRegistryRoot.CreateSubKey("WordLight"));
				//var settingsRepository = new VsSettingRepository(_application.Globals, "WordLight");

				AddinSettings.Instance.Load(settingsRepository);

				RegisterCommands();

				_watcher = new WindowWatcher(this);

				Log.Debug("Initialized.");
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception during initializing", ex);
			}
		}

		private void RegisterCommands()
		{
			var settingsCmdId = new CommandID(GuidList.guidWordLightPackageCmdSet, PkgCmdIDList.cmdidSettings);
			var freeze1CmdId = new CommandID(GuidList.guidWordLightPackageCmdSet, PkgCmdIDList.cmdidFreezeSearch1);
			var freeze2CmdId = new CommandID(GuidList.guidWordLightPackageCmdSet, PkgCmdIDList.cmdidFreezeSearch2);
			var freeze3CmdId = new CommandID(GuidList.guidWordLightPackageCmdSet, PkgCmdIDList.cmdidFreezeSearch3);
			var enableLog3CmdId = new CommandID(GuidList.guidWordLightPackageCmdSet, PkgCmdIDList.cmdidEnableLog);

			var mcs = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
			if (mcs != null)
			{
				mcs.AddCommand(new MenuCommand(SettingsMenuItemExecuted, settingsCmdId));

				mcs.AddCommand(new MenuCommand((o, e) => FreezeSearchGroup(1), freeze1CmdId));
				mcs.AddCommand(new MenuCommand((o, e) => FreezeSearchGroup(2), freeze2CmdId));
				mcs.AddCommand(new MenuCommand((o, e) => FreezeSearchGroup(3), freeze3CmdId));

				var mc = new MenuCommand((o, e) => Log.Enabled = true, enableLog3CmdId);
				mcs.AddCommand(mc);	
			}
		}

		private void SettingsMenuItemExecuted(object sender, EventArgs e)
		{
			using (var dialog = new SettingsForm())
			{
				var result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
					AddinSettings.Instance.Save();
				else
					AddinSettings.Instance.Reload();
			}
		}

		private void FreezeSearchGroup(int groupIndex)
		{
			if (_watcher != null)
			{
				_watcher.FreezeSearchOnActiveTextView(groupIndex);
			}
		}

	}
}
