using System.Windows;
using System.Windows.Controls;
using TodoWidget.Models;
using TodoWidget.Native;
using TodoWidget.Services;

namespace TodoWidget.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        UpdateStartupToggle();
        InitDeleteConfirmCombo();
        UpdateCloseConfirmToggle();
        Loaded += (_, _) => { if (Owner != null) { Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2; Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2; } };
    }

    private void StartupToggle_Click(object sender, RoutedEventArgs e) { StartupService.SetAutoStart(StartupToggle.IsChecked == true); UpdateStartupToggle(); }
    private void UpdateStartupToggle() { var e = StartupService.IsAutoStartEnabled(); StartupToggle.IsChecked = e; StartupToggle.Content = e ? "ON" : "OFF"; }

    private void InitDeleteConfirmCombo()
    {
        DeleteConfirmCombo.ItemsSource = new Dictionary<DeleteConfirmMode, string> { { DeleteConfirmMode.Always, "有" }, { DeleteConfirmMode.Uncompleted, "仅未完成" }, { DeleteConfirmMode.Never, "无" } };
        DeleteConfirmCombo.DisplayMemberPath = "Value"; DeleteConfirmCombo.SelectedValuePath = "Key"; DeleteConfirmCombo.SelectedValue = _settings.DeleteConfirmMode;
    }
    private void DeleteConfirm_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (DeleteConfirmCombo.SelectedValue is DeleteConfirmMode m) _settings.DeleteConfirmMode = m; }
    private void CloseConfirmToggle_Click(object sender, RoutedEventArgs e) { _settings.IsCloseConfirmEnabled = CloseConfirmToggle.IsChecked == true; UpdateCloseConfirmToggle(); }
    private void UpdateCloseConfirmToggle() { var e = _settings.IsCloseConfirmEnabled; CloseConfirmToggle.IsChecked = e; CloseConfirmToggle.Content = e ? "ON" : "OFF"; }
}