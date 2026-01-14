// MIT License
// Copyright (c) 2026 lin3310 (林楷庭)
// SPDX-License-Identifier: MIT

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace DesktopClock; 

public class App : Application
{
	private static Mutex? _mutex;

	protected override void OnStartup(StartupEventArgs e)
	{
		_mutex = new Mutex(initiallyOwned: true, "DesktopClock_SingleInstance", out var createdNew);
		if (!createdNew)
		{
			MessageBox.Show("桌面時鐘已在運行中", "提示", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			Shutdown();
		}
		else
		{
			base.OnStartup(e);
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_mutex?.ReleaseMutex();
		_mutex?.Dispose();
		base.OnExit(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.22.0")]
	public void InitializeComponent()
	{
		base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.22.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
