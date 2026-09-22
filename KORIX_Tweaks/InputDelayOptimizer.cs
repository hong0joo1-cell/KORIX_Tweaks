using System.Diagnostics;
using Microsoft.Win32;

namespace KORIX_Tweaks;

public static class InputDelayOptimizer
{
	public static bool Optimize()
	{
		bool result = true;
		try
		{
			using RegistryKey gameBar = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\GameBar");
			gameBar?.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);

			using RegistryKey gameDvr = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR");
			if (gameDvr != null)
			{
				gameDvr.SetValue("AppCaptureEnabled", 0, RegistryValueKind.DWord);
				gameDvr.SetValue("HistoricalCaptureEnabled", 0, RegistryValueKind.DWord);
				gameDvr.SetValue("AudioCaptureEnabled", 0, RegistryValueKind.DWord);
			}

			using RegistryKey gameConfigStore = Registry.CurrentUser.CreateSubKey("System\\GameConfigStore");
			gameConfigStore?.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);

			using RegistryKey mouse = Registry.CurrentUser.CreateSubKey("Control Panel\\Mouse");
			if (mouse != null)
			{
				mouse.SetValue("MouseSpeed", "0", RegistryValueKind.String);
				mouse.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
				mouse.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
				mouse.SetValue("MouseSensitivity", "10", RegistryValueKind.String);
			}
		}
		catch
		{
			result = false;
		}
		try
		{
			ApplyFortnitePriority();
		}
		catch
		{
			result = false;
		}
		return result;
	}

	private static void ApplyFortnitePriority()
	{
		Process[] processesByName = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
		foreach (Process process in processesByName)
		{
			try
			{
				if (!process.HasExited)
				{
					process.PriorityClass = ProcessPriorityClass.AboveNormal;
				}
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
	}
}
