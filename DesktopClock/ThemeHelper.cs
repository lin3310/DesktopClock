using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopClock;

public static class ThemeHelper
{
	public static bool IsSystemDarkMode()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			return key?.GetValue("AppsUseLightTheme") is int i && i == 0;
		}
		catch
		{
			return false;
		}
	}

	public static Color GetSystemAccentColor()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\DWM");
			if (key != null && key.GetValue("ColorizationColor") is int colorInt)
			{
				byte[] bytes = BitConverter.GetBytes(colorInt);
				return Color.FromArgb(byte.MaxValue, bytes[2], bytes[1], bytes[0]);
			}
		}
		catch
		{
		}
		try
		{
			return SystemParameters.WindowGlassColor;
		}
		catch
		{
		}
		return Colors.DodgerBlue;
	}

	public static string GetTextColorForMode(string themeMode)
	{
		switch (themeMode)
		{
		case "Light":
			return "#000000";
		case "Dark":
			return "#FFFFFF";
		case "Auto":
			if (!IsSystemDarkMode())
			{
				return "#000000";
			}
			return "#FFFFFF";
		default:
			return "#FFFFFF";
		}
	}

	public static string GetShadowColorForMode(string themeMode)
	{
		switch (themeMode)
		{
		case "Light":
			return "#FFFFFF";
		case "Dark":
			return "#000000";
		case "Auto":
			if (!IsSystemDarkMode())
			{
				return "#FFFFFF";
			}
			return "#000000";
		default:
			return "#000000";
		}
	}

	public static Color ChangeColorBrightness(Color color, float factor)
	{
		float r = (int)color.R;
		float g = (int)color.G;
		float b = (int)color.B;
		if (factor < 0f)
		{
			factor = 1f + factor;
			r *= factor;
			g *= factor;
			b *= factor;
		}
		else
		{
			r = (255f - r) * factor + r;
			g = (255f - g) * factor + g;
			b = (255f - b) * factor + b;
		}
		return Color.FromArgb(color.A, (byte)Math.Min(255f, Math.Max(0f, r)), (byte)Math.Min(255f, Math.Max(0f, g)), (byte)Math.Min(255f, Math.Max(0f, b)));
	}
}
