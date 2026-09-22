using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace KORIX_Tweaks;

public static class TweaksManager
{
	private static readonly Dictionary<int, ProcessPriorityClass> OriginalFortnitePriorities = new Dictionary<int, ProcessPriorityClass>();

	private static bool RunCommand(string fileName, string arguments)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = true,
				CreateNoWindow = true,
				Verb = "runas"
			});
			if (process == null)
			{
				return false;
			}
			process.WaitForExit();
			return process.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	public static bool ApplyFPSBooster()
	{
		try
		{
			bool result = true;
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\GameBar"))
			{
				if (registryKey != null)
				{
					registryKey.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
					registryKey.SetValue("AllowAutoGameMode", 1, RegistryValueKind.DWord);
				}
			}
			using (RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("System\\GameConfigStore"))
			{
				if (registryKey2 != null)
				{
					registryKey2.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
					registryKey2.SetValue("GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord);
				}
			}
			using (RegistryKey registryKey3 = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR"))
			{
				if (registryKey3 != null)
				{
					registryKey3.SetValue("AppCaptureEnabled", 0, RegistryValueKind.DWord);
					registryKey3.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
				}
			}
			if (!RunCommand("powercfg", "/setactive SCHEME_MIN"))
			{
				result = false;
			}
			return result;
		}
		catch
		{
			return false;
		}
	}

	public static bool ApplyFortniteOptimization()
	{
		bool result = BackupManager.ApplyFortniteCompetitiveSettings(out _);
		if (!result)
		{
			return false;
		}
		result &= ApplyFPSBooster();
		return result & MonitorFortniteProcess();
	}

	public static bool MonitorFortniteProcess()
	{
		// 게임 켜져 있을때만 우선순위 올리고 끝나면 기록만 정리함. 무작정 건드리면 안됨
		bool result = true;
		try
		{
			HashSet<int> activeIds = new HashSet<int>();
			foreach (Process process in Process.GetProcessesByName("FortniteClient-Win64-Shipping"))
			{
				using (process)
				{
					try
					{
						if (process.HasExited)
						{
							continue;
						}
						activeIds.Add(process.Id);
						if (!OriginalFortnitePriorities.ContainsKey(process.Id))
						{
							OriginalFortnitePriorities[process.Id] = process.PriorityClass;
						}
						if (process.PriorityClass != ProcessPriorityClass.AboveNormal)
						{
							process.PriorityClass = ProcessPriorityClass.AboveNormal;
						}
					}
					catch
					{
						result = false;
					}
				}
			}
			List<int> endedIds = new List<int>();
			foreach (int processId in OriginalFortnitePriorities.Keys)
			{
				if (!activeIds.Contains(processId))
				{
					endedIds.Add(processId);
				}
			}
			foreach (int processId in endedIds)
			{
				OriginalFortnitePriorities.Remove(processId);
			}
		}
		catch
		{
			result = false;
		}
		return result;
	}

	public static void RestoreFortniteProcessPriorities()
	{
		foreach (KeyValuePair<int, ProcessPriorityClass> item in OriginalFortnitePriorities)
		{
			try
			{
				using Process process = Process.GetProcessById(item.Key);
				if (!process.HasExited)
				{
					process.PriorityClass = item.Value;
				}
			}
			catch
			{
			}
		}
		OriginalFortnitePriorities.Clear();
	}

	public static bool EnableGameMode()
	{
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\GameBar"))
			{
				if (registryKey == null)
				{
					return false;
				}
				registryKey.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
				registryKey.SetValue("AllowAutoGameMode", 1, RegistryValueKind.DWord);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool EnableHighPerformance()
	{
		return RunCommand("powercfg", "/setactive SCHEME_MIN");
	}

	public static bool EnableUltimatePerformance()
	{
		try
		{
			using (Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "powercfg",
				Arguments = "/list",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			}))
			{
				if (process == null)
				{
					return false;
				}
				string text = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				if (text.IndexOf("e9a42b02-d5df-448d-aa00-03f14749eb61", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return RunCommand("powercfg", "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
				}
			}
			if (!RunCommand("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"))
			{
				return false;
			}
			using (Process process2 = Process.Start(new ProcessStartInfo
			{
				FileName = "powercfg",
				Arguments = "/list",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			}))
			{
				if (process2 == null)
				{
					return false;
				}
				string text2 = process2.StandardOutput.ReadToEnd();
				process2.WaitForExit();
				string[] array = text2.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text3 in array)
				{
					if (text3.IndexOf("e9a42b02-d5df-448d-aa00-03f14749eb61", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					int num = text3.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
					if (num >= 0)
					{
						num += 5;
						string text4 = text3.Substring(num).Trim().Split(' ')[0];
						if (!string.IsNullOrWhiteSpace(text4))
						{
							return RunCommand("powercfg", "/setactive " + text4);
						}
					}
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static bool RestoreDefaultPerformance()
	{
		return RunCommand("powercfg", "/setactive SCHEME_BALANCED");
	}

	public static int CleanTempFiles()
	{
		int num = 0;
		string tempPath = Path.GetTempPath();
		try
		{
			string[] files = Directory.GetFiles(tempPath);
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
					num++;
				}
				catch
				{
				}
			}
			files = Directory.GetDirectories(tempPath);
			foreach (string path2 in files)
			{
				try
				{
					Directory.Delete(path2, recursive: true);
					num++;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return num;
	}

	public static bool FlushDNS()
	{
		return RunCommand("ipconfig", "/flushdns");
	}

	public static bool ResetNetwork()
	{
		bool num = RunCommand("netsh", "winsock reset");
		bool flag = RunCommand("ipconfig", "/flushdns");
		return num & flag;
	}

	public static int OptimizeProcesses()
	{
		return ProcessOptimizer.OptimizeProcesses();
	}

	public static int GetProcessCount()
	{
		return ProcessOptimizer.GetProcessCount();
	}

	public static string RunProcessOptimization()
	{
		return ProcessOptimizer.RunOptimization();
	}

	public static bool OpenTaskManager()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "taskmgr.exe",
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool OpenDeviceManager()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "devmgmt.msc",
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool OpenSystemInformation()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "msinfo32.exe",
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool OpenTempFolder()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = Path.GetTempPath(),
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool OpenFortniteConfig()
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Config", "WindowsClient");
			if (!Directory.Exists(path))
			{
				return false;
			}
			Process.Start(new ProcessStartInfo
			{
				FileName = path,
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}
}
