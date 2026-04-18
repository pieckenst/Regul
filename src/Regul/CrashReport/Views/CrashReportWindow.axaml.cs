using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace Regul.CrashReport.Views;

public partial class CrashReportWindow : PleasantWindow
{
    public CrashReportWindow() => AvaloniaXamlLoader.Load(this);
}
