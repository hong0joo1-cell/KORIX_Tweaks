using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace KORIX_Tweaks;

public static class PingOptimizer
{
	public static bool Optimize()
	{
		if (!HasActiveNetworkAdapter())
		{
			return false;
		}
		bool requiredCommandsSucceeded = true;
		string[] requiredCommands =
		{
			"ipconfig /flushdns"
		};
		for (int i = 0; i < requiredCommands.Length; i++)
		{
			string command = requiredCommands[i];
			int separatorIndex = command.IndexOf(' ');
			string fileName = separatorIndex >= 0 ? command.Substring(0, separatorIndex) : command;
			string arguments = separatorIndex >= 0 ? command.Substring(separatorIndex + 1) : string.Empty;
			if (!RunElevatedCommand(fileName, arguments))
			{
				requiredCommandsSucceeded = false;
			}
		}
		RunElevatedCommand("netsh", "interface tcp set global autotuninglevel=normal");
		RunElevatedCommand("netsh", "interface tcp set global rss=enabled");
		RunElevatedCommand("netsh", "interface tcp set global ecncapability=disabled");
		return requiredCommandsSucceeded;
	}

	private static bool HasActiveNetworkAdapter()
	{
		try
		{
			foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.OperationalStatus == OperationalStatus.Up)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static string GetNetworkStatus()
	{
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			stringBuilder.AppendLine("=== KORIX Network Status ===");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(RunCommand("netsh", "interface tcp show global"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("DNS:");
			stringBuilder.AppendLine(RunCommand("ipconfig", "/all"));
		}
		catch
		{
			return "네트워크 상태를 확인할 수 없습니다.";
		}
		return stringBuilder.ToString();
	}

	public static double TestPing(string host = "8.8.8.8")
	{
		try
		{
			using Ping ping = new Ping();
			PingReply pingReply = ping.Send(host, 2000);
			if (pingReply != null && pingReply.Status == IPStatus.Success)
			{
				return pingReply.RoundtripTime;
			}
		}
		catch
		{
		}
		return -1.0;
	}

	private static string RunCommand(string fileName, string arguments)
	{
		try
		{
			using Process process = new Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.StandardOutputEncoding = Encoding.Default;
			process.StartInfo.StandardErrorEncoding = Encoding.Default;
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			string result = process.StandardError.ReadToEnd();
			process.WaitForExit();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return result;
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
				UseShellExecute = false,
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
