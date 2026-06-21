using System.Text.Json;
using TodoWidget.Models;

namespace TodoWidget.Services;

public class SettingsService
{
    private readonly string _path;
    private AppSettings _cfg;
    public SettingsService(string folder) { _path = System.IO.Path.Combine(folder, "settings.json"); _cfg = Load(); }
    public DeleteConfirmMode DeleteConfirmMode { get => _cfg.DeleteConfirmMode; set { _cfg.DeleteConfirmMode = value; Save(); } }
    public bool IsCloseConfirmEnabled { get => _cfg.IsCloseConfirmEnabled; set { _cfg.IsCloseConfirmEnabled = value; Save(); } }
    public bool IsGroupedMode { get => _cfg.IsGroupedMode; set { _cfg.IsGroupedMode = value; Save(); } }
    public double WindowLeft { get => _cfg.WindowLeft; set { _cfg.WindowLeft = value; Save(); } }
    public double WindowTop { get => _cfg.WindowTop; set { _cfg.WindowTop = value; Save(); } }
    public double WindowWidth { get => _cfg.WindowWidth; set { _cfg.WindowWidth = value; Save(); } }
    public double WindowHeight { get => _cfg.WindowHeight; set { _cfg.WindowHeight = value; Save(); } }
    private AppSettings Load() { try { if (System.IO.File.Exists(_path)) return JsonSerializer.Deserialize<AppSettings>(System.IO.File.ReadAllText(_path)) ?? new(); } catch { } return new(); }
    private void Save() { try { System.IO.File.WriteAllText(_path, JsonSerializer.Serialize(_cfg)); } catch { } }
}

public class AppSettings
{
    public DeleteConfirmMode DeleteConfirmMode { get; set; } = DeleteConfirmMode.Always;
    public bool IsCloseConfirmEnabled { get; set; } = true;
    public bool IsGroupedMode { get; set; } = true;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = double.NaN;
    public double WindowHeight { get; set; } = double.NaN;
}