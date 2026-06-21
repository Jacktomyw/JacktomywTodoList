using Microsoft.EntityFrameworkCore;
using TodoWidget.Models;

namespace TodoWidget.Data;

public class TodoRepository : IDisposable
{
    private readonly TodoDbContext _db;
    private bool _disposed;
    const string SysGroup = "未分组";

    public TodoRepository(string dbPath)
    {
        _db = new TodoDbContext(dbPath);
        _db.Database.EnsureCreated();
        MigrateDefault().Wait();
    }

    private async Task MigrateDefault()
    {
        var old = await _db.TodoItems.Where(i => i.GroupName == "默认").ToListAsync();
        foreach (var it in old) { it.GroupName = SysGroup; _db.TodoItems.Update(it); }
        if (old.Count > 0) await _db.SaveChangesAsync();
    }

    public async Task<List<TodoItem>> GetAllAsync() => await _db.TodoItems.OrderBy(t => t.GroupName).ThenBy(t => t.DueDate).ToListAsync();
    public async Task<TodoItem> AddAsync(TodoItem item) { _db.TodoItems.Add(item); await _db.SaveChangesAsync(); return item; }
    public async Task UpdateAsync(TodoItem item) { _db.TodoItems.Update(item); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var it = await _db.TodoItems.FindAsync(id); if (it != null) { _db.TodoItems.Remove(it); await _db.SaveChangesAsync(); } }

    public async Task<List<TodoGroup>> GetGroupsAsync()
    {
        var existing = await _db.TodoGroups.OrderBy(g => g.SortOrder).ToListAsync();
        if (existing.Count == 0)
        {
            var names = await _db.TodoItems.Select(i => i.GroupName).Distinct().Where(n => n != SysGroup).ToListAsync();
            for (int i = 0; i != names.Count; i++) _db.TodoGroups.Add(new TodoGroup { Name = names[i], SortOrder = i });
            await _db.SaveChangesAsync();
            return await _db.TodoGroups.OrderBy(g => g.SortOrder).ToListAsync();
        }
        return existing;
    }

    public async Task SaveGroupOrderAsync(List<string> ordered)
    {
        var groups = await _db.TodoGroups.ToListAsync();
        for (int i = 0; i != ordered.Count; i++)
        {
            var g = groups.FirstOrDefault(x => x.Name == ordered[i]);
            if (g != null) g.SortOrder = i;
            else _db.TodoGroups.Add(new TodoGroup { Name = ordered[i], SortOrder = i });
        }
        _db.TodoGroups.RemoveRange(groups.Where(g => !ordered.Contains(g.Name)));
        await _db.SaveChangesAsync();
    }

    public async Task AddGroupAsync(string name) { if (name == SysGroup) return; var mx = await _db.TodoGroups.MaxAsync(g => (int?)g.SortOrder) ?? -1; _db.TodoGroups.Add(new TodoGroup { Name = name, SortOrder = mx + 1 }); await _db.SaveChangesAsync(); }
    public async Task DeleteGroupAsync(string name) { if (name == SysGroup) return; var g = await _db.TodoGroups.FindAsync(name); if (g != null) { _db.TodoGroups.Remove(g); await _db.SaveChangesAsync(); } }

    public async Task RenameGroupAsync(string oldName, string newName, List<TodoItem> allItems)
    {
        if (oldName == SysGroup || newName == SysGroup) return;
        var g = await _db.TodoGroups.FindAsync(oldName);
        int order = g?.SortOrder ?? int.MaxValue;
        if (g != null) _db.TodoGroups.Remove(g);
        _db.TodoGroups.Add(new TodoGroup { Name = newName, SortOrder = order });
        foreach (var it in allItems.Where(i => i.GroupName == oldName)) { it.GroupName = newName; _db.TodoItems.Update(it); }
        await _db.SaveChangesAsync();
    }

    public async Task<int> DeleteExpiredAsync() { var now = DateTime.Now; var expired = await _db.TodoItems.Where(i => i.IsCompleted && i.DueDate != null && i.DueDate.Value.Date.AddDays(7) <= now).ToListAsync(); if (expired.Count > 0) { _db.TodoItems.RemoveRange(expired); await _db.SaveChangesAsync(); } return expired.Count; }

    public void Dispose() { if (!_disposed) { _db.Dispose(); _disposed = true; } }
}