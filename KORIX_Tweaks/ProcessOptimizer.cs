using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KORIX_Tweaks;

public static class ProcessOptimizer
{
	private static readonly HashSet<string> ProtectedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"System", "System Idle Process", "Registry", "smss", "csrss", "wininit", "services", "lsass", "svchost", "winlogon",
		"dwm", "fontdrvhost", "conhost", "explorer", "sihost", "taskhostw", "ctfmon", "SearchHost", "StartMenuExperienceHost", "ShellExperienceHost",
		"TextInputHost", "RuntimeBroker", "ApplicationFrameHost", "WmiPrvSE", "spoolsv", "audiodg", "MsMpEng", "SecurityHealthService", "NVIDIA Container", "nvcontainer"
	};

	private static readonly HashSet<string> SafeTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"OneDrive", "Teams", "ms-teams", "MicrosoftTeams", "Widgets", "WidgetService", "GameBar", "GameBarFTServer", "XboxApp", "XboxPcAppFT",
		"PhoneExperienceHost", "YourPhone", "EpicWebHelper", "steamwebhelper", "Spotify", "GoogleDriveFS", "Dropbox", "CCXProcess", "AdobeCollabSync"
	};

	public static int GetProcessCount()
	{
		try
		{
			return Process.GetProcesses().Length;
		}
		catch
		{
			return 0;
		}
	}

	public static List<Process> GetSafeProcesses()
	{
		List<Process> list = new List<Process>();
		try
		{
			int id = Process.GetCurrentProcess().Id;
			Process[] processes = Process.GetProcesses();
			for (int i = 0; i < processes.Length; i++)
			{
				Process process = processes[i];
				try
				{
					if (process.Id == id)
					{
						continue;
					}
					string processName = process.ProcessName;
					if (string.IsNullOrWhiteSpace(processName) || ProtectedProcesses.Contains(processName) || !SafeTargets.Contains(processName) || process.HasExited)
					{
						continue;
					}
					list.Add(process);
				}
				catch
				{
					try
					{
						process.Dispose();
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static int OptimizeProcesses()
	{
		if (!IsFortniteRunning())
		{
			return 0;
		}
		int num = 0;
		List<Process> safeProcesses = GetSafeProcesses();
		for (int i = 0; i < safeProcesses.Count; i++)
		{
			Process safeProcess = safeProcesses[i];
			try
			{
				if (safeProcess.HasExited)
				{
					continue;
				}
				string processName = safeProcess.ProcessName;
				if (ProtectedProcesses.Contains(processName) || !SafeTargets.Contains(processName))
				{
					continue;
				}
				try
				{
					if (safeProcess.CloseMainWindow())
					{
						safeProcess.WaitForExit(1500);
					}
				}
				catch
				{
				}
				if (safeProcess.HasExited)
				{
					num++;
				}
			}
			catch
			{
			}
			finally
			{
				try
				{
					safeProcess.Dispose();
				}
				catch
				{
				}
			}
		}
		return num;
	}

	public static bool IsFortniteRunning()
	{
		try
		{
			return Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length != 0;
		}
		catch
		{
			return false;
		}
	}

	public static List<string> GetSafeProcessNames()
	{
		HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Process safeProcess in GetSafeProcesses())
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(safeProcess.ProcessName))
					{
						names.Add(safeProcess.ProcessName);
					}
				}
				catch
				{
				}
				finally
				{
					try
					{
						safeProcess.Dispose();
					}
					catch
					{
					}
				}
			}
			List<string> result = names.ToList();
			result.Sort(StringComparer.OrdinalIgnoreCase);
			return result;
		}
		catch
		{
			return names.ToList();
		}
	}

	public static string RunOptimization()
	{
		int processCount = GetProcessCount();
		int value = OptimizeProcesses();
		int processCount2 = GetProcessCount();
		return $"프로세스 최적화 완료\n정리 전 : {processCount}개\n정리 후 : {processCount2}개\n정리됨 : {value}개";
	}
}
