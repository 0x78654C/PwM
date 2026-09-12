namespace PwM.Mobile.Services;

public sealed class SensitiveClipboardService : IDisposable
{
    private readonly VaultSession _vaultSession;
    private readonly object _sync = new();
    private CancellationTokenSource? _clearCancellation;
    private string? _copiedText;

    public SensitiveClipboardService(VaultSession vaultSession)
    {
        _vaultSession = vaultSession;
        _vaultSession.Locked += OnSessionLocked;
    }

    private async void OnSessionLocked(object? sender, EventArgs e) => await ClearCurrentAsync();

    private async Task ClearCurrentAsync()
    {
        var copiedText = _copiedText;
        _copiedText = null;
        if (copiedText is null) return;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (await Clipboard.Default.GetTextAsync() == copiedText)
                    await Clipboard.Default.SetTextAsync(string.Empty);
            });
        }
        catch (Exception)
        {
            // A platform can deny clipboard access while the app is backgrounded.
            // Keep the scheduled clear as a fallback.
        }
    }

    public async Task CopyForAsync(string text, TimeSpan duration)
    {
        if (!_vaultSession.IsUnlocked) return;
        var sessionVersion = _vaultSession.Version;
        CancellationTokenSource clearCancellation;
        lock (_sync)
        {
            _clearCancellation?.Cancel();
            _clearCancellation?.Dispose();
            _clearCancellation = new CancellationTokenSource();
            clearCancellation = _clearCancellation;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(
                () => Clipboard.Default.SetTextAsync(text));
            _copiedText = text;
            if (!_vaultSession.IsCurrent(sessionVersion))
                await ClearCurrentAsync();
        }
        catch
        {
            CancelClear(clearCancellation);
            throw;
        }

        _ = ClearAfterDelayAsync(text, duration, clearCancellation);
    }

    private async Task ClearAfterDelayAsync(
        string copiedText,
        TimeSpan duration,
        CancellationTokenSource clearCancellation)
    {
        try
        {
            await Task.Delay(duration, clearCancellation.Token);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var currentText = await Clipboard.Default.GetTextAsync();
                if (currentText == copiedText)
                    await Clipboard.Default.SetTextAsync(string.Empty);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // Clipboard access may be unavailable while backgrounded.
        }
        finally
        {
            CancelClear(clearCancellation);
        }
    }

    private void CancelClear(CancellationTokenSource clearCancellation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_clearCancellation, clearCancellation))
                return;

            _clearCancellation = null;
            _copiedText = null;
            clearCancellation.Cancel();
            clearCancellation.Dispose();
        }
    }

    public void Dispose()
    {
        _vaultSession.Locked -= OnSessionLocked;
        _ = ClearCurrentAsync();
        lock (_sync)
        {
            _clearCancellation?.Cancel();
            _clearCancellation?.Dispose();
            _clearCancellation = null;
        }
    }
}
