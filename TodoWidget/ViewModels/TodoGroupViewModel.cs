using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TodoWidget.Models;

namespace TodoWidget.ViewModels;

public class TodoGroupViewModel : INotifyPropertyChanged
{
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ObservableCollection<TodoItem> Items { get; } = new();

    private bool _isExpanded = true;
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ExpandIcon)); } }
    public string ExpandIcon => IsExpanded ? "v" : ">";
    public int VisibleCount => Items.Count;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
