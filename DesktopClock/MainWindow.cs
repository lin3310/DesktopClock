using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace DesktopClock;

public partial class MainWindow : Window, IComponentConnector
{
	private DispatcherTimer _timer;

	private TrayManager _trayManager;

	private ClockConfig _config = new ClockConfig();

	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_TOOLWINDOW = 128;

	public ClockConfig CurrentConfig => _config;

	public event Action<ClockConfig> ConfigChanged;

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	public MainWindow()
	{
		InitializeComponent();
		_config = ConfigManager.LoadConfig();
		ApplyConfiguration();
		_timer = new DispatcherTimer();
		_timer.Interval = TimeSpan.FromSeconds(1.0);
		_timer.Tick += Timer_Tick;
		_timer.Start();
		UpdateClock();
		InitializeTray();
		base.SourceInitialized += OnSourceInitialized;
	}

	private void OnSourceInitialized(object sender, EventArgs e)
	{
		EnableMousePassThrough();
		base.SizeChanged += OnWindowSizeChanged;
	}

	private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_config.IsDragMode || double.IsNaN(_config.WindowLeft))
		{
			return;
		}
		if (_config.Position == "center")
		{
			double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
			double screenHeight = SystemParameters.PrimaryScreenHeight;
			double newLeft = (primaryScreenWidth - e.NewSize.Width) / 2.0;
			double newTop = (screenHeight - e.NewSize.Height) / 2.0;
			base.Left = newLeft;
			base.Top = newTop;
		}
		else if (_config.Position == "top-right" || _config.Position == "bottom-right")
		{
			double deltaWidth = e.NewSize.Width - e.PreviousSize.Width;
			if (e.PreviousSize.Width > 0.0 && Math.Abs(deltaWidth) > 0.1)
			{
				base.Left -= deltaWidth;
			}
		}
	}

	private void EnableMousePassThrough()
	{
		try
		{
			IntPtr handle = new WindowInteropHelper(this).Handle;
			int extendedStyle = GetWindowLong(handle, -20);
			SetWindowLong(handle, -20, extendedStyle | 0x20 | 0x80);
		}
		catch
		{
		}
	}

	public void ApplyConfiguration()
	{
		_config = ConfigManager.LoadConfig();
		ClockText.FontFamily = new FontFamily(_config.FontFamily);
		ClockText.FontSize = _config.FontSize;
		string textColor = _config.TextColor;
		string shadowColor = _config.ShadowColor;
		if (_config.ThemeMode == "Auto")
		{
			textColor = ThemeHelper.GetTextColorForMode("Auto");
			shadowColor = ThemeHelper.GetShadowColorForMode("Auto");
		}
		else if (_config.ThemeMode == "Light")
		{
			textColor = "#000000";
			shadowColor = "#FFFFFF";
		}
		else if (_config.ThemeMode == "Dark")
		{
			textColor = "#FFFFFF";
			shadowColor = "#000000";
		}
		if (_config.ColorSource == "System")
		{
			Color accentColor = ThemeHelper.GetSystemAccentColor();
			if (_config.ColorAdjustment == "Brighten")
			{
				accentColor = ThemeHelper.ChangeColorBrightness(accentColor, 0.3f);
			}
			else if (_config.ColorAdjustment == "Darken")
			{
				accentColor = ThemeHelper.ChangeColorBrightness(accentColor, -0.3f);
			}
			textColor = $"#{accentColor.A:X2}{accentColor.R:X2}{accentColor.G:X2}{accentColor.B:X2}";
		}
		try
		{
			Color color = (Color)ColorConverter.ConvertFromString(textColor);
			ClockText.Foreground = new SolidColorBrush(color);
		}
		catch
		{
			ClockText.Foreground = new SolidColorBrush(Colors.White);
		}
		if (!string.IsNullOrEmpty(_config.FontWeight))
		{
			switch (_config.FontWeight)
			{
			case "Light":
				ClockText.FontWeight = FontWeights.Light;
				break;
			case "Normal":
				ClockText.FontWeight = FontWeights.Normal;
				break;
			case "SemiBold":
				ClockText.FontWeight = FontWeights.SemiBold;
				break;
			case "Bold":
				ClockText.FontWeight = FontWeights.Bold;
				break;
			default:
				ClockText.FontWeight = FontWeights.Light;
				break;
			}
		}
		base.Topmost = _config.AlwaysOnTop;
		MainGrid.Opacity = Math.Clamp(_config.ClockOpacity, 0.1, 1.0);
		if (_config.ShowDate)
		{
			DateText.Visibility = Visibility.Visible;
			DateText.FontFamily = new FontFamily(_config.FontFamily);
			DateText.FontSize = (double)_config.FontSize * 0.27;
			DateText.Foreground = ClockText.Foreground;
		}
		else
		{
			DateText.Visibility = Visibility.Collapsed;
		}
		if (_config.ShowShadow)
		{
			try
			{
				Color sColor = (Color)ColorConverter.ConvertFromString(shadowColor);
				DropShadowEffect dropShadow = new DropShadowEffect
				{
					BlurRadius = _config.ShadowBlurRadius,
					Color = sColor,
					ShadowDepth = 2.0,
					Opacity = 0.8,
					RenderingBias = RenderingBias.Performance
				};
				ClockText.Effect = dropShadow;
			}
			catch
			{
				ClockText.Effect = null;
			}
		}
		else
		{
			ClockText.Effect = null;
		}
		UpdateClock();
		if (!double.IsNaN(_config.WindowLeft) && !double.IsNaN(_config.WindowTop))
		{
			base.WindowStartupLocation = WindowStartupLocation.Manual;
			base.Left = _config.WindowLeft;
			base.Top = _config.WindowTop;
		}
		else
		{
			SetPosition(_config.Position);
		}
		this.ConfigChanged?.Invoke(_config);
	}

	private void Timer_Tick(object? sender, EventArgs e)
	{
		UpdateClock();
	}

	private void UpdateClock()
	{
		DateTime time = TimeZoneManager.GetTimeInZone(_config.TimeZone);
		ClockText.Text = time.ToString(_config.ShowSeconds ? "HH:mm:ss" : "HH:mm");
		if (_config.ShowDate && DateText.Visibility == Visibility.Visible)
		{
			DateText.Text = time.ToString(_config.DateFormat);
		}
	}

	private void SetPosition(string position)
	{
		base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)delegate
		{
			double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
			double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;
			double num = ((base.ActualWidth > 0.0) ? base.ActualWidth : 400.0);
			double num2 = ((base.ActualHeight > 0.0) ? base.ActualHeight : 200.0);
			switch (position)
			{
			case "top-left":
				base.Left = 20.0;
				base.Top = 20.0;
				break;
			case "top-right":
				base.Left = primaryScreenWidth - num - 20.0;
				base.Top = 20.0;
				break;
			case "bottom-left":
				base.Left = 20.0;
				base.Top = primaryScreenHeight - num2 - 40.0;
				break;
			case "bottom-right":
				base.Left = primaryScreenWidth - num - 20.0;
				base.Top = primaryScreenHeight - num2 - 40.0;
				break;
			default:
				base.Left = (primaryScreenWidth - num) / 2.0;
				base.Top = (primaryScreenHeight - num2) / 2.0;
				break;
			}
			_config.WindowLeft = base.Left;
			_config.WindowTop = base.Top;
		});
	}

	private void InitializeTray()
	{
		_trayManager = new TrayManager(this);
	}

	protected override void OnClosed(EventArgs e)
	{
		_trayManager?.Dispose();
		base.OnClosed(e);
	}
}
