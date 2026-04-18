using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using PleasantUI;
using PleasantUI.Controls;
using ReactiveUI;
using Regul.Logging;

namespace Regul.CrashReport.ViewModels;

public class CrashReportViewModel : ReactiveObject
{
    public string ExceptionText { get; set; }

    public CrashReportViewModel(string exceptionText) => ExceptionText = exceptionText;

    public async void CopyLogs(PleasantWindow window)
    {
        var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
        if (clipboard is not null)
            await ClipboardExtensions.SetTextAsync(clipboard, ExceptionText);
    }

    public async void SaveLogs(PleasantWindow window)
    {
        IStorageFile? file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new []
            {
                new FilePickerFileType("Log " + App.GetString("Files"))
                {
                    Patterns = new[] { ".log" }
                }
            },
            DefaultExtension = ".log"
        });
        
        if (file is not null)
            Logger.Instance.SaveLogsToFile(file.Path.AbsolutePath);
    }

    public void Close(PleasantWindow window) => window.Close();

    public async void CloseAndRelaunch(PleasantWindow window)
    {
        System.Diagnostics.Debug.WriteLine("[CrashReportViewModel] CloseAndRelaunch called");
        ApplicationSettings.Current.RestartingApp = true;
        System.Diagnostics.Debug.WriteLine($"[CrashReportViewModel] RestartingApp flag set to: {ApplicationSettings.Current.RestartingApp}");
        ApplicationSettings.Save();
        System.Diagnostics.Debug.WriteLine("[CrashReportViewModel] Settings saved");
        
#if DEBUG
        // In debug mode, wait a bit so we can see the console output
        System.Diagnostics.Debug.WriteLine("[CrashReportViewModel] Waiting 1 second before closing (debug mode)");
        await Task.Delay(1000);
#endif
        
        window.Close();
        System.Diagnostics.Debug.WriteLine("[CrashReportViewModel] Window closed");
    }
}
