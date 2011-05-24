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
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	///
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the 
	/// IVsPackage interface and uses the registration attributes defined in the framework to 
	/// register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
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
		private const string Freeze1CommandName = "Freeze1";
		private const string Freeze2CommandName = "Freeze2";
		private const string Freeze3CommandName = "Freeze3";
		private const string EnableLogCommandName = "EnableLog";

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

		protected override void Dispose(bool disposing)
		{
			if (_watcher != null)
			{
				_watcher.Dispose();
			}

			RemoveCommands();

			base.Dispose(disposing);
		}

		private void OnEnvironmentInitialized()
		{
			Log.Initialize("WordLight", this);
			Log.Debug("Initializing the add-in...");

			try
			{
				AddinSettings.Instance.Load(
					new RegistrySettingRepository(UserRegistryRoot.CreateSubKey("WordLight"))
					//new VsSettingRepository(_application.Globals, "WordLight")
				);

				RegisterMenuCommands();

				RegisterCommands();

				_watcher = new WindowWatcher(this);

				Log.Debug("Initialized.");
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception during initializing", ex);
			}
		}

		private void RegisterMenuCommands()
		{
			// Add our command handlers for menu (commands must exist in the .vsct file)
			var mcs = GetService(typeof(IMenuCommandService)) as IMenuCommandService;
			if (mcs != null)
			{
				// Create the command for the menu item.
				CommandID menuCommandID =
					new CommandID(GuidList.guidWordLightPackageCmdSet, (int)PkgCmdIDList.cmdidSettings);
				MenuCommand menuItem = new MenuCommand(SettingsMenuItemExecuted, menuCommandID);
				mcs.AddCommand(menuItem);

				CommandID menuCommandID2 =
					new CommandID(GuidList.guidWordLightPackageCmdSet, (int)PkgCmdIDList.cmdidFreezeSearch1);
				MenuCommand menuItem2 = new MenuCommand((o,e) => FreezeSearchGroup(1), menuCommandID2);
				mcs.AddCommand(menuItem2);
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
			_watcher.FreezeSearchOnActiveTextView(groupIndex);
		}


		#region Command registration

		private void RegisterCommands()
		{
			if (_dte == null)
			{
				return;
			}

			Commands commands = (Commands)_dte.Commands;
			
			object[] contextGUIDS = new object[] { };

			//This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
			//  just make sure you also update the QueryStatus/Exec method to include the new command names.
			try
			{
				RegisterSimpleCommand(commands, Freeze1CommandName, "Freeze search 1", AddinSettings.Instance.FreezeMark1Hotkey);
				RegisterSimpleCommand(commands, Freeze2CommandName, "Freeze search 2", AddinSettings.Instance.FreezeMark2Hotkey);
				RegisterSimpleCommand(commands, Freeze3CommandName, "Freeze search 3", AddinSettings.Instance.FreezeMark3Hotkey);

				RegisterSimpleCommand(commands, EnableLogCommandName, "Enable log", null);
			}
			catch (System.ArgumentException)
			{
				//If we are here, then the exception is probably because a command with that name
				//  already exists. If so there is no need to recreate the command and we can 
				//  safely ignore the exception.
			}
		}

		private void RegisterSimpleCommand(Commands commands, string name, string buttonText, string bindings)
		{

			//object[] contextGUIDS = new object[] { };
			//Command cmd = commands.AddNamedCommand(_dte, name, buttonText, string.Empty, true, 0, null, 16);

			//if (bindings != null)
			//{
			//    cmd.Bindings = bindings;
			//}
		}

		private void RemoveCommands()
		{
			//List<Command> toDelete = new List<Command>();

			//foreach (Command cmd in _application.Commands)
			//{
			//    if (!string.IsNullOrEmpty(cmd.Name) && cmd.Name.StartsWith(_addInInstance.ProgID + "."))
			//        toDelete.Add(cmd);
			//}

			//foreach (Command cmd in toDelete)
			//{
			//    cmd.Delete();
			//}
		}

		#endregion


		#region Implements IDTCommandTarget

		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			//try
			//{
			//    if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
			//    {
			//        string commandPrefix = _addInInstance.ProgID + ".";

			//        if (commandName == commandPrefix + Freeze1CommandName ||
			//            commandName == commandPrefix + Freeze2CommandName ||
			//            commandName == commandPrefix + Freeze3CommandName ||
			//            commandName == commandPrefix + EnableLogCommandName)
			//        {
			//            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
			//            return;
			//        }
			//    }
			//}
			//catch (Exception ex)
			//{
			//    Log.Error("Unhandled exception in IDTCommandTarget.QueryStatus", ex);
			//}
		}

		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{
			handled = false;

			//try
			//{
			//    if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			//    {
			//        string commandPrefix = _addInInstance.ProgID + ".";

			//        if (commandName == commandPrefix + EnableLogCommandName)
			//        {
			//            Log.Enabled = true;
			//            handled = true;
			//        }
			//        else if (commandName == commandPrefix + Freeze1CommandName)
			//        {
			//            FreezeSearchGroup(1);
			//            handled = true;
			//        }
			//        else if (commandName == commandPrefix + Freeze2CommandName)
			//        {
			//            FreezeSearchGroup(2);
			//            handled = true;
			//        }
			//        else if (commandName == commandPrefix + Freeze3CommandName)
			//        {
			//            FreezeSearchGroup(3);
			//            handled = true;
			//        }
			//    }
			//}
			//catch (Exception ex)
			//{
			//    Log.Error("Unhandled exception in IDTCommandTarget.Exec", ex);
			//}
		}

		#endregion
	}
}
