using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TodoWidget.Data;
using TodoWidget.Models;
using TodoWidget.Services;
using TodoWidget.ViewModels;

namespace TodoWidget;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;
    private TodoRepository _repo = null!;
    private SettingsService _settingsSvc = null!;
    private double _lx, _ly, _lw, _lh;
    private bool _drag; private Point _dp;

    public MainWindow()
    {
        InitializeComponent();
        try { var f = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); System.IO.Directory.CreateDirectory(f); _repo = new TodoRepository(System.IO.Path.Combine(f, "todos.db")); _settingsSvc = new SettingsService(f); _vm = new MainViewModel(_repo); DataContext = _vm; }
        catch (Exception ex) { MessageBox.Show($"初始化失败!\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error); Application.Current.Shutdown(); return; }
        Native.DesktopWindow.PinToDesktop(this); Opacity = 0.85;
        _vm.IsGroupedMode = _settingsSvc.IsGroupedMode; UpdateGroupModeBtn();
        double sl = _settingsSvc.WindowLeft, st = _settingsSvc.WindowTop, sw = _settingsSvc.WindowWidth, sh = _settingsSvc.WindowHeight; var wa = SystemParameters.WorkArea; if (!double.IsNaN(sl) && !double.IsNaN(st) && !double.IsNaN(sw) && !double.IsNaN(sh)) { Left = sl; Top = st; Width = sw; Height = sh; } else { Left = wa.Right - Width - 20; Top = wa.Top + 60; }
        LocationChanged += (_, _) => { if (WindowState == WindowState.Normal && Left > 0) { _lx = Left; _ly = Top; _settingsSvc.WindowLeft = Left; _settingsSvc.WindowTop = Top; } };
        SizeChanged += (_, _) => { if (WindowState == WindowState.Normal && ActualWidth > 100) { _lw = ActualWidth; _lh = ActualHeight; _settingsSvc.WindowWidth = ActualWidth; _settingsSvc.WindowHeight = ActualHeight; } };
        Loaded += async (_, _) => { await _vm.StartupDeleteAndLoadAsync(); };
        Loaded += (_, _) => { Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { var h2 = new WindowInteropHelper(this).Handle; if (h2 != IntPtr.Zero) Native.DesktopWindow.SetWindowPos(h2, new IntPtr(1), 0, 0, 0, 0, 1 | 2 | 0x10 | 0x40); })); };
        Closing += (_, _) => { if (WindowState == WindowState.Normal) { _settingsSvc.WindowLeft = Left; _settingsSvc.WindowTop = Top; _settingsSvc.WindowWidth = ActualWidth; _settingsSvc.WindowHeight = ActualHeight; } _repo.Dispose(); };
        SourceInitialized += (_, _) => { var h = new WindowInteropHelper(this).Handle; if (h != IntPtr.Zero) { int es = Native.DesktopWindow.GetWindowLong(h, -20); Native.DesktopWindow.SetWindowLong(h, -20, es | 0x08000000); } };
        PreviewMouseMove += (s, e) => { if (_drag) { var p = e.GetPosition(this); Left += p.X - _dp.X; Top += p.Y - _dp.Y; if (Left < SystemParameters.VirtualScreenLeft) Left = SystemParameters.VirtualScreenLeft; if (Top < SystemParameters.VirtualScreenTop) Top = SystemParameters.VirtualScreenTop; if (Left + ActualWidth > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - ActualWidth; if (Top + ActualHeight > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight) Top = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - ActualHeight; } };
        PreviewMouseUp += (s, e) => { _drag = false; ReleaseMouseCapture(); };
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (Math.Abs(Left) <= 2 && Math.Abs(Top) <= 2 && Math.Abs(ActualWidth - SystemParameters.WorkArea.Width) < 20)
        { WindowState = WindowState.Normal; if (_lx > 0) { Left = _lx; Top = _ly; Width = _lw > 100 ? _lw : 380; Height = _lh > 100 ? _lh : 600; } }
        _drag = true; _dp = e.GetPosition(this); CaptureMouse();
    }

    private void SendToBottom() { var h = new WindowInteropHelper(this).Handle; if (h != IntPtr.Zero) Native.DesktopWindow.SetWindowPos(h, new IntPtr(1), 0, 0, 0, 0, 1 | 2 | 0x10 | 0x40); }

    private void ChkBtn_Loaded(object sender, RoutedEventArgs e) { var b = (Button)sender; if (b.DataContext is TodoItem it) { b.Content = it.IsCompleted ? "\u2713" : ""; b.Foreground = it.IsCompleted ? new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88)) : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); } }
    private void GroupHeader_Click(object sender, MouseButtonEventArgs e) { if (((Grid)sender).DataContext is TodoGroupViewModel g) g.IsExpanded = !g.IsExpanded; }
    private async void ChkBtn_Click(object sender, RoutedEventArgs e) { var b = (Button)sender; if (b.DataContext is TodoItem it) { it.IsCompleted = !it.IsCompleted; b.Content = it.IsCompleted ? "\u2713" : ""; b.Foreground = it.IsCompleted ? new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88)) : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); await _repo.UpdateAsync(it); await _vm.LoadAsync(); } }
    private void GroupModeBtn_Click(object sender, RoutedEventArgs e) { _vm.IsGroupedMode = !_vm.IsGroupedMode; _settingsSvc.IsGroupedMode = _vm.IsGroupedMode; UpdateGroupModeBtn(); _ = _vm.LoadAsync(); }
    private void UpdateGroupModeBtn() { GroupModeBtn.Content = _vm.IsGroupedMode ? "G" : "g"; GroupModeBtn.Foreground = _vm.IsGroupedMode ? new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88)) : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); GroupModeBtn.ToolTip = _vm.IsGroupedMode ? "分组模式: 开" : "分组模式: 关"; }
    private void EditBtn_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is TodoItem item) { var d = new Views.AddEditDialog(_repo, item, () => _ = _vm.LoadAsync()) { Owner = this, Topmost = true }; d.ShowDialog(); SendToBottom(); } }
    private async void DeleteBtn_Click(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is TodoItem item) { if (RequireConfirm(item) && new Views.ConfirmDialog($"确定要删除[{item.Title}] 吗？", "确认删除") { Owner = this, Topmost = true }.ShowDialog() != true) { SendToBottom(); return; } _vm.AllItems.Remove(item); await _repo.DeleteAsync(item.Id); await _vm.LoadAsync(); SendToBottom(); } }
    private bool RequireConfirm(TodoItem it) => _settingsSvc.DeleteConfirmMode switch { DeleteConfirmMode.Never => false, DeleteConfirmMode.Uncompleted => !it.IsCompleted, _ => true };
    private void AddBtn_Click(object sender, RoutedEventArgs e) { new Views.AddEditDialog(_repo, null, () => _ = _vm.LoadAsync()) { Owner = this, Topmost = true }.ShowDialog(); SendToBottom(); }
    private void GroupBtn_Click(object sender, RoutedEventArgs e) { new Views.GroupDialog(_vm.AllItems.ToList(), _repo, () => _ = _vm.LoadAsync()) { Owner = this, Topmost = true }.ShowDialog(); SendToBottom(); }
    private void SettingsBtn_Click(object sender, RoutedEventArgs e) { new Views.SettingsWindow(_settingsSvc) { Owner = this, Topmost = true }.ShowDialog(); SendToBottom(); }
    private void CloseBtn_Click(object sender, RoutedEventArgs e) { if (!_settingsSvc.IsCloseConfirmEnabled) { Application.Current.Shutdown(); return; } if (new Views.ConfirmDialog("确定要退出 TodoWidget 吗？", "确认退出") { Owner = this, Topmost = true }.ShowDialog() == true) Application.Current.Shutdown(); }
}