using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

using EnvDTE;
using EnvDTE80;
using Extensibility;

using WordLight;
using WordLight.Settings;

namespace WordLight2010
{
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
		private const string Freeze1CommandName = "Freeze1";
		private const string Freeze2CommandName = "Freeze2";
		private const string Freeze3CommandName = "Freeze3";
		private const string EnableLogCommandName = "EnableLog";

		private DTE2 _application;
		private AddIn _addInInstance;
		private WindowWatcher _watcher;

		public Connect()
		{
		}

		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_application = (DTE2)application;
			_addInInstance = (AddIn)addInInst;


			try
			{
				if (connectMode == ext_ConnectMode.ext_cm_AfterStartup)
				{
					
				}

				if (_watcher == null)
				{
					_watcher = new WindowWatcher(_application);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception during initializing", ex);
			}
		}

		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
			_watcher.Dispose();
			if (disconnectMode == ext_DisconnectMode.ext_dm_HostShutdown || disconnectMode == ext_DisconnectMode.ext_dm_UserClosed)
			{
				RemoveCommands();
			}
		}

		public void OnAddInsUpdate(ref Array custom)
		{
		}

		public void OnStartupComplete(ref Array custom)
		{
			try
			{
				RegisterCommands();
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception in OnStartupComplete", ex);
			}
		}

		public void OnBeginShutdown(ref Array custom)
		{
		}

		private void RegisterCommands()
		{
			Commands2 commands = (Commands2)_application.Commands;

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

		private void RegisterSimpleCommand(Commands2 commands, string name, string buttonText, string bindings)
		{
			object[] contextGUIDS = new object[] { };
			Command cmd = commands.AddNamedCommand2(_addInInstance, name, buttonText, string.Empty, true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);

			if (bindings != null)
				cmd.Bindings = bindings;
		}

		private void RemoveCommands()
		{
			List<Command> toDelete = new List<Command>();

			foreach (Command cmd in _application.Commands)
			{
				if (!string.IsNullOrEmpty(cmd.Name) && cmd.Name.StartsWith(_addInInstance.ProgID + "."))
					toDelete.Add(cmd);
			}

			foreach (Command cmd in toDelete)
			{
				cmd.Delete();
			}
		}

		#region Implements IDTCommandTarget

		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			try
			{
				if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
				{
					string commandPrefix = _addInInstance.ProgID + ".";

					if (commandName == commandPrefix + Freeze1CommandName ||
						commandName == commandPrefix + Freeze2CommandName ||
						commandName == commandPrefix + Freeze3CommandName ||
						commandName == commandPrefix + EnableLogCommandName)
					{
						status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception in IDTCommandTarget.QueryStatus", ex);
			}
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

			try
			{
				if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
				{
					string commandPrefix = _addInInstance.ProgID + ".";

					if (commandName == commandPrefix + EnableLogCommandName)
					{
						Log.Enabled = true;
						handled = true;
					}
					else if (commandName == commandPrefix + Freeze1CommandName)
					{
						FreezeSearchGroup(1);
						handled = true;
					}
					else if (commandName == commandPrefix + Freeze2CommandName)
					{
						FreezeSearchGroup(2);
						handled = true;
					}
					else if (commandName == commandPrefix + Freeze3CommandName)
					{
						FreezeSearchGroup(3);
						handled = true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Unhandled exception in IDTCommandTarget.Exec", ex);
			}
		}

		#endregion

		private void FreezeSearchGroup(int groupIndex)
		{
			_watcher.FreezeSearchOnActiveTextView(groupIndex);
		}
	}
}