using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TodoWidget.Data;
using TodoWidget.Models;

namespace TodoWidget.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly TodoRepository _repo;
    private System.Timers.Timer _countdownTimer;
    private System.Timers.Timer? _midnightTimer;
    private readonly ConcurrentDictionary<int, System.Timers.Timer> _recurringTimers = new();
    private const double MaxTimerInterval = int.MaxValue;

    public ObservableCollection<TodoItem> AllItems { get; } = new();
    public ObservableCollection<TodoGroupViewModel> Groups { get; } = new();
    public bool IsGroupedMode { get; set; } = true;

    public MainViewModel(TodoRepository repo)
    {
        _repo = repo;
        var now = DateTime.Now;
        var msToNextMinute = (60 - now.Second) * 1000 - now.Millisecond;
        _countdownTimer = new System.Timers.Timer(msToNextMinute) { AutoReset = false };
        _countdownTimer.Elapsed += CountdownFirstTick;
        _countdownTimer.Start();
    }

    private void CountdownFirstTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        RefreshCountdown();
        _countdownTimer.Stop();
        _countdownTimer = new System.Timers.Timer(60_000) { AutoReset = true };
        _countdownTimer.Elapsed += (_, _) => RefreshCountdown();
        _countdownTimer.Start();
    }

    private void ScheduleMidnightTimer()
    {
        _midnightTimer?.Stop(); _midnightTimer?.Dispose();
        var now = DateTime.Now;
        var nextMidnight = now.Date.AddDays(1);
        var msUntilMidnight = (nextMidnight - now).TotalMilliseconds;
        if (msUntilMidnight <= 0) msUntilMidnight = 1000;
        _midnightTimer = new System.Timers.Timer(msUntilMidnight) { AutoReset = false };
        _midnightTimer.Elapsed += MidnightElapsed;
        _midnightTimer.Start();
    }

    private async void MidnightElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await DeleteExpiredAndReloadAsync();
        ScheduleMidnightTimer();
    }

    private async Task DeleteExpiredAndReloadAsync()
    {
        var count = await _repo.DeleteExpiredAsync();
        if (count > 0) await ReloadFromDbAsync();
    }

    public async Task StartupDeleteAndLoadAsync()
    {
        await _repo.AdvancePastDueRecurringAsync();
        await _repo.DeleteExpiredAsync();
        await ReloadFromDbAsync();
        ScheduleMidnightTimer();
    }

    public async Task LoadAsync()
    {
        await ReloadFromDbAsync();
    }

    private async Task ReloadFromDbAsync()
    {
        var items = await _repo.GetAllAsync();
        AllItems.Clear();
        foreach (var item in items) AllItems.Add(item);
        await RebuildGroupsAsync();
        RescheduleAllRecurringTimers();
    }

    private void ScheduleRecurringTimer(TodoItem item)
    {
        if (!item.IsRecurring || !item.DueDate.HasValue) return;
        CancelRecurringTimer(item.Id);
        var msUntil = (item.DueDate.Value - DateTime.Now).TotalMilliseconds;
        if (msUntil <= 0) msUntil = 1000;
        if (msUntil > MaxTimerInterval) msUntil = MaxTimerInterval;
        var timer = new System.Timers.Timer(msUntil) { AutoReset = false };
        var id = item.Id;
        timer.Elapsed += async (_, _) =>
        {
            _recurringTimers.TryRemove(id, out _);
            await _repo.AdvancePastDueRecurringAsync();
            var disp = Application.Current?.Dispatcher;
            if (disp != null) await disp.InvokeAsync(async () => await ReloadFromDbAsync());
            else await ReloadFromDbAsync();
        };
        timer.Start();
        _recurringTimers[id] = timer;
    }

    private void CancelRecurringTimer(int id)
    {
        if (_recurringTimers.TryRemove(id, out var t)) { t.Stop(); t.Dispose(); }
    }

    private void RescheduleAllRecurringTimers()
    {
        foreach (var (_, timer) in _recurringTimers) { timer.Stop(); timer.Dispose(); }
        _recurringTimers.Clear();
        foreach (var item in AllItems.Where(i => i.IsRecurring))
            ScheduleRecurringTimer(item);
    }

    private async Task RebuildGroupsAsync()
    {
        Groups.Clear();
        if (!IsGroupedMode)
        {
            var allGroup = new TodoGroupViewModel { GroupName = "", IsExpanded = true };
            var sorted = AllItems.OrderBy(i => i.IsCompleted).ThenBy(i => i.DueDate ?? DateTime.MaxValue).ToList();
            foreach (var item in sorted) allGroup.Items.Add(item);
            Groups.Add(allGroup);
            return;
        }
        var groupOrder = await _repo.GetGroupsAsync();
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < groupOrder.Count; i++) dict[groupOrder[i].Name] = i;

        var sysGroup = "未分组";
        var names = AllItems.Select(i => i.GroupName).Distinct()
            .OrderBy(n => n == sysGroup ? 1 : 0)
            .ThenBy(n => dict.GetValueOrDefault(n, int.MaxValue));
        foreach (var name in names)
        {
            var sorted = AllItems.Where(i => i.GroupName == name)
                .OrderBy(i => i.IsCompleted).ThenBy(i => i.DueDate ?? DateTime.MaxValue).ToList();
            var gvm = new TodoGroupViewModel { GroupName = name, SortOrder = dict.GetValueOrDefault(name, int.MaxValue) };
            foreach (var item in sorted) gvm.Items.Add(item);
            Groups.Add(gvm);
        }
    }

    public async Task SaveNewItem(TodoItem item) { await _repo.AddAsync(item); AllItems.Add(item); await RebuildGroupsAsync(); }
    public async Task UpdateItemAsync(TodoItem item) { await _repo.UpdateAsync(item); await RebuildGroupsAsync(); }
    public async Task DeleteTodo(TodoItem item) { AllItems.Remove(item); await _repo.DeleteAsync(item.Id); await RebuildGroupsAsync(); }

    private void RefreshCountdown() { var tmp = AllItems.ToList(); AllItems.Clear(); foreach (var i in tmp) AllItems.Add(i); _ = RebuildGroupsAsync(); }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}