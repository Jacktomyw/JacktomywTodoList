using Microsoft.EntityFrameworkCore;
using TodoWidget.Models;

namespace TodoWidget.Data;

public class TodoDbContext : DbContext
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoGroup> TodoGroups => Set<TodoGroup>();
    private readonly string _dbPath;
    public TodoDbContext(string dbPath) { _dbPath = dbPath; }
    protected override void OnConfiguring(DbContextOptionsBuilder options) { options.UseSqlite($"Data Source={_dbPath}"); }
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TodoItem>().HasIndex(t => t.IsCompleted);
        model.Entity<TodoItem>().HasIndex(t => t.GroupName);
        model.Entity<TodoItem>().HasIndex(t => t.DueDate);
        model.Entity<TodoGroup>().HasIndex(g => g.SortOrder);
    }
}
