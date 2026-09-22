using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace KORIX_tweaks;

public class App : Application
{
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.12.0")]
	public void InitializeComponent()
	{
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.12.0")]
	public static void Main()
	{
		if (!IsAdministrator())
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = Environment.ProcessPath,
					UseShellExecute = true,
					Verb = "runas"
				});
			}
			catch
			{
				MessageBox.Show("관리자 권한이 필요합니다.", "KORIX Tweaks", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			return;
		}
		App app = new App();
		app.InitializeComponent();
		app.Run(new KORIX_Tweaks.MainWindow());
	}

	private static bool IsAdministrator()
	{
		using WindowsIdentity identity = WindowsIdentity.GetCurrent();
		return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
	}
}
