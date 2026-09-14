using System.Windows;
using PwM.Updating;

namespace PwM;

public partial class MainWindow
{
    private UpdateController _updates;

    // Production startup owns the automatic check; constructing a window in tests does not contact GitHub.
    internal void InitializeUpdates()
    {
        _updates = new UpdateController(this);
        _updates.Initialize();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (_updates == null) return;
        checkForUpdatesButton.IsEnabled = false;
        try { await _updates.CheckForUpdatesAsync(true); }
        finally { checkForUpdatesButton.IsEnabled = true; }
    }
}
