using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;
using PleasantUI.Controls.Chrome;
using Regul.Managers;
using Regul.ModuleSystem;
using Regul.ViewModels.Pages;
using TitleBarType = PleasantUI.Controls.Chrome.PleasantTitleBar.Type;

namespace Regul.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = null!;

    public SettingsPage()
    {
        AvaloniaXamlLoader.Load(this);

        this.FindControl<NavigationView>("NavigationView")?.GetObservable(NavigationView.DisplayModeProperty).Subscribe(ChangeDisplayMode);
    }

    public SettingsPage(SettingsPageViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        DetachedFromVisualTree += (_, _) => { ModuleManager.Modules.CollectionChanged -= ViewModel.ModulesOnCollectionChanged; };
    }
    private void ChangeDisplayMode(SplitViewDisplayMode obj)
    {
        if (WindowsManager.MainWindow is not null)
            WindowsManager.MainWindow.TitleBarType = obj == SplitViewDisplayMode.Overlay ? TitleBarType.ClassicExtended : TitleBarType.Classic;
    }
}
