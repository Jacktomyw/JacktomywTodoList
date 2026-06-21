using System.ComponentModel.DataAnnotations;

namespace TodoWidget.Models;

public class TodoGroup
{
    [Key, MaxLength(50)] public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
