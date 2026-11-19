using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MousePilot;

public partial class MainWindow : Window
{
    private const int WhBilenytyuzetLl = 13, WmBillentyuLenyomasa = 0x0100, WmSysBillentyuLenyomasa = 0x0104;
    private const int VkBal = 0x25, VkFel = 0x26, VkJobbra = 0x27, VkLe = 0x28, VkF4 = 0x73, VkMenu = 0x12;
    private const uint EgerBalraLe = 0x0002, EgerBalraFel = 0x0004, EgerJobbraLe = 0x0008, EgerJobbraFel = 0x0010, EgerKozepLent = 0x0020, EgerKozepFent = 0x0040;
    private readonly string UtBealitasa = Path.Combine(AppContext.BaseDirectory, "setting.txt");
    private readonly DispatcherTimer IdoBeallitasa = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly LowLevelKeyboardProc hookVisszaHivasa;
    private IntPtr hookKezeles;
    private int MozgatasiLepes = 10;
    private int BalBillentyu = VkBal, JobbraBillentyu = VkJobbra, FelBillentyu = VkFel, LeBillentyu = VkLe;
    private CancellationTokenSource? KatintasTorlese;

    public MainWindow()
    {
        InitializeComponent();
        hookVisszaHivasa = KeyboardHook;
        Loaded += OnLoaded;
        Closing += (_, _) => StopClicker();
        IdoBeallitasa.Tick += (_, _) => LoadSettings();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(UtBealitasa)) File.WriteAllText(UtBealitasa, "# Change these values; keys accept names such as Left, A, or F6. Reloaded every second.\nMoveStep=10\nLeftKey=Left\nRightKey=Right\nUpKey=Up\nDownKey=Down\n");
        LoadSettings();
        hookKezeles = SetWindowsHookEx(WhBilenytyuzetLl, hookVisszaHivasa, GetModuleHandle(null), 0);
        if (hookKezeles == IntPtr.Zero) StatusText.Text = "Could not enable global keyboard controls. Try running with normal desktop access.";
        IdoBeallitasa.Start();
    }

    private void LoadSettings()
    {
        try
        {
            int newStep = MozgatasiLepes, l = BalBillentyu, r = JobbraBillentyu, u = FelBillentyu, d = LeBillentyu;
            foreach (var raw in File.ReadLines(UtBealitasa))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#') || !line.Contains('=')) continue;
                var pair = line.Split('=', 2); var key = pair[0].Trim(); var value = pair[1].Trim();
                if (key.Equals("MoveStep", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var step)) newStep = Math.Clamp(step, 1, 500);
                else if (key.Equals("LeftKey", StringComparison.OrdinalIgnoreCase)) l = ParseKey(value, l);
                else if (key.Equals("RightKey", StringComparison.OrdinalIgnoreCase)) r = ParseKey(value, r);
                else if (key.Equals("UpKey", StringComparison.OrdinalIgnoreCase)) u = ParseKey(value, u);
                else if (key.Equals("DownKey", StringComparison.OrdinalIgnoreCase)) d = ParseKey(value, d);
            }
            MozgatasiLepes = newStep; BalBillentyu = l; JobbraBillentyu = r; FelBillentyu = u; LeBillentyu = d;
            KeybindSummary.Text = $"Move {KulcsNeve(BalBillentyu)} / {KulcsNeve(JobbraBillentyu)} / {KulcsNeve(FelBillentyu)} / {KulcsNeve(LeBillentyu)}    •    Step {MozgatasiLepes}px    •    Settings: {UtBealitasa}";
        }
        catch (IOException) { }
    }

    private static int ParseKey(string name, int fallback)
    {
        if (name.Length == 1 && char.IsLetterOrDigit(name[0])) return char.ToUpperInvariant(name[0]);
        return name.Trim().ToUpperInvariant() switch { "LEFT" => VkBal, "RIGHT" => VkJobbra, "UP" => VkFel, "DOWN" => VkLe, "SPACE" => 0x20, "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => VkF4, "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77, "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B, _ => fallback };
    }
    private static string KulcsNeve(int code) => code switch { VkBal => "Left", VkJobbra => "Right", VkFel => "Up", VkLe => "Down", _ => code >= 0x30 && code <= 0x5A ? ((char)code).ToString() : $"VK {code}" };

    private IntPtr KeyboardHook(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && (wParam == (IntPtr)WmBillentyuLenyomasa || wParam == (IntPtr)WmSysBillentyuLenyomasa))
        {
            var key = Marshal.PtrToStructure<KbdLlHookStruct>(lParam).vkCode;
            if (key == VkF4 && (GetAsyncKeyState(VkMenu) & 0x8000) != 0)
            {
                Dispatcher.BeginInvoke(new Action(Close));
                return (IntPtr)1;
            }
            int dx = key == BalBillentyu ? -MozgatasiLepes : key == JobbraBillentyu ? MozgatasiLepes : 0;
            int dy = key == FelBillentyu ? -MozgatasiLepes : key == LeBillentyu ? MozgatasiLepes : 0;
            if (dx != 0 || dy != 0)
            {
                GetCursorPos(out var p); SetCursorPos(p.X + dx, p.Y + dy);
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(hookKezeles, code, wParam, lParam);
    }

    private void MoveCursor_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(XBox.Text, out var x) && int.TryParse(YBox.Text, out var y)) { SetCursorPos(x, y); StatusText.Text = $"Cursor moved to ({x}, {y})."; }
        else StatusText.Text = "Enter whole-number screen coordinates for X and Y.";
    }
    private void ReadPosition_Click(object sender, RoutedEventArgs e) { GetCursorPos(out var p); XBox.Text = p.X.ToString(); YBox.Text = p.Y.ToString(); ClickXBox.Text = p.X.ToString(); ClickYBox.Text = p.Y.ToString(); }

    private void ToggleClicker_Click(object sender, RoutedEventArgs e)
    {
        if (KatintasTorlese is not null) { StopClicker(); return; }
        if (!double.TryParse(RateBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var cps) || cps < 0.1 || cps > 200 || !int.TryParse(ClickXBox.Text, out _) || !int.TryParse(ClickYBox.Text, out _))
        { StatusText.Text = "Enter a click rate from 0.1 to 200 and whole-number X/Y coordinates."; return; }
        var x = int.Parse(ClickXBox.Text); var y = int.Parse(ClickYBox.Text);
        var item = (ComboBoxItem)ClickTypeBox.SelectedItem; var type = item.Content?.ToString() ?? "Left";
        KatintasTorlese = new CancellationTokenSource(); ClickButton.Content = "Stop clicking"; StatusText.Text = $"Clicking {type.ToLowerInvariant()} at {cps:0.##} CPS at ({x}, {y}).";
        _ = ClickLoopAsync(cps, type, x, y, KatintasTorlese.Token);
    }
    private async Task ClickLoopAsync(double cps, string type, int x, int y, CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(1.0 / cps);
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (MoveToClickPositionBox.IsChecked == true) SetCursorPos(x, y);
                var (down, up) = type switch { "Right" => (EgerJobbraLe, EgerJobbraFel), "Middle" => (EgerKozepLent, EgerKozepFent), _ => (EgerBalraLe, EgerBalraFel) };
                mouse_event(down, 0, 0, 0, UIntPtr.Zero); mouse_event(up, 0, 0, 0, UIntPtr.Zero);
                await Task.Delay(interval, token);
            }
        }
        catch (OperationCanceledException) { }
    }
    private void StopClicker() { KatintasTorlese?.Cancel(); KatintasTorlese?.Dispose(); KatintasTorlese = null; if (ClickButton is not null) ClickButton.Content = "Start clicking"; }

    protected override void OnClosed(EventArgs e)
    {
        IdoBeallitasa.Stop(); StopClicker();
        if (hookKezeles != IntPtr.Zero) UnhookWindowsHookEx(hookKezeles);
        base.OnClosed(e);
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct KbdLlHookStruct { public int vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
}
