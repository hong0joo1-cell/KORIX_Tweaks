using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace KORIX_Tweaks;

public static class DeviceManager
{
	private const int MetricRefreshMs = 800;
	private static readonly object MetricLock = new object();
	private static PerformanceCounter CpuCounter;
	private static readonly List<PerformanceCounter> GpuCounters = new List<PerformanceCounter>();
	private static DateTime _lastMetricRefresh = DateTime.MinValue;
	private static double _cachedCpuUsage;
	private static double _cachedGpuUsage;
	private static double _cachedRamUsage;

	public static void Initialize()
	{
		InitializeCpuCounter();
		InitializeGpuCounters();
		RefreshMetricsCache();
	}

	private static void RefreshMetricsCache()
	{
		lock (MetricLock)
		{
			_lastMetricRefresh = DateTime.UtcNow;
			_cachedCpuUsage = GetCPUUsageCore();
			_cachedGpuUsage = GetGPUUsageCore();
			_cachedRamUsage = GetRAMUsagePercent();
		}
	}

	private static bool ShouldRefreshMetrics()
	{
		return (DateTime.UtcNow - _lastMetricRefresh).TotalMilliseconds >= MetricRefreshMs;
	}

	private static void InitializeCpuCounter()
	{
		try
		{
			CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			CpuCounter.NextValue();
		}
		catch
		{
			CpuCounter = null;
		}
	}

	public static string GetCPUName()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				string text = item["Name"]?.ToString()?.Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "Unknown CPU";
	}

	public static int GetCPUCores()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["NumberOfCores"] != null)
				{
					return Convert.ToInt32(item["NumberOfCores"]);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	public static int GetCPUThreads()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT NumberOfLogicalProcessors FROM Win32_Processor");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["NumberOfLogicalProcessors"] != null)
				{
					return Convert.ToInt32(item["NumberOfLogicalProcessors"]);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	public static double GetCPUUsage()
	{
		if (ShouldRefreshMetrics())
		{
			RefreshMetricsCache();
		}
		return _cachedCpuUsage;
	}

	private static double GetCPUUsageCore()
	{
		try
		{
			if (CpuCounter == null)
			{
				InitializeCpuCounter();
			}
			if (CpuCounter == null)
			{
				return 0.0;
			}
			return ClampPercent(CpuCounter.NextValue());
		}
		catch
		{
			return 0.0;
		}
	}

	private static void InitializeGpuCounters()
	{
		foreach (PerformanceCounter gpuCounter in GpuCounters)
		{
			try
			{
				gpuCounter.Dispose();
			}
			catch
			{
			}
		}
		GpuCounters.Clear();
		try
		{
			string[] instanceNames = new PerformanceCounterCategory("GPU Engine").GetInstanceNames();
			foreach (string text in instanceNames)
			{
				if (text.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						PerformanceCounter performanceCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", text);
						performanceCounter.NextValue();
						GpuCounters.Add(performanceCounter);
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
	}

	public static double GetGPUUsage()
	{
		if (ShouldRefreshMetrics())
		{
			RefreshMetricsCache();
		}
		return _cachedGpuUsage;
	}

	private static double GetGPUUsageCore()
	{
		try
		{
			if (GpuCounters.Count == 0)
			{
				InitializeGpuCounters();
			}
			if (GpuCounters.Count == 0)
			{
				return 0.0;
			}
			double num = 0.0;
			foreach (PerformanceCounter gpuCounter in GpuCounters)
			{
				try
				{
					double num2 = gpuCounter.NextValue();
					if (!double.IsNaN(num2) && !double.IsInfinity(num2) && num2 > 0.0)
					{
						num += num2;
					}
				}
				catch
				{
				}
			}
			return ClampPercent(num);
		}
		catch
		{
			return 0.0;
		}
	}

	public static List<string> GetGPUNames()
	{
		List<string> list = new List<string>();
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				string text = item["Name"]?.ToString()?.Trim() ?? "";
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			list.Add("Unknown GPU");
		}
		return list;
	}

	public static string GetGPUName()
	{
		List<string> gPUNames = GetGPUNames();
		if (gPUNames.Count <= 0)
		{
			return "Unknown GPU";
		}
		return gPUNames[0];
	}

	public static long GetGPUVRAM()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT AdapterRAM FROM Win32_VideoController");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["AdapterRAM"] != null)
				{
					return Convert.ToInt64(item["AdapterRAM"]);
				}
			}
		}
		catch
		{
		}
		return 0L;
	}

	public static string GetGPUVRAMFormatted()
	{
		long gPUVRAM = GetGPUVRAM();
		if (gPUVRAM <= 0)
		{
			return "Unknown VRAM";
		}
		double value = (double)gPUVRAM / 1024.0 / 1024.0 / 1024.0;
		return $"{value:0.0} GB VRAM";
	}

	public static ulong GetTotalRAM()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["TotalVisibleMemorySize"] != null)
				{
					return Convert.ToUInt64(item["TotalVisibleMemorySize"]) * 1024;
				}
			}
		}
		catch
		{
		}
		return 0uL;
	}

	public static double GetTotalRAMGB()
	{
		ulong totalRAM = GetTotalRAM();
		if (totalRAM == 0L)
		{
			return 0.0;
		}
		return (double)totalRAM / 1024.0 / 1024.0 / 1024.0;
	}

	public static ulong GetAvailableRAM()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["FreePhysicalMemory"] != null)
				{
					return Convert.ToUInt64(item["FreePhysicalMemory"]) * 1024;
				}
			}
		}
		catch
		{
		}
		return 0uL;
	}

	public static double GetUsedRAMGB()
	{
		double totalRAMGB = GetTotalRAMGB();
		if (totalRAMGB <= 0.0)
		{
			return 0.0;
		}
		double num = (double)GetAvailableRAM() / 1024.0 / 1024.0 / 1024.0;
		double num2 = totalRAMGB - num;
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > totalRAMGB)
		{
			num2 = totalRAMGB;
		}
		return num2;
	}

	public static double GetRAMUsagePercent()
	{
		double totalRAMGB = GetTotalRAMGB();
		if (totalRAMGB <= 0.0)
		{
			return 0.0;
		}
		return ClampPercent(GetUsedRAMGB() / totalRAMGB * 100.0);
	}

	public static double GetRAMUsage()
	{
		if (ShouldRefreshMetrics())
		{
			RefreshMetricsCache();
		}
		return _cachedRamUsage;
	}

	public static double GetCPUTemperature()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["CurrentTemperature"] != null)
				{
					double num = Convert.ToDouble(item["CurrentTemperature"]) / 10.0 - 273.15;
					if (num >= 0.0 && num <= 150.0)
					{
						return num;
					}
				}
			}
		}
		catch
		{
		}
		return -1.0;
	}

	public static double GetGPUTemperature()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, Temperature FROM Win32_VideoController");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				if (item["Temperature"] != null)
				{
					double num = Convert.ToDouble(item["Temperature"]);
					if (num > 150.0)
					{
						num = num / 10.0 - 273.15;
					}
					if (num >= 0.0 && num <= 150.0)
					{
						return num;
					}
				}
			}
		}
		catch
		{
		}
		return -1.0;
	}

	public static string GetWindowsVersion()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher.Get().GetEnumerator();
			if (managementObjectEnumerator.MoveNext())
			{
				ManagementObject obj = (ManagementObject)managementObjectEnumerator.Current;
				string text = obj["Caption"]?.ToString() ?? "Windows";
				string text2 = obj["Version"]?.ToString() ?? "";
				return string.IsNullOrWhiteSpace(text2) ? text : (text + " (" + text2 + ")");
			}
		}
		catch
		{
		}
		return "Unknown Windows";
	}

	public static string GetSystemArchitecture()
	{
		try
		{
			return Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
		}
		catch
		{
			return "Unknown";
		}
	}

	public static string GetComputerName()
	{
		return Environment.MachineName;
	}

	private static double ClampPercent(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return 0.0;
		}
		if (value < 0.0)
		{
			return 0.0;
		}
		if (value > 100.0)
		{
			return 100.0;
		}
		return value;
	}

	public static void Dispose()
	{
		try
		{
			CpuCounter?.Dispose();
		}
		catch
		{
		}
		CpuCounter = null;
		foreach (PerformanceCounter gpuCounter in GpuCounters)
		{
			try
			{
				gpuCounter.Dispose();
			}
			catch
			{
			}
		}
		GpuCounters.Clear();
	}
}
