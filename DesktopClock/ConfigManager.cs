using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

// MIT License
// Copyright (c) 2026 lin3310 (林楷庭)
// SPDX-License-Identifier: MIT

namespace DesktopClock;

public static class ConfigManager
{
	private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopClock", "config.json");

	private static ClockConfig? _cachedConfig;

	private static DateTime _lastReadTime = DateTime.MinValue;

	private const int CacheExpirySeconds = 5;

	public static void InvalidateCache()
	{
		_cachedConfig = null;
	}

	public static ClockConfig LoadConfig()
	{
		if (_cachedConfig != null && (DateTime.Now - _lastReadTime).TotalSeconds < 5.0)
		{
			return _cachedConfig;
		}
		if (!File.Exists(ConfigPath))
		{
			_cachedConfig = new ClockConfig();
			_lastReadTime = DateTime.Now;
			return _cachedConfig;
		}
		try
		{
			_cachedConfig = JsonSerializer.Deserialize<ClockConfig>(File.ReadAllText(ConfigPath)) ?? new ClockConfig();
			_lastReadTime = DateTime.Now;
			return _cachedConfig;
		}
		catch
		{
			_cachedConfig = new ClockConfig();
			_lastReadTime = DateTime.Now;
			return _cachedConfig;
		}
	}

	public static void SaveConfig(ClockConfig config)
	{
		string directory = Path.GetDirectoryName(ConfigPath);
		if (directory != null)
		{
			Directory.CreateDirectory(directory);
		}
		string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(ConfigPath, json);
		InvalidateCache();
	}

	public static void SetStartup(bool enable)
	{
		string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "DesktopClock.lnk");
		if (enable)
		{
			CreateShortcut(startupPath);
		}
		else if (File.Exists(startupPath))
		{
			File.Delete(startupPath);
		}
		ClockConfig clockConfig = LoadConfig();
		clockConfig.StartWithWindows = enable;
		SaveConfig(clockConfig);
	}

	public static bool IsInstalled()
	{
		string a = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
		string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopClock").TrimEnd('\\');
		return string.Equals(a, installPath, StringComparison.OrdinalIgnoreCase);
	}

	public static void InstallApp()
	{
		string sourcePath = AppDomain.CurrentDomain.BaseDirectory;
		string targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopClock");
		string exeName = "DesktopClock.exe";
		try
		{
			if (!Directory.Exists(targetPath))
			{
				Directory.CreateDirectory(targetPath);
			}
			string[] files = Directory.GetFiles(sourcePath);
			foreach (string obj in files)
			{
				string fileName = Path.GetFileName(obj);
				string destFile = Path.Combine(targetPath, fileName);
				File.Copy(obj, destFile, overwrite: true);
			}
			files = Directory.GetDirectories(sourcePath);
			foreach (string obj2 in files)
			{
				string dirName = Path.GetFileName(obj2);
				string destDir = Path.Combine(targetPath, dirName);
				CopyDirectory(obj2, destDir);
			}
			CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Desktop Clock.lnk"), Path.Combine(targetPath, exeName), targetPath);
			Process.Start(Path.Combine(targetPath, exeName));
			Application.Current.Shutdown();
		}
		catch (Exception ex)
		{
			MessageBox.Show("安裝失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	public static void UninstallApp()
	{
		string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopClock");
		string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopClock");
		string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Desktop Clock.lnk");
		string startupShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "DesktopClock.lnk");
		try
		{
			if (File.Exists(desktopShortcut))
			{
				File.Delete(desktopShortcut);
			}
			if (File.Exists(startupShortcut))
			{
				File.Delete(startupShortcut);
			}
			if (Directory.Exists(configPath))
			{
				Directory.Delete(configPath, recursive: true);
			}
			string script = "\r\n                    timeout /t 2 /nobreak >nul\r\n                    rmdir /s /q \"" + installPath + "\"\r\n                ";
			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c \"" + script + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			MessageBox.Show("解除安裝完成！\n程式將在 2 秒後完全移除。", "完成", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			Application.Current.Shutdown();
		}
		catch (Exception ex)
		{
			MessageBox.Show("解除安裝失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private static void CopyDirectory(string sourceDir, string destinationDir)
	{
		DirectoryInfo dir = new DirectoryInfo(sourceDir);
		if (!dir.Exists)
		{
			throw new DirectoryNotFoundException("Source directory not found: " + dir.FullName);
		}
		Directory.CreateDirectory(destinationDir);
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files)
		{
			string targetFilePath = Path.Combine(destinationDir, file.Name);
			file.CopyTo(targetFilePath, overwrite: true);
		}
		DirectoryInfo[] directories = dir.GetDirectories();
		foreach (DirectoryInfo subDir in directories)
		{
			string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
			CopyDirectory(subDir.FullName, newDestinationDir);
		}
	}

	private static void CreateShortcut(string shortcutPath, string targetPath = null, string workDir = null)
	{
		string exePath = targetPath ?? Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
		if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
		{
			exePath = Path.ChangeExtension(exePath, ".exe");
		}
		string workDirectory = workDir ?? AppDomain.CurrentDomain.BaseDirectory;
		try
		{
			string script = $"\r\n                    $WshShell = New-Object -ComObject WScript.Shell\r\n                    $Shortcut = $WshShell.CreateShortcut('{shortcutPath}')\r\n                    $Shortcut.TargetPath = '{exePath}'\r\n                    $Shortcut.WorkingDirectory = '{workDirectory}'\r\n                    $Shortcut.Description = '桌面時鐘 - 低功耗筆電專用'\r\n                    $Shortcut.Save()\r\n                ";
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
				UseShellExecute = false,
				CreateNoWindow = true
			});
			process?.WaitForExit();
		}
		catch
		{
		}
	}
}
