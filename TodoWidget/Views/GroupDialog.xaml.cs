using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TodoWidget.Data;
using TodoWidget.Models;

namespace TodoWidget.Views;

public partial class GroupDialog : Window
{
    private readonly List<TodoItem> _allItems;
    private readonly TodoRepository _repo;
    private readonly Action _onChanged;
    private List<string> _groupNames = new();

    public GroupDialog(List<TodoItem> allItems, TodoRepository repo, Action onChanged)
    {
        InitializeComponent();
        _allItems = allItems; _repo = repo; _onChanged = onChanged;
        Loaded += async (_, _) =>
        {
            if (Owner != null) { Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2; Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2; }
            await RefreshGroups();
        };
    }

    private async Task RefreshGroups() { var gs = await _repo.GetGroupsAsync(); _groupNames = gs.Select(g => g.Name).Where(n => n != "未分组").ToList(); RefreshList(); }
    private void RefreshList() { GroupListBox.ItemsSource = null; GroupListBox.ItemsSource = _groupNames.ToList(); }
    private void UpdateBtns() { var i = GroupListBox.SelectedIndex; UpBtn.IsEnabled = i > 0; DownBtn.IsEnabled = i >= 0 && i < _groupNames.Count - 1; RenameBtn.IsEnabled = i >= 0; }
    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateBtns(); }
    private void GroupList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (GroupListBox.SelectedItem is string n) StartRename(n); }

    private async void UpBtn_Click(object sender, RoutedEventArgs e) { var i = GroupListBox.SelectedIndex; if (i <= 0) return; (_groupNames[i], _groupNames[i - 1]) = (_groupNames[i - 1], _groupNames[i]); RefreshList(); GroupListBox.SelectedIndex = i - 1; UpdateBtns(); await _repo.SaveGroupOrderAsync(_groupNames); _onChanged(); }
    private async void DownBtn_Click(object sender, RoutedEventArgs e) { var i = GroupListBox.SelectedIndex; if (i < 0 || i >= _groupNames.Count - 1) return; (_groupNames[i], _groupNames[i + 1]) = (_groupNames[i + 1], _groupNames[i]); RefreshList(); GroupListBox.SelectedIndex = i + 1; UpdateBtns(); await _repo.SaveGroupOrderAsync(_groupNames); _onChanged(); }
    private void RenameBtn_Click(object sender, RoutedEventArgs e) { if (GroupListBox.SelectedItem is string n) StartRename(n); }

    private void StartRename(string oldName)
    {
        var input = new TextBox { Text = oldName, Width = 200 };
        var dlg = new Window { Title = "重命名分组", Width = 300, Height = 170, WindowStartupLocation = WindowStartupLocation.Manual, Owner = this, ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow, Content = new StackPanel { Margin = new Thickness(20) } };
        dlg.Loaded += (_, _) => { if (Owner != null) { dlg.Left = Owner.Left + (Owner.ActualWidth - dlg.ActualWidth) / 2; dlg.Top = Owner.Top + (Owner.ActualHeight - dlg.ActualHeight) / 2; } };
        var sp = (StackPanel)dlg.Content; sp.Children.Add(new TextBlock { Text = $"将\"{oldName}\" 改名为", Margin = new Thickness(0, 0, 0, 8) }); sp.Children.Add(input);
        var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var ok = new Button { Content = "确定", Width = 70, Height = 28, IsDefault = true };
        ok.Click += async (_, _) => { var n = input.Text.Trim(); if (string.IsNullOrWhiteSpace(n) || n == oldName) { dlg.Close(); return; } if (_groupNames.Contains(n)) { new ConfirmDialog("已存在同名分组", "提示", false) { Owner = dlg, Topmost = true }.ShowDialog(); return; } await _repo.RenameGroupAsync(oldName, n, _allItems); _groupNames[_groupNames.IndexOf(oldName)] = n; RefreshList(); UpdateBtns(); _onChanged(); dlg.Close(); };
        bp.Children.Add(ok); bp.Children.Add(new Button { Content = "取消", Width = 70, Height = 28, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) }); sp.Children.Add(bp);
        dlg.ShowDialog();
    }

    private async void CreateGroup_Click(object sender, RoutedEventArgs e) { var n = NewGroupBox.Text.Trim(); if (string.IsNullOrWhiteSpace(n) || n == "未分组" || _groupNames.Contains(n)) return; _groupNames.Add(n); await _repo.AddGroupAsync(n); NewGroupBox.Text = ""; RefreshList(); _onChanged(); }

    private async void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        var n = ((Button)sender).DataContext?.ToString(); if (n == null) return;
        var its = _allItems.Where(i => i.GroupName == n).ToList();
        if (its.Count > 0)
        {
            var dlg = new Window { Title = "确认删除分组", Width = 380, Height = 320, WindowStartupLocation = WindowStartupLocation.Manual, Owner = this, ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow };
            dlg.Loaded += (_, _) => { if (Owner != null) { dlg.Left = Owner.Left + (Owner.ActualWidth - dlg.ActualWidth) / 2; dlg.Top = Owner.Top + (Owner.ActualHeight - dlg.ActualHeight) / 2; } };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = $"删除分组 \"{n}\"？以下 {its.Count} 个事项将被一并删除:", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10), FontSize = 13 });
            var lb = new ListBox { MaxHeight = 160, Margin = new Thickness(0, 0, 0, 12), ItemsSource = its, DisplayMemberPath = "Title" };
            sp.Children.Add(lb);
            var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "确认删除", Width = 80, Height = 28, IsDefault = true, Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x66, 0x66)) };
            bool confirmed = false;
            ok.Click += (_, _) => { confirmed = true; dlg.Close(); };
            bp.Children.Add(ok);
            bp.Children.Add(new Button { Content = "取消", Width = 80, Height = 28, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) });
            sp.Children.Add(bp);
            dlg.Content = sp; dlg.ShowDialog();
            if (!confirmed) return;
        }
        else if (new ConfirmDialog($"删除空分组 \"{n}\"？", "确认删除") { Owner = this, Topmost = true }.ShowDialog() != true) return;
        foreach (var it in its) { _allItems.Remove(it); await _repo.DeleteAsync(it.Id); }
        _groupNames.Remove(n); await _repo.DeleteGroupAsync(n); RefreshList(); _onChanged();
    }
}