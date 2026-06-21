using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoWidget.Models;

public class TodoItem
{
    [Key] public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    [MaxLength(50)] public string GroupName { get; set; } = "未分组";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRecurring { get; set; }
    public int RecurringIntervalDays { get; set; }

    [NotMapped] public TimeSpan? RemainingTime => DueDate.HasValue ? DueDate.Value - DateTime.Now : null;

    [NotMapped] public string RemainingDisplay
    {
        get
        {
            if (!DueDate.HasValue) return "无截止";
            var remain = RemainingTime!.Value;
            if (remain.TotalSeconds < 0) return $"已逾期 {-(int)remain.TotalDays}天 {-(int)remain.Hours}小时";
            if (remain.TotalDays >= 1) return $"剩余 {(int)remain.TotalDays}天 {(int)remain.Hours}小时";
            if (remain.TotalHours >= 1) return $"剩余 {(int)remain.TotalHours}小时 {remain.Minutes}分";
            return $"剩余 {remain.Minutes}分";
        }
    }

    [NotMapped] public string DueDateLabel => IsRecurring ? "下次刷新" : "到期";
    [NotMapped] public string DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("yyyy-MM-dd HH:mm") : "无";

    [NotMapped] public DateTime? AutoDeleteTime => (IsRecurring || !DueDate.HasValue) ? null : DueDate.Value.Date.AddDays(7);
    [NotMapped] public string AutoDeleteDisplay => AutoDeleteTime.HasValue ? $"自动删除: {AutoDeleteTime.Value:yyyy-MM-dd}" : string.Empty;

    [NotMapped] public string RecurringInfo => IsRecurring && RecurringIntervalDays > 0 ? $"每{RecurringIntervalDays}天刷新" : string.Empty;
}