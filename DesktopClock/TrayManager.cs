using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Resources;

namespace DesktopClock;

public class TrayManager : IDisposable
{
	private NotifyIcon? _trayIcon;

	private readonly MainWindow _mainWindow;

	public TrayManager(MainWindow mainWindow)
	{
		_mainWindow = mainWindow;
		InitializeTrayIconAsync();
	}

	private void InitializeTrayIconAsync()
	{
		_trayIcon = new NotifyIcon
		{
			Icon = SystemIcons.Application,
			Visible = true,
			Text = "桌面時鐘"
		};
		_trayIcon.ContextMenuStrip = CreateContextMenu();
		_trayIcon.DoubleClick += delegate
		{
			OpenSettings();
		};
		Task.Run(delegate
		{
			LoadCustomIconAsync();
		});
	}

	private void LoadCustomIconAsync()
	{
		try
		{
			System.Windows.Application.Current.Dispatcher.Invoke(delegate
			{
				StreamResourceInfo resourceStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/DesktopClock;component/Resources/clock_icon.png"));
				if (resourceStream != null)
				{
					using (Stream stream = resourceStream.Stream)
					{
						using Bitmap bitmap = new Bitmap(stream);
						Icon icon = Icon.FromHandle(bitmap.GetHicon());
						if (_trayIcon != null)
						{
							_trayIcon.Icon = icon;
						}
					}
				}
			});
		}
		catch
		{
		}
	}

	private ContextMenuStrip CreateContextMenu()
	{
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		ToolStripMenuItem settingsItem = new ToolStripMenuItem("⚙\ufe0f 設置...");
		settingsItem.Click += delegate
		{
			OpenSettings();
		};
		contextMenuStrip.Items.Add(settingsItem);
		ToolStripMenuItem reloadItem = new ToolStripMenuItem("\ud83d\udd04 重新載入配置");
		reloadItem.Click += delegate
		{
			ReloadClock();
		};
		contextMenuStrip.Items.Add(reloadItem);
		contextMenuStrip.Items.Add(new ToolStripSeparator());
		ClockConfig config = ConfigManager.LoadConfig();
		ToolStripMenuItem startupItem = new ToolStripMenuItem("\ud83d\udccd 開機自動啟動")
		{
			CheckOnClick = true,
			Checked = config.StartWithWindows
		};
		startupItem.Click += delegate
		{
			ToggleStartup(startupItem.Checked);
		};
		contextMenuStrip.Items.Add(startupItem);
		contextMenuStrip.Items.Add(new ToolStripSeparator());
		ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ 退出");
		exitItem.Click += delegate
		{
			ExitApplication();
		};
		contextMenuStrip.Items.Add(exitItem);
		return contextMenuStrip;
	}

	private void OpenSettings()
	{
		try
		{
			if (new SettingsWindow(_mainWindow).ShowDialog() == true)
			{
				ReloadClock();
			}
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show("無法打開設置窗口：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ReloadClock()
	{
		_mainWindow.ApplyConfiguration();
	}

	private void ToggleStartup(bool enable)
	{
		try
		{
			ConfigManager.SetStartup(enable);
			_trayIcon.ShowBalloonTip(2000, "桌面時鐘", enable ? "已設置開機自動啟動" : "已取消開機自動啟動", ToolTipIcon.Info);
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show("設置開機自啟失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ExitApplication()
	{
		System.Windows.Application.Current.Shutdown();
	}

	public void Dispose()
	{
		if (_trayIcon != null)
		{
			_trayIcon.Visible = false;
			_trayIcon.Dispose();
			_trayIcon = null;
		}
	}
}
