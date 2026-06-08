namespace PwM.Mobile.Services;

public sealed class SensitiveClipboardService : IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _clearCancellation;

    public async Task CopyForAsync(string text, TimeSpan duration)
    {
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
            clearCancellation.Cancel();
            clearCancellation.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _clearCancellation?.Cancel();
            _clearCancellation?.Dispose();
            _clearCancellation = null;
        }
    }
}
