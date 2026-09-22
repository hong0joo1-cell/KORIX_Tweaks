using System;
using System.Diagnostics;
using System.Threading;

namespace KORIX_Tweaks;

public static class PowerManager
{
	private const string UltimateGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

	private static bool RunAdmin(string command)
	{
		try
		{
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c " + command,
				Verb = "runas",
				UseShellExecute = true,
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

	private static string Run(string command)
	{
		Process process = Process.Start(new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c " + command,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		});
		if (process == null)
		{
			return "";
		}
		string result = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
		process.WaitForExit();
		return result;
	}

	public static bool Exists(string name)
	{
		return Run("powercfg /list").ToLower().Contains(name.ToLower());
	}

	public static bool CreateUltimate()
	{
		if (Exists("KORIX Ultimate"))
		{
			SetActive("KORIX Ultimate");
			return true;
		}
		string before = Run("powercfg /list");
		if (!RunAdmin("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"))
		{
			return false;
		}
		Thread.Sleep(1500);
		string after = Run("powercfg /list");
		string text = FindNewGuid(before, after);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		RunAdmin("powercfg -changename " + text + " \"KORIX Ultimate\"");
		RunAdmin("powercfg -setactive " + text);
		ApplyPerformanceTweaks(text);
		return true;
	}

	public static bool CreateGaming()
	{
		if (Exists("KORIX Gaming"))
		{
			SetActive("KORIX Gaming");
			return true;
		}
		string before = Run("powercfg /list");
		if (!RunAdmin("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"))
		{
			return false;
		}
		Thread.Sleep(1500);
		string after = Run("powercfg /list");
		string text = FindNewGuid(before, after);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		RunAdmin("powercfg -changename " + text + " \"KORIX Gaming\"");
		RunAdmin("powercfg -setactive " + text);
		ApplyGamingTweaks(text);
		return true;
	}

	public static bool CreateEsports()
	{
		if (Exists("KORIX Esports"))
		{
			SetActive("KORIX Esports");
			return true;
		}
		string before = Run("powercfg /list");
		if (!RunAdmin("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"))
		{
			return false;
		}
		Thread.Sleep(1500);
		string after = Run("powercfg /list");
		string text = FindNewGuid(before, after);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		RunAdmin("powercfg -changename " + text + " \"KORIX Esports\"");
		RunAdmin("powercfg -setactive " + text);
		ApplyEsportsTweaks(text);
		return true;
	}

	private static void ApplyPerformanceTweaks(string guid)
	{
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_PROCESSOR PROCTHROTTLEMIN 100");
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_PROCESSOR PROCTHROTTLEMAX 100");
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_DISK DISKIDLE 0");
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_SLEEP STANDBYIDLE 0");
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_VIDEO VIDEOIDLE 0");
		RunAdmin("powercfg -setactive " + guid);
	}

	private static void ApplyGamingTweaks(string guid)
	{
		ApplyPerformanceTweaks(guid);
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_PROCESSOR IDLEDISABLE 1");
	}

	private static void ApplyEsportsTweaks(string guid)
	{
		ApplyPerformanceTweaks(guid);
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_PROCESSOR IDLEDISABLE 1");
		RunAdmin("powercfg -setacvalueindex " + guid + " SUB_PROCESSOR PERFBOOSTMODE 0");
	}

	private static string FindNewGuid(string before, string after)
	{
		string[] array = after.Split(Environment.NewLine);
		foreach (string text in array)
		{
			if (!text.Contains("GUID"))
			{
				continue;
			}
			int num = text.IndexOf(':');
			if (num >= 0)
			{
				string text2 = text.Substring(num + 1, 36).Trim();
				if (!before.Contains(text2))
				{
					return text2;
				}
			}
		}
		return "";
	}

	public static string GetGuid(string planName)
	{
		string[] array = Run("powercfg /list").Split(Environment.NewLine);
		foreach (string text in array)
		{
			if (text.ToLower().Contains(planName.ToLower()))
			{
				int num = text.IndexOf(':');
				if (num >= 0)
				{
					return text.Substring(num + 1, 36).Trim();
				}
			}
		}
		return "";
	}

	public static void SetActive(string planName)
	{
		string guid = GetGuid(planName);
		if (!string.IsNullOrWhiteSpace(guid))
		{
			RunAdmin("powercfg -setactive " + guid);
		}
	}

	public static void Delete(string planName)
	{
		string guid = GetGuid(planName);
		if (!string.IsNullOrWhiteSpace(guid))
		{
			RunAdmin("powercfg -delete " + guid);
		}
	}

	public static string GetActivePlan()
	{
		return Run("powercfg /getactivescheme");
	}
}
