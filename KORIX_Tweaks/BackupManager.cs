using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace KORIX_Tweaks;

public static class BackupManager
{
	private static readonly string BackupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KORIX Tweaks", "Backups");
	private static readonly string FortniteBackupPath = Path.Combine(BackupRoot, "GameUserSettings.ini.backup");
	private static readonly string NetworkBackupPath = Path.Combine(BackupRoot, "network-tcp-global.txt");
	private static readonly string PowerBackupPath = Path.Combine(BackupRoot, "active-power-scheme.txt");

	public static string BackupLocation => BackupRoot;

	public static bool CreateAllBackups(out string message)
	{
		try
		{
			Directory.CreateDirectory(BackupRoot);
			// 백업 실패를 성공인척 넘기면 나중에 복원이 안됨. 이건 진짜 조심해야 됨
			bool backupSucceeded = true;
			backupSucceeded &= BackupRegistry("Software\\Microsoft\\GameBar", "gamebar.reg");
			backupSucceeded &= BackupRegistry("System\\GameConfigStore", "game-config-store.reg");
			backupSucceeded &= BackupRegistry("Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "game-dvr.reg");
			backupSucceeded &= BackupRegistry("Control Panel\\Mouse", "mouse.reg");
			backupSucceeded &= BackupText("netsh", "interface tcp show global", NetworkBackupPath);
			backupSucceeded &= BackupText("powercfg", "/getactivescheme", PowerBackupPath);
			string settingsPath = GetFortniteSettingsPath();
			if (File.Exists(settingsPath))
			{
				File.Copy(settingsPath, FortniteBackupPath, true);
			}
			else if (File.Exists(FortniteBackupPath))
			{
				File.Delete(FortniteBackupPath);
			}
			message = backupSucceeded ? "백업이 완료되었습니다." : "일부 설정 백업에 실패했습니다.";
			return backupSucceeded;
		}
		catch (Exception ex)
		{
			message = "백업 중 오류가 발생했습니다: " + ex.Message;
			return false;
		}
	}

	public static bool RestoreFortniteSettings(out string message)
	{
		try
		{
			if (!File.Exists(FortniteBackupPath))
			{
				message = "포트나이트 설정 백업 파일이 없습니다.";
				return false;
			}
			string settingsPath = GetFortniteSettingsPath();
			Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
			File.Copy(FortniteBackupPath, settingsPath, true);
			message = "포트나이트 설정을 백업 상태로 복원했습니다.";
			return true;
		}
		catch (Exception ex)
		{
			message = "포트나이트 설정 복원 중 오류가 발생했습니다: " + ex.Message;
			return false;
		}
	}

	public static bool ApplyFortniteCompetitiveSettings(out string message)
	{
		try
		{
			string settingsPath = GetFortniteSettingsPath();
			if (!File.Exists(settingsPath))
			{
				message = "GameUserSettings.ini 파일을 찾을 수 없습니다.";
				return false;
			}
			string[] settingValues =
			{
				"sg.ShadowQuality=0",
				"sg.EffectsQuality=0",
				"sg.PostProcessQuality=0",
				"sg.FoliageQuality=0",
				"sg.ShadingQuality=0",
				"sg.ReflectionQuality=0",
				"sg.GlobalIlluminationQuality=0",
				"sg.LandscapeQuality=0",
				"sg.AntiAliasingQuality=0",
				"sg.ViewDistanceQuality=2",
				"sg.TextureQuality=1"
			};
			List<string> lines = new List<string>(File.ReadAllLines(settingsPath));
			HashSet<string> changedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			bool inScalabilitySection = false;
			for (int index = 0; index < lines.Count; index++)
			{
				string trimmed = lines[index].Trim();
				if (trimmed.StartsWith("[", StringComparison.Ordinal))
				{
					inScalabilitySection = trimmed.Equals("[ScalabilityGroups]", StringComparison.OrdinalIgnoreCase);
				}
				if (!inScalabilitySection || trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal))
				{
					continue;
				}
				foreach (string setting in settingValues)
				{
					int separator = setting.IndexOf('=');
					string key = setting.Substring(0, separator);
					if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
					{
						lines[index] = setting;
						changedKeys.Add(key);
						break;
					}
				}
			}
			int sectionStart = lines.FindIndex(line => line.Trim().Equals("[ScalabilityGroups]", StringComparison.OrdinalIgnoreCase));
			if (sectionStart < 0)
			{
				lines.Add("[ScalabilityGroups]");
				foreach (string setting in settingValues)
				{
					lines.Add(setting);
				}
			}
			else
			{
				int sectionEnd = lines.Count;
				for (int index = sectionStart + 1; index < lines.Count; index++)
				{
					if (lines[index].TrimStart().StartsWith("[", StringComparison.Ordinal))
					{
						sectionEnd = index;
						break;
					}
				}
				int insertionIndex = sectionEnd;
				foreach (string setting in settingValues)
				{
					string key = setting.Substring(0, setting.IndexOf('='));
					if (!changedKeys.Contains(key))
					{
						lines.Insert(insertionIndex++, setting);
					}
				}
			}
			File.WriteAllLines(settingsPath, lines);
			message = "포트나이트 경쟁 설정을 적용했습니다.";
			return true;
		}
		catch (Exception ex)
		{
			message = "포트나이트 경쟁 설정 적용 중 오류가 발생했습니다: " + ex.Message;
			return false;
		}
	}

	public static bool RestoreWindowsSettings(out string message)
	{
		try
		{
			bool attempted = false;
			bool restored = true;
			string[] registryBackups = { "gamebar.reg", "game-config-store.reg", "game-dvr.reg", "mouse.reg" };
			foreach (string registryBackup in registryBackups)
			{
				string backupPath = Path.Combine(BackupRoot, registryBackup);
				if (File.Exists(backupPath))
				{
					attempted = true;
					restored &= RestoreRegistry(registryBackup);
				}
			}
			if (File.Exists(PowerBackupPath))
			{
				attempted = true;
				string powerText = File.ReadAllText(PowerBackupPath);
				int guidStart = powerText.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
				if (guidStart >= 0 && guidStart + 41 <= powerText.Length)
				{
					string guid = powerText.Substring(guidStart + 5, 36).Trim();
					restored &= RunCommand("powercfg", "/setactive " + guid);
				}
				else
				{
					restored = false;
				}
			}
			if (File.Exists(NetworkBackupPath))
			{
				attempted = true;
				restored &= RunElevatedCommand("netsh", "interface tcp set global autotuninglevel=normal");
				restored &= RunElevatedCommand("netsh", "interface tcp set global rss=enabled");
				restored &= RunElevatedCommand("netsh", "interface tcp set global ecncapability=default");
			}
			message = !attempted ? "복원할 Windows 백업이 없습니다." : restored ? "Windows 설정을 백업 상태로 복원했습니다." : "일부 Windows 설정 복원에 실패했습니다.";
			return attempted && restored;
		}
		catch (Exception ex)
		{
			message = "Windows 설정 복원 중 오류가 발생했습니다: " + ex.Message;
			return false;
		}
	}

	private static string GetFortniteSettingsPath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Config", "WindowsClient", "GameUserSettings.ini");
	}

	private static bool BackupRegistry(string subKey, string fileName)
	{
		string path = Path.Combine(BackupRoot, fileName);
		using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey))
		{
			if (key == null)
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				return true;
			}
		}
		bool succeeded = RunCommand("reg.exe", $"export \"HKCU\\{subKey}\" \"{path}\" /y") && File.Exists(path);
		if (!succeeded && File.Exists(path))
		{
			File.Delete(path);
		}
		return succeeded;
	}

	private static bool RestoreRegistry(string fileName)
	{
		string path = Path.Combine(BackupRoot, fileName);
		return File.Exists(path) && RunCommand("reg.exe", $"import \"{path}\"");
	}

	private static bool BackupText(string fileName, string arguments, string destination)
	{
		string output = RunCommandWithOutput(fileName, arguments);
		if (!string.IsNullOrWhiteSpace(output))
		{
			File.WriteAllText(destination, output);
			return true;
		}
		if (File.Exists(destination))
		{
			File.Delete(destination);
		}
		return false;
	}

	private static bool RunCommand(string fileName, string arguments)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			process?.WaitForExit();
			return process?.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	private static string RunCommandWithOutput(string fileName, string arguments)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			});
			if (process == null)
			{
				return string.Empty;
			}
			string output = process.StandardOutput.ReadToEnd();
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			return process.ExitCode == 0 ? output : string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool RunElevatedCommand(string fileName, string arguments)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = true,
				Verb = "runas",
				CreateNoWindow = true
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
}
