using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace Regul.Views.Windows;

public partial class ToolWindow : ContentDialog
{
    public ToolWindow() => AvaloniaXamlLoader.Load(this);
}
