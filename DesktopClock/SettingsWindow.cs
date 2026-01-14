// MIT License
// Copyright (c) 2026 lin3310 (林楷庭)
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace DesktopClock;

public partial class SettingsWindow : Window, IComponentConnector
{
	private MainWindow _mainWindow;

	private bool _isLoaded;

	public SettingsWindow(MainWindow mainWindow)
	{
		InitializeComponent();
		_mainWindow = mainWindow;
		LoadCurrentConfig();
		LoadSystemFonts();
		LoadTimeZones();
		_isLoaded = true;
	}

	private void LoadCurrentConfig()
	{
		ClockConfig config = ConfigManager.LoadConfig();
		if (!ConfigManager.IsInstalled())
		{
			InstallPanel.Visibility = Visibility.Visible;
			UninstallPanel.Visibility = Visibility.Collapsed;
		}
		else
		{
			InstallPanel.Visibility = Visibility.Collapsed;
			UninstallPanel.Visibility = Visibility.Visible;
		}
		FontSizeSlider.Value = config.FontSize;
		FontSizeText.Text = config.FontSize.ToString();
		ShowSecondsCheckBox.IsChecked = config.ShowSeconds;
		StartWithWindowsCheckBox.IsChecked = config.StartWithWindows;
		AlwaysOnTopCheckBox.IsChecked = config.AlwaysOnTop;
		PositionComboBox.SelectedIndex = 0;
		foreach (ComboBoxItem item in (IEnumerable)ThemeModeComboBox.Items)
		{
			if (item.Tag.ToString() == config.ThemeMode)
			{
				item.IsSelected = true;
				break;
			}
		}
		TextColorBox.Text = config.TextColor;
		SystemAccentCheckBox.IsChecked = config.ColorSource == "System";
		foreach (ComboBoxItem item2 in (IEnumerable)ColorAdjComboBox.Items)
		{
			if (item2.Tag.ToString() == config.ColorAdjustment)
			{
				item2.IsSelected = true;
				break;
			}
		}
		UpdateColorPanelVisibility();
		foreach (ComboBoxItem item3 in (IEnumerable)FontWeightComboBox.Items)
		{
			if (item3.Tag.ToString() == config.FontWeight)
			{
				item3.IsSelected = true;
				break;
			}
		}
		ShowShadowCheckBox.IsChecked = config.ShowShadow;
		ShadowBlurSlider.Value = config.ShadowBlurRadius;
		ShadowColorBox.Text = config.ShadowColor;
		OpacitySlider.Value = config.ClockOpacity;
		ShowDateCheckBox.IsChecked = config.ShowDate;
		foreach (ComboBoxItem item4 in (IEnumerable)DateFormatComboBox.Items)
		{
			if (item4.Tag.ToString() == config.DateFormat)
			{
				item4.IsSelected = true;
				break;
			}
		}
		if (DateFormatComboBox.SelectedItem == null && DateFormatComboBox.Items.Count > 0)
		{
			DateFormatComboBox.SelectedIndex = 0;
		}
		double screenWidth = SystemParameters.PrimaryScreenWidth;
		double screenHeight = SystemParameters.PrimaryScreenHeight;
		double winLeft = ((!double.IsNaN(config.WindowLeft)) ? config.WindowLeft : _mainWindow.Left);
		double winTop = ((!double.IsNaN(config.WindowTop)) ? config.WindowTop : _mainWindow.Top);
		if (screenWidth > 0.0)
		{
			PosXSlider.Value = winLeft / (screenWidth - _mainWindow.ActualWidth) * 100.0;
		}
		if (screenHeight > 0.0)
		{
			PosYSlider.Value = winTop / (screenHeight - _mainWindow.ActualHeight) * 100.0;
		}
		FontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
		_isLoaded = true;
		_mainWindow.ConfigChanged += OnConfigChanged;
	}

	protected override void OnClosed(EventArgs e)
	{
		_mainWindow.ConfigChanged -= OnConfigChanged;
		base.OnClosed(e);
	}

	private void OnConfigChanged(ClockConfig config)
	{
		base.Dispatcher.Invoke(delegate
		{
			FontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
			FontSizeSlider.Value = config.FontSize;
			FontSizeText.Text = config.FontSize.ToString();
			FontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
		});
	}

	private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		FontSizeText.Text = ((int)e.NewValue).ToString();
	}

	private void OnPositionSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_isLoaded)
		{
			if (PositionComboBox.SelectedIndex != 0)
			{
				PositionComboBox.SelectedIndex = 0;
			}
			UpdateWindowPositionFromSliders();
		}
	}

	private void LoadSystemFonts()
	{
		string currentFont = ConfigManager.LoadConfig().FontFamily;
		List<string> fonts = (from f in Fonts.SystemFontFamilies
			where !string.IsNullOrEmpty(f.Source)
			where !f.Source.StartsWith("Wingdings", StringComparison.OrdinalIgnoreCase)
			where !f.Source.StartsWith("Webdings", StringComparison.OrdinalIgnoreCase)
			where !f.Source.StartsWith("Symbol", StringComparison.OrdinalIgnoreCase)
			where !f.Source.StartsWith("Marlett", StringComparison.OrdinalIgnoreCase)
			where !f.Source.StartsWith("HoloLens MDL2 Assets", StringComparison.OrdinalIgnoreCase)
			where !f.Source.StartsWith("Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase)
			select f.Source into f
			orderby f
			select f).ToList();
		FontFamilyCombo.ItemsSource = fonts;
		FontFamilyCombo.SelectedItem = currentFont;
		if (FontFamilyCombo.SelectedItem == null && FontFamilyCombo.Items.Count > 0)
		{
			FontFamilyCombo.SelectedIndex = 0;
		}
	}

	private void LoadTimeZones()
	{
		ClockConfig config = ConfigManager.LoadConfig();
		TimeZoneStatusText.Text = "正在載入時區...";
		List<string> timeZones = TimeZoneManager.GetSystemTimeZones();
		UpdateTimeZoneCombo(timeZones, config.TimeZone);
		TimeZoneStatusText.Text = "已載入本地時區";
	}

	private void UpdateTimeZoneCombo(List<string> timeZones, string currentZone)
	{
		List<string> displayList = new List<string> { "Local" };
		displayList.AddRange(timeZones);
		TimeZoneComboBox.ItemsSource = displayList;
		TimeZoneComboBox.SelectedItem = currentZone;
		if (TimeZoneComboBox.SelectedItem == null)
		{
			TimeZoneComboBox.SelectedItem = "Local";
		}
	}

	private async void OnRefreshTimeZonesClick(object sender, RoutedEventArgs e)
	{
		TimeZoneStatusText.Text = "正在從網路獲取時區...";
		Button button = sender as Button;
		if (button != null)
		{
			button.IsEnabled = false;
		}
		List<string> timeZones = await TimeZoneManager.GetTimeZonesAsync();
		ClockConfig config = ConfigManager.LoadConfig();
		UpdateTimeZoneCombo(timeZones, config.TimeZone);
		TimeZoneStatusText.Text = $"已獲取 {timeZones.Count} 個時區 (WorldTimeAPI)";
		if (button != null)
		{
			button.IsEnabled = true;
		}
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ClockConfig obj = new ClockConfig
			{
				FontFamily = (FontFamilyCombo.SelectedItem?.ToString() ?? "Microsoft JhengHei"),
				FontSize = (int)FontSizeSlider.Value,
				ShowSeconds = (ShowSecondsCheckBox.IsChecked == true),
				Position = ((PositionComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "center"),
				StartWithWindows = (StartWithWindowsCheckBox.IsChecked == true),
				AlwaysOnTop = (AlwaysOnTopCheckBox.IsChecked == true),
				ThemeMode = ((ThemeModeComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Auto"),
				ColorSource = ((SystemAccentCheckBox.IsChecked == true) ? "System" : "Custom"),
				ColorAdjustment = ((ColorAdjComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "None"),
				TextColor = TextColorBox.Text,
				FontWeight = ((FontWeightComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Light"),
				ShowShadow = (ShowShadowCheckBox.IsChecked == true),
				ShadowBlurRadius = ShadowBlurSlider.Value,
				ShadowColor = ShadowColorBox.Text,
				TimeZone = (TimeZoneComboBox.SelectedItem?.ToString() ?? "Local"),
				ShowDate = (ShowDateCheckBox.IsChecked == true),
				DateFormat = ((DateFormatComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "MM/dd ddd"),
				ClockOpacity = OpacitySlider.Value,
				IsDragMode = false,
				WindowLeft = _mainWindow.Left,
				WindowTop = _mainWindow.Top
			};
			ConfigManager.SaveConfig(obj);
			ConfigManager.SetStartup(obj.StartWithWindows);
			base.DialogResult = true;
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show("保存設定時發生錯誤: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void OnPositionComboChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isLoaded)
		{
			return;
		}
		string selectedTag = (PositionComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString();
		if (!string.IsNullOrEmpty(selectedTag) && !(selectedTag == "custom"))
		{
			PosXSlider.ValueChanged -= OnPositionSliderChanged;
			PosYSlider.ValueChanged -= OnPositionSliderChanged;
			switch (selectedTag)
			{
			case "center":
				PosXSlider.Value = 50.0;
				PosYSlider.Value = 50.0;
				break;
			case "top-left":
				PosXSlider.Value = 2.0;
				PosYSlider.Value = 5.0;
				break;
			case "top-right":
				PosXSlider.Value = 98.0;
				PosYSlider.Value = 5.0;
				break;
			case "bottom-left":
				PosXSlider.Value = 2.0;
				PosYSlider.Value = 95.0;
				break;
			case "bottom-right":
				PosXSlider.Value = 98.0;
				PosYSlider.Value = 95.0;
				break;
			}
			UpdateWindowPositionFromSliders();
			PosXSlider.ValueChanged += OnPositionSliderChanged;
			PosYSlider.ValueChanged += OnPositionSliderChanged;
		}
	}

	private void UpdateWindowPositionFromSliders()
	{
		double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
		double screenHeight = SystemParameters.PrimaryScreenHeight;
		double actualWidth = ((_mainWindow.ActualWidth > 0.0) ? _mainWindow.ActualWidth : 400.0);
		double actualHeight = ((_mainWindow.ActualHeight > 0.0) ? _mainWindow.ActualHeight : 200.0);
		double xPercent = PosXSlider.Value / 100.0;
		double yPercent = PosYSlider.Value / 100.0;
		double newLeft = (primaryScreenWidth - actualWidth) * xPercent;
		double newTop = (screenHeight - actualHeight) * yPercent;
		_mainWindow.Left = newLeft;
		_mainWindow.Top = newTop;
	}

	private void OnInstallClick(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("確定要安裝 Desktop Clock 嗎？\n\n這將會：\n1. 將程式複製到系統資料夾\n2. 創建桌面捷徑\n3. 重啟應用程式\n\n這樣可以確保您的設定和位置記憶永久有效。", "安裝確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
		{
			ConfigManager.InstallApp();
		}
	}

	private void OnUninstallClick(object sender, RoutedEventArgs e)
	{
		if (MessageBox.Show("確定要解除安裝 Desktop Clock 嗎？\n\n這將會：\n1. 刪除程式檔案\n2. 移除桌面捷徑\n3. 刪除所有設定\n\n此操作無法復原！", "解除安裝確認", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			ConfigManager.UninstallApp();
		}
	}

	private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateColorPanelVisibility();
	}

	private void OnAccentColorChecked(object sender, RoutedEventArgs e)
	{
		UpdateColorPanelVisibility();
	}

	private void OnAccentColorUnchecked(object sender, RoutedEventArgs e)
	{
		UpdateColorPanelVisibility();
	}

	private void UpdateColorPanelVisibility()
	{
		if (ThemeModeComboBox == null || CustomColorPanel == null || SystemAccentCheckBox == null)
		{
			return;
		}
		string selectedMode = (ThemeModeComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString();
		bool useSystemAccent = SystemAccentCheckBox.IsChecked == true;
		if (selectedMode == "Custom" && !useSystemAccent)
		{
			CustomColorPanel.Visibility = Visibility.Visible;
		}
		else
		{
			CustomColorPanel.Visibility = Visibility.Collapsed;
		}
		SystemAccentCheckBox.Visibility = ((!(selectedMode == "Custom") && !(selectedMode == "Auto")) ? Visibility.Collapsed : Visibility.Visible);
		if (SystemAccentCheckBox.Visibility == Visibility.Visible && useSystemAccent)
		{
			if (ColorAdjPanel != null)
			{
				ColorAdjPanel.Visibility = Visibility.Visible;
			}
		}
		else if (ColorAdjPanel != null)
		{
			ColorAdjPanel.Visibility = Visibility.Collapsed;
		}
	}
}
