using System.Windows;

namespace TodoWidget.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message, string title = "确认", bool showCancel = true)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        if (!showCancel) { CancelBtn.Visibility = Visibility.Collapsed; OkBtn.Margin = new Thickness(0); }
        Loaded += (_, _) => { if (Owner != null) { Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2; Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2; } };
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
}