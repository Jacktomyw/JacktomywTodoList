using System.Windows;
using TodoWidget.Data;
using TodoWidget.Models;

namespace TodoWidget.Views;

public partial class AddEditDialog : Window
{
    private TodoRepository _repo;
    private TodoItem? _edit;
    private Func<Task> _onSaved;
    private string? _initialGroup;

    public AddEditDialog(TodoRepository repo, TodoItem? edit, Func<Task> onSaved, string? initialGroup = null)
    {
        _repo = repo; _edit = edit; _onSaved = onSaved; _initialGroup = initialGroup;
        InitializeComponent();
        DatePick.SelectedDate = DateTime.Today.AddDays(1);
        Loaded += async (_, _) =>
        {
            if (Owner != null) { Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2; Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2; }
            var gs = await _repo.GetGroupsAsync();
            foreach (var g in gs.Select(x => x.Name).Where(n => n != "未分组")) GroupCombo.Items.Add(g);
            GroupCombo.Items.Add("未分组");
            if (_edit != null)
            {
                Title = "编辑待办事项"; TitleBox.Text = _edit.Title;
                GroupCombo.SelectedItem = _edit.GroupName;
                if (_edit.IsRecurring) { RecurringCheck.IsChecked = true; CycleDaysBox.Text = _edit.RecurringIntervalDays.ToString(); }
                if (_edit.DueDate.HasValue) { RadioSpecific.IsChecked = true; DatePick.SelectedDate = _edit.DueDate.Value.Date; TimeBox.Text = _edit.DueDate.Value.Hour.ToString("D2"); }
            }
            if (_edit == null && _initialGroup != null) GroupCombo.SelectedItem = _initialGroup;
            GroupCombo.SelectedItem ??= _edit?.GroupName ?? "未分组";
        };
    }

    private void RecurringCheck_Changed(object sender, RoutedEventArgs e)
    {
        var rec = RecurringCheck.IsChecked == true;
        RecurringPanel.Visibility = rec ? Visibility.Visible : Visibility.Collapsed;
        PeriodGroupBox.Header = rec ? "下次刷新时间" : "到期时间";
    }

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text)) { new ConfirmDialog("请输入标题。", "提示", false) { Owner = this, Topmost = true }.ShowDialog(); return; }
        var isRecurring = RecurringCheck.IsChecked == true;
        int.TryParse(CycleDaysBox.Text, out var cycleDays);
        if (isRecurring && cycleDays <= 0) { new ConfirmDialog("请输入有效的周期天数。", "提示", false) { Owner = this, Topmost = true }.ShowDialog(); return; }
        DateTime? due = null;
        if (RadioSpecific.IsChecked == true && DatePick.SelectedDate.HasValue)
        {
            int.TryParse(TimeBox.Text, out var h);
            due = DatePick.SelectedDate.Value.Date.AddHours(h);
        }
        else
        {
            int.TryParse(RemainDaysBox.Text, out var d); int.TryParse(RemainHoursBox.Text, out var h);
            var raw = DateTime.Now.AddDays(d).AddHours(h);
            due = new DateTime(raw.Year, raw.Month, raw.Day, raw.Hour, 0, 0);
            if (raw.Minute > 0) due = due.Value.AddHours(1);
        }
        if (isRecurring && due.HasValue) { var now = DateTime.Now; int safety = 0; while (due.Value <= now && safety++ < 10000) due = due.Value.AddDays(cycleDays); }
        var grp = GroupCombo.SelectedItem?.ToString() ?? "未分组";
        if (_edit != null) { _edit.Title = TitleBox.Text.Trim(); _edit.GroupName = grp; _edit.DueDate = due; _edit.IsRecurring = isRecurring; _edit.RecurringIntervalDays = cycleDays; await _repo.UpdateAsync(_edit); }
        else await _repo.AddAsync(new TodoItem { Title = TitleBox.Text.Trim(), GroupName = grp, DueDate = due, IsRecurring = isRecurring, RecurringIntervalDays = cycleDays, CreatedAt = DateTime.Now });
        await _onSaved();
        DialogResult = true; Close();
    }


    public string FinalGroup => GroupCombo.SelectedItem?.ToString() ?? "未分组";
}