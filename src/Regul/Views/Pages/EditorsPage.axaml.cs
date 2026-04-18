using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;
using PleasantUI.Controls.Chrome;
using PleasantUI.Core.Interfaces;
using Regul.Controls;
using Regul.Managers;
using TitleBarType = PleasantUI.Controls.Chrome.PleasantTitleBar.Type;

namespace Regul.Views.Pages;

public partial class EditorsPage : UserControl
{
    private readonly Button? _globalMenu;
    private readonly EditorsTabView? _editorsTabView;

    public EditorsPage()
    {
        AvaloniaXamlLoader.Load(this);

        Panel? dragPanel = this.FindControl<Panel>("DragPanel");
        _globalMenu = this.FindControl<Button>("GlobalMenu");
        _editorsTabView = this.FindControl<EditorsTabView>("TabView");

        if (WindowsManager.MainWindow is { EnableCustomTitleBar: true } && dragPanel is not null)
            PleasantTitleBar.SetIsTitleBarHitTestVisible(WindowsManager.MainWindow, false);

        if (WindowsManager.MainWindow is { EnableCustomTitleBar: true } && WindowsManager.MainWindow.TitleBarType != TitleBarType.Compact)
        {
            if (_editorsTabView != null)
                _editorsTabView.MarginType = PleasantTabView.ViewMarginType.Extended;
        }
        else
        {
            if (_editorsTabView != null)
                _editorsTabView.MarginType = PleasantTabView.ViewMarginType.Little;
        }

        TemplateApplied += (_, _) =>
        {
            if (WindowsManager.MainWindow is null) return;

            if (WindowsManager.MainWindow.ViewModel.Workbenches.Count == 0)
                WindowsManager.MainWindow.ChangePage(typeof(HomePage));
        };
    }

    // ReSharper disable once UnusedParameter.Local
    private void MenuButtonsOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            button.Command?.Execute(button.CommandParameter);

        _globalMenu?.Flyout?.Hide();
        _editorsTabView?.AdderButton?.Flyout?.Hide();
    }
}
