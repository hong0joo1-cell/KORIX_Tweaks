using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace KORIX_Tweaks;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly DispatcherTimer monitorTimer;
	private readonly object updateLock = new object();
	private bool autoMonitorEnabled = true;
	private readonly string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KORIX Tweaks", "korix.log");

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
		monitorTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.5)
		};
		monitorTimer.Tick += MonitorTimer_Tick;
		_ = InitializeAsync();
		monitorTimer.Start();
	}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		var fade = new System.Windows.Media.Animation.DoubleAnimation
		{
			From = 0,
			To = 1,
			Duration = TimeSpan.FromMilliseconds(500),
			EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
		};
		var glow = new System.Windows.Media.Animation.DoubleAnimation
		{
			From = 0.2,
			To = 0.8,
			Duration = TimeSpan.FromMilliseconds(1200),
			AutoReverse = true,
			RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
		};
		BeginAnimation(OpacityProperty, fade);
		var glowShift = new System.Windows.Media.Animation.DoubleAnimation
		{
			From = 0,
			To = 22,
			Duration = TimeSpan.FromSeconds(7),
			AutoReverse = true,
			RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
		};
		((System.Windows.Media.TranslateTransform)NeonGlow.RenderTransform).BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, glowShift);
		AnimateWave(BackWave, -18, 18, 11);
		AnimateWave(MiddleWave, 22, -22, 15);
		AnimateWave(FrontWave, -14, 14, 9);
	
		var border = (System.Windows.Controls.Border)FindName("MainContentBorder");
		if (border != null)
		{
			var effect = border.Effect as System.Windows.Media.Effects.DropShadowEffect;
			if (effect != null)
			{
				effect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, glow);
			}
		}
	}

	private static void AnimateWave(System.Windows.Shapes.Path wave, double from, double to, double seconds)
	{
		var transform = (System.Windows.Media.TranslateTransform)wave.RenderTransform;
		var animation = new System.Windows.Media.Animation.DoubleAnimation
		{
			From = from,
			To = to,
			Duration = TimeSpan.FromSeconds(seconds),
			AutoReverse = true,
			RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
		};
		transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
	}

	private void HideSplash()
	{
		var splashFade = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(650));
		splashFade.Completed += (_, _) => SplashOverlay.Visibility = Visibility.Collapsed;
		SplashOverlay.BeginAnimation(OpacityProperty, splashFade);
	}

	private async Task InitializeAsync()
	{
		LoadingText.Text = "장치 정보를 확인하고 있습니다...";
		try
		{
			await Task.Run(() => DeviceManager.Initialize());
		}
		catch
		{
		}
		await Dispatcher.BeginInvoke(new Action(() =>
		{
			ShowHome();
			UpdateDeviceInfo();
			UpdateMonitor();
			StatusText.Text = "준비됨";
			LoadingText.Text = "준비가 완료되었습니다.";
			HideSplash();
		}));
	}

	private void MonitorTimer_Tick(object sender, EventArgs e)
	{
		if (!Monitor.TryEnter(updateLock))
		{
			return;
		}
		try
		{
			UpdateDeviceInfo();
			UpdateMonitor();
			UpdateSystemStatus();
			if (autoMonitorEnabled)
			{
				TweaksManager.MonitorFortniteProcess();
			}
		}
		finally
		{
			Monitor.Exit(updateLock);
		}
	}

	private void HideAllPanels()
	{
		HomePanel.Visibility = Visibility.Collapsed;
		TweaksPanel.Visibility = Visibility.Collapsed;
		FortnitePanel.Visibility = Visibility.Collapsed;
		PowerPanel.Visibility = Visibility.Collapsed;
		CleanerPanel.Visibility = Visibility.Collapsed;
		MonitorPanel.Visibility = Visibility.Collapsed;
		UtilityPanel.Visibility = Visibility.Collapsed;
		SettingsPanel.Visibility = Visibility.Collapsed;
	}

	private void ShowPanel(System.Windows.UIElement panel)
	{
		HideAllPanels();
		panel.Visibility = Visibility.Visible;
		var transform = new System.Windows.Media.TranslateTransform(18, 0);
		panel.RenderTransform = transform;
		var slide = new System.Windows.Media.Animation.DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(230))
		{
			EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
		};
		transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slide);
	}

	private void SetPage(string title, string description)
	{
		PageTitleText.Text = title;
		CurrentPageText.Text = description;
	}

	private void ShowHome()
	{
		ShowPanel(HomePanel);
		SetPage("홈", "KORIX 성능 관리");
		StatusText.Text = "시스템 준비됨";
	}

	private void HomeButton_Click(object sender, RoutedEventArgs e)
	{
		ShowHome();
	}

	private void ShowTweaks()
	{
		ShowPanel(TweaksPanel);
		SetPage("성능 최적화", "Windows 성능을 위한 도구");
		StatusText.Text = "최적화 도구 준비됨";
	}

	private void TweaksButton_Click(object sender, RoutedEventArgs e)
	{
		ShowTweaks();
	}

	private void ShowFortnite()
	{
		ShowPanel(FortnitePanel);
		SetPage("포트나이트", "포트나이트 성능 최적화");
		StatusText.Text = "포트나이트 도구 준비됨";
	}

	private void FortniteButton_Click(object sender, RoutedEventArgs e)
	{
		ShowFortnite();
	}

	private void ShowPower()
	{
		ShowPanel(PowerPanel);
		SetPage("전원 관리", "Windows 전원 설정");
		StatusText.Text = "전원 관리 준비됨";
	}

	private void PowerButton_Click(object sender, RoutedEventArgs e)
	{
		ShowPower();
	}

	private void ShowCleaner()
	{
		ShowPanel(CleanerPanel);
		SetPage("시스템 정리", "불필요한 파일 정리");
		StatusText.Text = "정리 도구 준비됨";
	}

	private void CleanerButton_Click(object sender, RoutedEventArgs e)
	{
		ShowCleaner();
	}

	private void ShowMonitor()
	{
		ShowPanel(MonitorPanel);
		SetPage("시스템 모니터", "실시간 시스템 상태");
		StatusText.Text = "모니터링 중";
	}

	private void MonitorButton_Click(object sender, RoutedEventArgs e)
	{
		ShowMonitor();
	}

	private void ShowUtility()
	{
		ShowPanel(UtilityPanel);
		SetPage("Windows 도구", "Windows 시스템 도구");
		StatusText.Text = "도구 준비됨";
	}

	private void UtilityButton_Click(object sender, RoutedEventArgs e)
	{
		ShowUtility();
	}

	private void ShowSettings()
	{
		ShowPanel(SettingsPanel);
		SetPage("설정", "KORIX 성능 관리 설정");
		StatusText.Text = "설정";
	}

	private void SettingsButton_Click(object sender, RoutedEventArgs e)
	{
		ShowSettings();
	}

	private void UpdateSystemStatus()
	{
		try
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			bool isAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
			AdminStatusText.Text = "관리자 권한: " + (isAdmin ? "사용 중" : "없음");
			FortniteStatusText.Text = "포트나이트: " + (ProcessOptimizer.IsFortniteRunning() ? "실행 중" : "실행 안 됨");
			PowerStatusText.Text = "전원 모드: " + GetActivePowerScheme();
			NetworkStatusText.Text = "네트워크: " + (NetworkInterface.GetIsNetworkAvailable() ? "연결됨" : "연결 안 됨");
		}
		catch
		{
			AdminStatusText.Text = "상태 확인 안 됨";
			FortniteStatusText.Text = "포트나이트 상태 확인 안 됨";
			PowerStatusText.Text = "전원 모드 확인 안 됨";
			NetworkStatusText.Text = "네트워크 상태 확인 안 됨";
		}
	}

	private static string GetActivePowerScheme()
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "powercfg",
				Arguments = "/getactivescheme",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true
			});
			string output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
			process?.WaitForExit();
			int separator = output.LastIndexOf(')');
			int start = output.IndexOf('(');
			return start >= 0 && separator > start ? output.Substring(start + 1, separator - start - 1).Trim() : "확인 안 됨";
		}
		catch
		{
			return "확인 안 됨";
		}
	}

	private void AutoMonitorCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		autoMonitorEnabled = AutoMonitorCheckBox.IsChecked == true;
		StatusText.Text = autoMonitorEnabled ? "자동 감시 켜짐" : "자동 감시 꺼짐";
		WriteLog(StatusText.Text);
	}

	private void StandardProfileButton_Click(object sender, RoutedEventArgs e)
	{
		bool applied = TweaksManager.ApplyFPSBooster() && TweaksManager.EnableGameMode();
		StatusText.Text = applied ? "일반 성능 프로필 적용 완료" : "일반 성능 프로필 일부 실패";
		WriteLog(StatusText.Text);
	}

	private void CompetitiveProfileButton_Click(object sender, RoutedEventArgs e)
	{
		if (!PrepareSettingsChange())
		{
			return;
		}
		bool applied = TweaksManager.ApplyFortniteOptimization();
		StatusText.Text = applied ? "포트나이트 경쟁 프로필 적용 완료" : "포트나이트 경쟁 프로필 실패";
		WriteLog(StatusText.Text);
	}

	private void RestoreProfileButton_Click(object sender, RoutedEventArgs e)
	{
		bool restoredWindows = BackupManager.RestoreWindowsSettings(out string windowsMessage);
		bool restoredFortnite = BackupManager.RestoreFortniteSettings(out string fortniteMessage);
		StatusText.Text = restoredWindows && restoredFortnite ? "백업 프로필 복원 완료" : restoredWindows || restoredFortnite ? "백업 프로필 일부 복원" : "복원할 백업이 없음";
		WriteLog(windowsMessage + " " + fortniteMessage);
	}

	private void OpenBackupFolderButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Directory.CreateDirectory(BackupManager.BackupLocation);
			Process.Start(new ProcessStartInfo { FileName = BackupManager.BackupLocation, UseShellExecute = true });
			StatusText.Text = "백업 폴더 열기 완료";
		}
		catch (Exception ex)
		{
			StatusText.Text = "백업 폴더 오류: " + ex.Message;
		}
	}

	private void ExportLogButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			WriteLog("로그 내보내기 실행");
			Directory.CreateDirectory(Path.GetDirectoryName(logPath));
			Process.Start(new ProcessStartInfo { FileName = logPath, UseShellExecute = true });
			StatusText.Text = "작업 로그 열기 완료";
		}
		catch (Exception ex)
		{
			StatusText.Text = "로그 열기 오류: " + ex.Message;
		}
	}

	private void WriteLog(string message)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(logPath));
			File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
		}
		catch
		{
		}
	}

	private bool PrepareSettingsChange()
	{
		MessageBoxResult answer = MessageBox.Show(
			"Windows 또는 포트나이트 설정을 변경합니다.\n\n예: 현재 설정을 백업한 뒤 적용\n아니오: 백업 없이 바로 적용\n취소: 작업 취소",
			"설정 변경 방법 선택",
			MessageBoxButton.YesNoCancel,
			MessageBoxImage.Warning);
		if (answer == MessageBoxResult.Cancel)
		{
			StatusText.Text = "변경을 취소했습니다.";
			return false;
		}
		if (answer == MessageBoxResult.Yes && !BackupManager.CreateAllBackups(out string backupMessage))
		{
			MessageBox.Show(backupMessage + "\n\n백업이 완료되지 않아 설정을 변경하지 않았습니다.", "백업 실패", MessageBoxButton.OK, MessageBoxImage.Error);
			StatusText.Text = "백업 실패로 변경하지 않음";
			return false;
		}
		StatusText.Text = answer == MessageBoxResult.Yes ? "백업 완료. 설정을 적용하는 중..." : "백업 없이 설정을 적용하는 중...";
		return true;
	}

	private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
	{
		if (BackupManager.CreateAllBackups(out string message))
		{
			StatusText.Text = "현재 설정 백업 완료";
			MessageBox.Show(message + "\n\n저장 위치:\n" + BackupManager.BackupLocation, "백업 완료", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		else
		{
			StatusText.Text = "백업 실패";
			MessageBox.Show(message, "백업 실패", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void RestoreFortniteSettingsButton_Click(object sender, RoutedEventArgs e)
	{
		MessageBoxResult answer = MessageBox.Show("포트나이트 설정을 마지막 백업 상태로 복원합니다. 게임을 종료한 상태에서 진행하세요.", "복원 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (answer != MessageBoxResult.Yes)
		{
			return;
		}
		bool restored = BackupManager.RestoreFortniteSettings(out string message);
		StatusText.Text = restored ? "포트나이트 설정 복원 완료" : "포트나이트 설정 복원 실패";
		MessageBox.Show(message, restored ? "복원 완료" : "복원 실패", MessageBoxButton.OK, restored ? MessageBoxImage.Information : MessageBoxImage.Error);
	}

	private void RestoreWindowsSettingsButton_Click(object sender, RoutedEventArgs e)
	{
		MessageBoxResult answer = MessageBox.Show("백업해 둔 Windows 설정과 전원 계획을 복원합니다. 계속하시겠습니까?", "복원 경고", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (answer != MessageBoxResult.Yes)
		{
			return;
		}
		bool restored = BackupManager.RestoreWindowsSettings(out string message);
		StatusText.Text = restored ? "Windows 설정 복원 완료" : "Windows 설정 복원 실패";
		MessageBox.Show(message, restored ? "복원 완료" : "복원 실패", MessageBoxButton.OK, restored ? MessageBoxImage.Information : MessageBoxImage.Error);
	}

	private void UpdateDeviceInfo()
	{
		try
		{
			string cPUName = DeviceManager.GetCPUName();
			double cPUUsage = DeviceManager.GetCPUUsage();
			double cPUTemperature = DeviceManager.GetCPUTemperature();
			CpuText.Text = (string.IsNullOrWhiteSpace(cPUName) ? "CPU 확인 안 됨" : cPUName);
			CpuPercentText.Text = $"{cPUUsage:0}%";
			CpuTempText.Text = ((cPUTemperature >= 0.0) ? $"온도: {cPUTemperature:0}°C" : "온도: 확인 안 됨");
			string gPUName = DeviceManager.GetGPUName();
			double gPUUsage = DeviceManager.GetGPUUsage();
			double gPUTemperature = DeviceManager.GetGPUTemperature();
			GpuText.Text = (string.IsNullOrWhiteSpace(gPUName) ? "GPU 확인 안 됨" : gPUName);
			GpuPercentText.Text = $"{gPUUsage:0}%";
			GpuTempText.Text = ((gPUTemperature >= 0.0) ? $"온도: {gPUTemperature:0}°C" : "온도: 확인 안 됨");
			double rAMUsage = DeviceManager.GetRAMUsage();
			double totalRAMGB = DeviceManager.GetTotalRAMGB();
			double usedRAMGB = DeviceManager.GetUsedRAMGB();
			RamPercentText.Text = $"{rAMUsage:0}%";
			RamProgressBar.Value = Math.Max(0.0, Math.Min(100.0, rAMUsage));
			if (totalRAMGB > 0.0)
			{
				RamText.Text = $"{usedRAMGB:0.0} / {totalRAMGB:0.0} GB";
			}
			else
			{
				RamText.Text = "메모리 확인 안 됨";
			}
			RamTempText.Text = "메모리 사용량";
		}
		catch
		{
			CpuText.Text = "CPU 확인 안 됨";
			CpuPercentText.Text = "N/A";
			CpuTempText.Text = "온도: 확인 안 됨";
			GpuText.Text = "GPU 확인 안 됨";
			GpuPercentText.Text = "N/A";
			GpuTempText.Text = "온도: 확인 안 됨";
			RamText.Text = "메모리 확인 안 됨";
			RamPercentText.Text = "N/A";
			RamTempText.Text = "메모리 사용량";
		}
	}

	private void UpdateMonitor()
	{
		try
		{
			double cPUUsage = DeviceManager.GetCPUUsage();
			double gPUUsage = DeviceManager.GetGPUUsage();
			double rAMUsage = DeviceManager.GetRAMUsage();
			MonitorCpuText.Text = $"{cPUUsage:0}%";
			MonitorGpuText.Text = $"{gPUUsage:0}%";
			MonitorRamText.Text = $"{rAMUsage:0}%";
		}
		catch
		{
			MonitorCpuText.Text = "N/A";
			MonitorGpuText.Text = "N/A";
			MonitorRamText.Text = "N/A";
		}
	}

	private void FPSBoosterButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.ApplyFPSBooster();
			StatusText.Text = (flag ? "FPS 최적화 적용 완료" : "FPS 최적화 일부 적용 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "FPS 최적화 오류: " + ex.Message;
		}
	}

	private void GameModeButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.EnableGameMode();
			StatusText.Text = (flag ? "게임 모드 활성화 완료" : "게임 모드 활성화 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "게임 모드 오류: " + ex.Message;
		}
	}

	private void InputDelayButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = InputDelayOptimizer.Optimize();
			StatusText.Text = (flag ? "입력 지연 최적화 완료" : "입력 지연 최적화 일부 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "입력 지연 최적화 오류: " + ex.Message;
		}
	}

	private void ProcessOptimizerButton_Click(object sender, RoutedEventArgs e)
	{
		if (!ProcessOptimizer.IsFortniteRunning())
		{
			StatusText.Text = "포트나이트 실행 중에만 프로세스를 정리할 수 있습니다.";
			return;
		}
		if (!PrepareSettingsChange())
		{
			return;
		}
		try
		{
			int processCount = TweaksManager.GetProcessCount();
			int value = TweaksManager.OptimizeProcesses();
			int processCount2 = TweaksManager.GetProcessCount();
			StatusText.Text = $"프로세스 최적화 | {processCount} → {processCount2} | {value}개 정리";
		}
		catch (Exception ex)
		{
			StatusText.Text = "프로세스 최적화 오류: " + ex.Message;
		}
	}

	private void TempCleanerButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			int value = TweaksManager.CleanTempFiles();
			StatusText.Text = $"임시 파일 정리 완료 | {value}개 항목 정리";
		}
		catch (Exception ex)
		{
			StatusText.Text = "임시 파일 정리 오류: " + ex.Message;
		}
	}

	private void OpenTempFolderButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.OpenTempFolder();
			StatusText.Text = (flag ? "임시 폴더 열기 완료" : "임시 폴더 열기 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "임시 폴더 오류: " + ex.Message;
		}
	}

	private void DNSButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.FlushDNS();
			StatusText.Text = (flag ? "DNS 캐시 초기화 완료" : "DNS 초기화 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "DNS 오류: " + ex.Message;
		}
	}

	private void NetworkResetButton_Click(object sender, RoutedEventArgs e)
	{
		if (!PrepareSettingsChange())
		{
			return;
		}
		try
		{
			bool flag = TweaksManager.ResetNetwork();
			StatusText.Text = (flag ? "네트워크 초기화 완료" : "네트워크 초기화 일부 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "네트워크 오류: " + ex.Message;
		}
	}

	private void PingOptimizeButton_Click(object sender, RoutedEventArgs e)
	{
		if (!PrepareSettingsChange())
		{
			return;
		}
		try
		{
			if (PingOptimizer.Optimize())
			{
				StatusText.Text = "Ping 최적화 적용 완료";
				MessageBox.Show("Ping 최적화를 적용했습니다.\n\n적용 항목:\n• DNS 캐시 초기화\n• TCP Auto-Tuning 설정\n• RSS 활성화\n• ECN 설정 조정\n\n네트워크를 강제로 초기화하거나 재부팅을 요구하는 명령은 기본 실행에서 제외했습니다.", "KORIX Tweaks", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			else
			{
				StatusText.Text = "Ping 최적화 일부 실패";
				MessageBox.Show("필수 네트워크 초기화 명령 중 일부가 실패했습니다.\n\nWindows가 재부팅을 요구하거나 네트워크 서비스가 사용 중일 수 있습니다. 관리자 권한 문제는 아닙니다.", "KORIX Tweaks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}
		catch (Exception ex)
		{
			StatusText.Text = "Ping 최적화 오류";
			MessageBox.Show("Ping 최적화 중 오류가 발생했습니다.\n\n" + ex.Message, "KORIX Tweaks", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void HighPerformanceButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.EnableHighPerformance();
			StatusText.Text = (flag ? "고성능 전원 모드 활성화 완료" : "고성능 전원 모드 활성화 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "고성능 전원 모드 오류: " + ex.Message;
		}
	}

	private void UltimatePerformanceButton_Click(object sender, RoutedEventArgs e)
	{
		if (!PrepareSettingsChange())
		{
			return;
		}
		try
		{
			bool flag = TweaksManager.EnableUltimatePerformance();
			StatusText.Text = (flag ? "최고 성능 전원 모드 활성화 완료" : "최고 성능 전원 모드 활성화 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "최고 성능 전원 모드 오류: " + ex.Message;
		}
	}

	private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.RestoreDefaultPerformance();
			StatusText.Text = (flag ? "균형 전원 설정으로 복구 완료" : "전원 설정 복구 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "복구 오류: " + ex.Message;
		}
	}

	private void FortniteOptimizeButton_Click(object sender, RoutedEventArgs e)
	{
		if (!PrepareSettingsChange())
		{
			return;
		}
		try
		{
			bool flag = TweaksManager.ApplyFortniteOptimization();
			StatusText.Text = (flag ? "포트나이트 최적화 완료" : "포트나이트 최적화 일부 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "포트나이트 최적화 오류: " + ex.Message;
		}
	}

	private void FortniteConfigButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.OpenFortniteConfig();
			StatusText.Text = (flag ? "설정 폴더 열기 완료" : "설정 폴더 열기 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "설정 폴더 오류: " + ex.Message;
		}
	}

	private void TaskManagerButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.OpenTaskManager();
			StatusText.Text = (flag ? "작업 관리자 실행" : "작업 관리자 실행 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "작업 관리자 오류: " + ex.Message;
		}
	}

	private void DeviceManagerButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.OpenDeviceManager();
			StatusText.Text = (flag ? "장치 관리자 실행" : "장치 관리자 실행 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "장치 관리자 오류: " + ex.Message;
		}
	}

	private void SystemInformationButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			bool flag = TweaksManager.OpenSystemInformation();
			StatusText.Text = (flag ? "시스템 정보 실행" : "시스템 정보 실행 실패");
		}
		catch (Exception ex)
		{
			StatusText.Text = "시스템 정보 오류: " + ex.Message;
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		try
		{
			if (monitorTimer != null)
			{
				monitorTimer.Stop();
				monitorTimer.Tick -= MonitorTimer_Tick;
			}
		}
		catch
		{
		}
		try
		{
			TweaksManager.RestoreFortniteProcessPriorities();
		}
		catch
		{
		}
		try
		{
			DeviceManager.Dispose();
		}
		catch
		{
		}
		base.OnClosed(e);
	}
}
