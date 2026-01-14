// MIT License
// Copyright (c) 2026 lin3310 (林楷庭)
// SPDX-License-Identifier: MIT

namespace DesktopClock;

public class ClockConfig
{
	public string FontFamily { get; set; } = "Microsoft JhengHei";

	public int FontSize { get; set; } = 120;

	public string FontWeight { get; set; } = "Light";

	public string TextColor { get; set; } = "#FFFFFF";

	public bool ShowSeconds { get; set; }

	public string Position { get; set; } = "center";

	public bool StartWithWindows { get; set; }

	public bool AlwaysOnTop { get; set; } = true;

	public bool IsDragMode { get; set; }

	public double WindowLeft { get; set; } = double.NaN;

	public double WindowTop { get; set; } = double.NaN;

	public bool ShowShadow { get; set; } = true;

	public double ShadowBlurRadius { get; set; } = 8.0;

	public string ShadowColor { get; set; } = "#000000";

	public string ThemeMode { get; set; } = "Auto";

	public string ColorSource { get; set; } = "System";

	public string ColorAdjustment { get; set; } = "None";

	public string TimeZone { get; set; } = "Local";

	public bool ShowDate { get; set; }

	public string DateFormat { get; set; } = "MM/dd ddd";

	public double ClockOpacity { get; set; } = 1.0;
}
