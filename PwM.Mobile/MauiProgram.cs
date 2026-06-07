using Microsoft.Extensions.Logging;
using PwM.Mobile.Services;
using PwM.Mobile.ViewModels;
using PwM.Mobile.Pages;

namespace PwM.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Point PwMLib storage to the platform app-data directory
        PwMLib.GlobalVariables.passwordManagerDirectory =
            Path.Combine(FileSystem.AppDataDirectory, "PwM");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<VaultService>();
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<HibpService>();

        // ViewModels
        builder.Services.AddTransient<VaultListViewModel>();
        builder.Services.AddTransient<VaultViewModel>();
        builder.Services.AddTransient<AddCredentialViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<VaultListPage>();
        builder.Services.AddTransient<VaultPage>();
        builder.Services.AddTransient<AddCredentialPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
