using System.Windows;
using TodoWidget.Data;
using TodoWidget.Models;

namespace TodoWidget.Views;

public partial class AddEditDialog : Window
{
    private TodoRepository _repo;
    private TodoItem? _edit;
    private Func<Task> _onSaved;

    public AddEditDialog(TodoRepository repo, TodoItem? edit, Func<Task> onSaved)
    {
        _repo = repo; _edit = edit; _onSaved = onSaved;
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
                if (_edit.DueDate.HasValue) { RadioSpecific.IsChecked = true; DatePick.SelectedDate = _edit.DueDate.Value.Date; TimeBox.Text = _edit.DueDate.Value.Hour.ToString("D2"); }
            }
            GroupCombo.SelectedItem ??= _edit?.GroupName ?? "未分组";
        };
    }

    private async void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text)) { new ConfirmDialog("请输入标题。", "提示", false) { Owner = this, Topmost = true }.ShowDialog(); return; }
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
        var grp = GroupCombo.SelectedItem?.ToString() ?? "未分组";
        if (_edit != null) { _edit.Title = TitleBox.Text.Trim(); _edit.GroupName = grp; _edit.DueDate = due; await _repo.UpdateAsync(_edit); }
        else await _repo.AddAsync(new TodoItem { Title = TitleBox.Text.Trim(), GroupName = grp, DueDate = due, CreatedAt = DateTime.Now });
        await _onSaved();
        DialogResult = true; Close();
    }
}