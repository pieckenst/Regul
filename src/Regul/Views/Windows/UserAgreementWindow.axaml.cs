using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;
using PleasantUI.Core.Interfaces;
using Regul.Managers;

namespace Regul.Views.Windows;

public partial class UserAgreementWindow : ContentDialog
{
    public UserAgreementWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static async Task<bool> Show()
    {
        if (WindowsManager.MainWindow is null) return false;
        
        UserAgreementWindow window = new UserAgreementWindow();

        window.FindControl<Button>("YesButton").Click += (_, _) =>
        {
            window.CloseAsync(true);
        };
        window.FindControl<Button>("NoButton").Click += (_, _) =>
        {
            window.CloseAsync(false);
        };

        return await window.ShowAsync<bool>((IPleasantWindow)WindowsManager.MainWindow);
    }
}
