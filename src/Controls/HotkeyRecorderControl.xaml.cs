using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SemaphURL.Controls;

public partial class HotkeyRecorderControl : UserControl
{
    private bool _isRecording;

    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(string),
            typeof(HotkeyRecorderControl),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnHotkeyChanged));

    public static readonly DependencyProperty DefaultHotkeyProperty =
        DependencyProperty.Register(
            nameof(DefaultHotkey),
            typeof(string),
            typeof(HotkeyRecorderControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShowResetButtonProperty =
        DependencyProperty.Register(
            nameof(ShowResetButton),
            typeof(bool),
            typeof(HotkeyRecorderControl),
            new PropertyMetadata(true));

    public string Hotkey
    {
        get => (string)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public string DefaultHotkey
    {
        get => (string)GetValue(DefaultHotkeyProperty);
        set => SetValue(DefaultHotkeyProperty, value);
    }

    public bool ShowResetButton
    {
        get => (bool)GetValue(ShowResetButtonProperty);
        set => SetValue(ShowResetButtonProperty, value);
    }

    public HotkeyRecorderControl()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        LostFocus += OnLostFocus;
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyRecorderControl control && !control._isRecording)
        {
            control.HotkeyText.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        StartRecording();
        e.Handled = true;
    }

    private void StartRecording()
    {
        _isRecording = true;
        HotkeyText.Text = "Press your shortcut...";
        HotkeyText.Foreground = new SolidColorBrush(Color.FromRgb(0x9B, 0x88, 0xFF));
        HotkeyBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x7B, 0x68, 0xEE));
        HotkeyBorder.BorderThickness = new Thickness(2);
        Focus();
        Keyboard.Focus(this);
    }

    private void StopRecording(bool restoreText = true)
    {
        _isRecording = false;
        if (restoreText)
        {
            HotkeyText.Text = Hotkey;
        }
        HotkeyText.ClearValue(ForegroundProperty);
        HotkeyBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x31, 0x32, 0x44));
        HotkeyBorder.BorderThickness = new Thickness(1);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecording)
            return;

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore modifier-only keys
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Cancel on Escape
        if (key == Key.Escape)
        {
            StopRecording();
            return;
        }

        // Build hotkey string
        var modifiers = Keyboard.Modifiers;
        var hotkeyParts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control))
            hotkeyParts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            hotkeyParts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            hotkeyParts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows))
            hotkeyParts.Add("Win");

        // Require at least one modifier
        if (hotkeyParts.Count == 0)
            return;

        hotkeyParts.Add(GetKeyName(key));

        Hotkey = string.Join("+", hotkeyParts);
        HotkeyText.Text = Hotkey;
        StopRecording(restoreText: false);
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(DefaultHotkey))
        {
            Hotkey = DefaultHotkey;
            HotkeyText.Text = DefaultHotkey;
        }
    }

    private static string GetKeyName(Key key)
    {
        return key switch
        {
            Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
            Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
            Key.NumPad0 => "Num0", Key.NumPad1 => "Num1", Key.NumPad2 => "Num2",
            Key.NumPad3 => "Num3", Key.NumPad4 => "Num4", Key.NumPad5 => "Num5",
            Key.NumPad6 => "Num6", Key.NumPad7 => "Num7", Key.NumPad8 => "Num8", Key.NumPad9 => "Num9",
            Key.OemPlus => "+", Key.OemMinus => "-", Key.OemComma => ",", Key.OemPeriod => ".",
            Key.Space => "Space",
            _ => key.ToString()
        };
    }
}
