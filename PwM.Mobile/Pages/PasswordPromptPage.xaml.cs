namespace PwM.Mobile.Pages;

public partial class PasswordPromptPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isClosing;

    public Task<string?> Result => _result.Task;

    public PasswordPromptPage(string title, string message, string placeholder)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        PasswordEntry.Placeholder = placeholder;
    }

    public PasswordPromptPage(string password) : this("Password", "Stored password:", string.Empty)
    {
        PasswordEntry.IsPassword = false;
        PasswordEntry.IsReadOnly = true;
        PasswordEntry.Text = password;
    }

    public Task CancelAsync()
    {
        PasswordEntry.Text = string.Empty;
        _result.TrySetResult(null);
        return CompleteAsync(null);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PasswordEntry.Focus();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(null);
        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!_isClosing)
        {
            PasswordEntry.Text = string.Empty;
            _result.TrySetResult(null);
        }
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordEntry.Text))
        {
            ValidationLabel.IsVisible = true;
            return;
        }

        await CompleteAsync(PasswordEntry.Text);
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await CompleteAsync(null);

    private async Task CompleteAsync(string? password)
    {
        if (_isClosing)
            return;

        _isClosing = true;
        PasswordEntry.Text = string.Empty;

        try
        {
            if (Navigation.ModalStack.LastOrDefault() == this)
                await Navigation.PopModalAsync();
        }
        finally
        {
            _result.TrySetResult(password);
        }
    }
}
