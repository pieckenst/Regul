using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PleasantUI;
using PleasantUI.Controls;
using PleasantUI.Controls.Chrome;
using PleasantUI.Core.Models;
using PleasantUI.Core.Structures;
using PleasantUI.ToolKit;
using ReactiveUI;
using Regul.Enums;
using Regul.Helpers;
using Regul.Logging;
using Regul.Managers;
using Regul.ModuleSystem;
using Regul.ModuleSystem.Structures;
using Regul.Other;
using Regul.Structures;
using Regul.Views.Pages;
using TitleBarType = PleasantUI.Controls.Chrome.PleasantTitleBar.Type;
using Language = Regul.Structures.Language;
using PleasantUI.Core;

namespace Regul.ViewModels.Pages;

public class SettingsPageViewModel : ReactiveObject
{
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

    private Module? _selectedModule;

    private CustomTheme? _selectedTheme;
    private bool _inRenameProcess;
    private string _renameText = string.Empty;

    private bool _isThemesChanged;
    private bool _isCheckUpdateModules;
    private bool _isCheckUpdateProgram;

    private string _moduleNameSearching = string.Empty;
    private string _editorRelatedExtension = string.Empty;
    private string _extensionSearching = string.Empty;
    private bool _invertModuleList;
    private bool _invertEditorRelatedExtensionList;

    private readonly TextBox? _renameTextBox = null!;

    private object? PreviousContent { get; }

    private TitleBarType PreviousTitleBarType { get; }

    public string DotNetInformation { get; } = $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}";

    public AvaloniaList<CustomTheme> Themes { get; } = new();
    public AvaloniaList<FontFamily> Fonts { get; } = new();
    public AvaloniaList<Module> SortedModules { get; } = new();
    public AvaloniaList<EditorRelatedExtension> SortedEditorRelatedExtensions { get; } = new();

    public Module? SelectedModule
    {
        get => _selectedModule;
        set => this.RaiseAndSetIfChanged(ref _selectedModule, value);
    }

    public bool InRenameProcess
    {
        get => _inRenameProcess;
        set => this.RaiseAndSetIfChanged(ref _inRenameProcess, value);
    }
    public string RenameText
    {
        get => _renameText;
        set => this.RaiseAndSetIfChanged(ref _renameText, value);
    }

    public string DecryptedVirusTotalApiKey
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ApplicationSettings.Current.VirusTotalApiKey))
                return AesEncryption.DecryptString(ApplicationSettings.Current.Key, ApplicationSettings.Current.VirusTotalApiKey);
            return string.Empty;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                ApplicationSettings.Current.VirusTotalApiKey = string.Empty;
            else
                ApplicationSettings.Current.VirusTotalApiKey = AesEncryption.EncryptString(ApplicationSettings.Current.Key, value);

            if (string.IsNullOrWhiteSpace(ApplicationSettings.Current.VirusTotalApiKey) || DecryptedVirusTotalApiKey.Length < 64 && ScanForVirus)
            {
                ScanForVirus = false;
                this.RaisePropertyChanged(nameof(ScanForVirus));
            }
        }
    }

    public bool ScanForVirus
    {
        get => ApplicationSettings.Current.ScanForVirus;
        set
        {
            if (value && string.IsNullOrWhiteSpace(ApplicationSettings.Current.VirusTotalApiKey))
            {
                WindowsManager.MainWindow?.ShowNotification("YouNeedToEnterVirusTotalApiKey", NotificationType.Error, TimeSpan.FromSeconds(3));
                this.RaisePropertyChanged();
                return;
            }

            if (value && DecryptedVirusTotalApiKey.Length < 64)
            {
                WindowsManager.MainWindow?.ShowNotification("ApiKeyIsTooShort", NotificationType.Error, TimeSpan.FromSeconds(3));
                this.RaisePropertyChanged();
                return;
            }

            ApplicationSettings.Current.ScanForVirus = value;
        }
    }

    public string ModuleNameSearching
    {
        get => _moduleNameSearching;
        set => this.RaiseAndSetIfChanged(ref _moduleNameSearching, value);
    }
    public string EditorRelatedExtensionSearching
    {
        get => _editorRelatedExtension;
        set => this.RaiseAndSetIfChanged(ref _editorRelatedExtension, value);
    }
    public string ExtensionSearching
    {
        get => _extensionSearching;
        set => this.RaiseAndSetIfChanged(ref _extensionSearching, value);
    }
    public bool InvertModuleList
    {
        get => _invertModuleList;
        set => this.RaiseAndSetIfChanged(ref _invertModuleList, value);
    }
    public bool InvertEditorRelatedExtensionList
    {
        get => _invertEditorRelatedExtensionList;
        set => this.RaiseAndSetIfChanged(ref _invertEditorRelatedExtensionList, value);
    }

    public bool IsSupportedOperatingSystem
    {
        get
        {
#if Windows
            return Environment.OSVersion.Version > new Version(10, 0, 10586);
#else
            return false;
#endif
        }
    }

    public bool IsWindows
    {
        get
        {
#if !Windows
            return false;
#else
            return true;
#endif
        }
    }

    public CustomTheme? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTheme, value);
            
            PleasantTheme.SelectedCustomTheme = value;
            PleasantSettings.Current!.Theme = value is not null ? "Custom" : "System";
        }
    }

    public FontFamily SelectedFont
    {
        get => FontFamily.Default;
        set { /* font selection not supported in this PleasantUI version */ }
    }

    public int SelectedIndexBlurType
    {
        get => PleasantSettings.Current?.WindowSettings.EnableBlur == true ? 1 : 0;
        set
        {
            if (PleasantSettings.Current is not null)
                PleasantSettings.Current.WindowSettings.EnableBlur = value == 1;
        }
    }

    public int SelectedIndexCheckUpdateInterval
    {
        get
        {
            return ApplicationSettings.Current.CheckUpdateInterval switch
            {
                CheckUpdateInterval.EveryDay => 0,
                CheckUpdateInterval.EveryWeek => 1,
                CheckUpdateInterval.EveryMonth => 2,
                CheckUpdateInterval.EveryYear => 3,
                _ => 4
            };
        }
        set
        {
            ApplicationSettings.Current.CheckUpdateInterval = value switch
            {
                0 => CheckUpdateInterval.EveryDay,
                1 => CheckUpdateInterval.EveryWeek,
                2 => CheckUpdateInterval.EveryMonth,
                3 => CheckUpdateInterval.EveryYear,
                _ => CheckUpdateInterval.Never
            };
        }
    }

    public int SelectedIndexMode
    {
        get
        {
            return PleasantSettings.Current!.Theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                "Mint" => 3,
                "Strawberry" => 4,
                "Ice" => 5,
                "Sunny" => 6,
                "Spruce" => 7,
                "Cherry" => 8,
                "Cave" => 9,
                "Lunar" => 10,
                "Custom" => 11,
                _ => 0
            };
        }
        set
        {
            PleasantSettings.Current!.Theme = value switch
            {
                1 => "Light",
                2 => "Dark",
                3 => "Mint",
                4 => "Strawberry",
                5 => "Ice",
                6 => "Sunny",
                7 => "Spruce",
                8 => "Cherry",
                9 => "Cave",
                10 => "Lunar",
                11 => "Custom",
                _ => "System"
            };
        }
    }

    public Language SelectedLanguage
    {
        get => App.Languages.First(l => l.Key == ApplicationSettings.Current.Language);
        set
        {
            App.ChangeLanguage(value.Key);

            foreach (Window modalWindow in WindowsManager.Windows)
            {
                if (modalWindow.Content is not null)
                    modalWindow.Content = Activator.CreateInstance(modalWindow.Content.GetType());
            }

            if (WindowsManager.MainWindow is null) return;
            
            SettingsPageViewModel viewModel = new(PreviousContent, PreviousTitleBarType);
            SettingsPage settingsPage = new(viewModel);

            WindowsManager.MainWindow.ChangePage(settingsPage, TitleBarType.Classic);
        }
    }

    public bool IsCheckUpdateModules
    {
        get => _isCheckUpdateModules;
        private set => this.RaiseAndSetIfChanged(ref _isCheckUpdateModules, value);
    }

    public bool IsCheckUpdateProgram
    {
        get => _isCheckUpdateProgram;
        set => this.RaiseAndSetIfChanged(ref _isCheckUpdateProgram, value);
    }

    public bool HasUpdateInModules
    {
        get => ModuleManager.Modules.Any(module => module.HasUpdate);
    }

    public SettingsPageViewModel(object? previousContent, TitleBarType titleBarType)
    {
        PreviousContent = previousContent;
        PreviousTitleBarType = titleBarType;

        foreach (CustomTheme theme in PleasantTheme.CustomThemes)
            Themes.Add(theme);

        SelectedTheme = PleasantTheme.SelectedCustomTheme;

        foreach (FontFamily font in FontManager.Current.SystemFonts)
            Fonts.Add(font);
        
        ModuleManager.Modules.CollectionChanged += ModulesOnCollectionChanged;
        ApplicationSettings.Current.EditorRelatedExtensions.CollectionChanged += EditorRelatedExtensionsOnCollectionChanged;

        this.WhenAnyValue(x => x.ModuleNameSearching, x => x.InvertModuleList)
            .Subscribe(_ => OnSearchModules(ModuleManager.Modules));
        this.WhenAnyValue(x => x.EditorRelatedExtensionSearching, x => x.ExtensionSearching)
            .Subscribe(_ => OnSearchEditorRelatedExtensions(ApplicationSettings.Current.EditorRelatedExtensions));
        this.WhenAnyValue(x => x.InvertEditorRelatedExtensionList)
            .Subscribe(_ => OnSearchEditorRelatedExtensions(ApplicationSettings.Current.EditorRelatedExtensions));
    }
    private void EditorRelatedExtensionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnSearchEditorRelatedExtensions(ApplicationSettings.Current.EditorRelatedExtensions);
    internal void ModulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnSearchModules(ModuleManager.Modules);

    private void OnSearchModules(IEnumerable<Module> modules)
    {
        SortedModules.Clear();

        List<Module> list = new(modules);

        if (!string.IsNullOrWhiteSpace(ModuleNameSearching))
            list = list.FindAll(x => App.GetString(x.Instance.Name).ToLower().Contains(ModuleNameSearching));

        list = new List<Module>(list.OrderBy(x => x.Instance.Name));

        if (InvertModuleList)
            list.Reverse();

        SortedModules.AddRange(list);
    }

    private void OnSearchEditorRelatedExtensions(IEnumerable<EditorRelatedExtension> extensions)
    {
        SortedEditorRelatedExtensions.Clear();

        List<EditorRelatedExtension> list = new(extensions);

        if (!string.IsNullOrWhiteSpace(EditorRelatedExtensionSearching))
            list = list.FindAll(x =>
            {
                string? nameEditor = ModuleManager.GetEditorById(x.IdEditor)?.Name;
                return nameEditor is not null && App.GetString(nameEditor).ToLower().Contains(EditorRelatedExtensionSearching);
            });
        if (!string.IsNullOrWhiteSpace(ExtensionSearching))
            list = list.FindAll(x => x.Extension.ToLower().Contains(ExtensionSearching));
        
        list = new List<EditorRelatedExtension>(list.OrderBy(x => x.Extension));

        if (InvertEditorRelatedExtensionList)
            list.Reverse();

        SortedEditorRelatedExtensions.AddRange(list);
    }

    public void Release()
    {
        if (!_isThemesChanged) return;
        App.PleasantTheme.UpdateCustomThemes();
    }

    public async void ResetSettings()
    {
        if (WindowsManager.MainWindow is null) return;

        string result = await MessageBox.Show(WindowsManager.MainWindow, "ResetSettingsWarning", string.Empty, MessageBoxButtons.YesNo);

        if (result != "Yes") return;

        SelectedTheme = null;

        ApplicationSettings.Reset();

        App.ChangeLanguage(ApplicationSettings.Current.Language);

        this.RaisePropertyChanged(nameof(SelectedLanguage));
        this.RaisePropertyChanged(nameof(SelectedFont));
        this.RaisePropertyChanged(nameof(SelectedIndexMode));

        WindowsManager.MainWindow.ShowNotification("SettingsHaveBeenReset", NotificationType.Success, TimeSpan.FromSeconds(3));
    }

    public void BackToPreviousContent() => WindowsManager.MainWindow?.ChangePage(PreviousContent?.GetType(), PreviousTitleBarType);

    public async void ChangeAccentColor()
    {
        if (WindowsManager.MainWindow is null) return;

        Color? newColor = await ColorPickerWindow.SelectColor(WindowsManager.MainWindow, PleasantSettings.Current!.NumericalAccentColor);

        if (newColor is { } color)
            PleasantSettings.Current!.NumericalAccentColor = color.ToUInt32();
    }

    public async void CopyAccentColor()
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        if (clipboard is not null)
            await ClipboardExtensions.SetTextAsync(clipboard, $"#{PleasantSettings.Current!.NumericalAccentColor.ToString("x8").ToUpper()}");

        WindowsManager.MainWindow?.ShowNotification("ColorCopied", timeSpan: TimeSpan.FromSeconds(2));
    }

    public async void PasteAccentColor()
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        string? data = clipboard is not null ? await ClipboardExtensions.TryGetTextAsync(clipboard) : null;

        if (uint.TryParse(data, out uint uintColor))
            PleasantSettings.Current!.NumericalAccentColor = uintColor;
        else if (Color.TryParse(data, out Color color))
            PleasantSettings.Current!.NumericalAccentColor = color.ToUInt32();
    }

    public async void CopyColor(string key, uint value)
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        if (clipboard is not null)
            await ClipboardExtensions.SetTextAsync(clipboard, $"#{value.ToString("x8").ToUpper()}");

        WindowsManager.MainWindow?.ShowNotification("ColorCopied", timeSpan: TimeSpan.FromSeconds(2));
    }

    public async void PasteColor(string key)
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        string? data = clipboard is not null ? await ClipboardExtensions.TryGetTextAsync(clipboard) : null;

        uint newColor;

        if (uint.TryParse(data, out uint uintColor))
            newColor = uintColor;
        else if (Color.TryParse(data, out Color color))
            newColor = color.ToUInt32();
        else return;

        if (SelectedTheme is CustomTheme customTheme && customTheme.Colors.ContainsKey(key))
        {
            customTheme.Colors[key] = Color.FromUInt32(newColor);
            _isThemesChanged = true;
            App.PleasantTheme.UpdateCustomThemes();
        }
    }

    public async void ChangeColor(string key, uint currentValue)
    {
        if (WindowsManager.MainWindow is null) return;

        Color? newColor = await ColorPickerWindow.SelectColor(WindowsManager.MainWindow, currentValue);

        if (newColor is not null && SelectedTheme is CustomTheme customTheme && customTheme.Colors.ContainsKey(key))
        {
            customTheme.Colors[key] = (Color)newColor;
            _isThemesChanged = true;
            App.PleasantTheme.UpdateCustomThemes();
        }
    }

    private string CheckAndGetThemeName(string name)
    {
        bool isCheckedOriginalName = false;
        int index = 0;

        while (true)
        {
            if (!isCheckedOriginalName && Themes.Any(t => t.Name == name))
            {
                index++;
                isCheckedOriginalName = true;
            }
            else if (Themes.Any(t => t.Name == $"{name} {index}"))
                index++;
            else break;
        }

        return index == 0 ? name : $"{name} {index}";
    }

    public void CreateTheme()
    {
        var colors = PleasantTheme.GetThemeTemplateDictionary();
        var custom = new CustomTheme(null, CheckAndGetThemeName("New Theme"), colors);

        PleasantTheme.CustomThemes.Add(custom);
        Themes.Add(custom);
        SelectedTheme = custom;
        _isThemesChanged = true;
    }

    public void DeleteTheme()
    {
        InRenameProcess = false;
        if (SelectedTheme is CustomTheme customTheme)
        {
            PleasantTheme.CustomThemes.Remove(customTheme);
            Themes.Remove(customTheme);
        }
        _isThemesChanged = true;
    }

    public async void CopyTheme()
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        if (SelectedTheme is CustomTheme customTheme && clipboard is not null)
            await ClipboardExtensions.SetTextAsync(clipboard, System.Text.Json.JsonSerializer.Serialize(customTheme));

        WindowsManager.MainWindow?.ShowNotification("ThemeCopied", timeSpan: TimeSpan.FromSeconds(2));
    }

    public async void PasteTheme(bool withoutName = false)
    {
        var clipboard = TopLevel.GetTopLevel(WindowsManager.MainWindow)?.Clipboard;
        CustomTheme? theme;

        try
        {
            string? json = clipboard is not null ? await ClipboardExtensions.TryGetTextAsync(clipboard) : null;
            theme = System.Text.Json.JsonSerializer.Deserialize<CustomTheme>(json ?? string.Empty);
            if (theme is null) return;
        }
        catch
        {
            return;
        }

        if (SelectedTheme is CustomTheme current)
        {
            if (!withoutName)
                current.Name = CheckAndGetThemeName(theme.Name);

            App.PleasantTheme.EditCustomTheme(current, theme);
            _isThemesChanged = true;
        }

        WindowsManager.MainWindow?.ShowNotification("ThemeAppliedFromClipboard", timeSpan: TimeSpan.FromSeconds(2));
    }

    public void ApplyRenameTheme()
    {
        if (string.IsNullOrWhiteSpace(RenameText))
        {
            CancelRenameTheme();
            return;
        }

        if (SelectedTheme is CustomTheme customTheme)
        {
            customTheme.Name = CheckAndGetThemeName(RenameText);
            PleasantSettings.Current!.Theme = "Custom";
        }
        InRenameProcess = false;
        _isThemesChanged = true;
    }

    public void CancelRenameTheme() => InRenameProcess = false;

    public void RenameTheme()
    {
        RenameText = SelectedTheme!.Name;

        _renameTextBox?.Focus();
        _renameTextBox?.SelectAll();

        InRenameProcess = true;
    }

    public void DeleteEditorRelatedExtension(EditorRelatedExtension editorRelatedExtension)
    {
        ApplicationSettings.Current.EditorRelatedExtensions.Remove(editorRelatedExtension);
    }
    
    public void GetApiKey() => IoHelpers.OpenBrowserAsync("https://www.virustotal.com/gui/my-apikey");

    public void OpenPatreon() => IoHelpers.OpenBrowserAsync("https://www.patreon.com/onebeld");

    public void OpenGitHub() => IoHelpers.OpenBrowserAsync("https://github.com/Onebeld/Regul");

    public void OpenGitHubModulesMd() => IoHelpers.OpenBrowserAsync("https://github.com/Onebeld/Regul/blob/main/modules.md");

    public void WriteEmail()
    {
        const string mailto = "mailto:onebeld@gmail.com";
        Process.Start(new ProcessStartInfo
        {
            FileName = mailto,
            UseShellExecute = true,
        });
    }

    public void OpenDiscord() => IoHelpers.OpenBrowserAsync("https://discordapp.com/users/546992251562098690");

    public void OpenSocialNetwork()
    {
        string language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        if (language == "ru")
            IoHelpers.OpenBrowserAsync("https://vk.com/onebeld");
        else
        {
            if (WindowsManager.MainWindow is null) return;
            
            MessageBox.Show(WindowsManager.MainWindow, "FeatureIsNotSupported", string.Empty, MessageBoxButtons.Ok);
        }
    }

    public async void ReloadModules()
    {
        bool b = await App.UnloadModules();
        if (!b) return;

        await Task.Delay(200);

        App.LoadModules(Directory.EnumerateFiles(RegulDirectories.Modules, "*.dll", SearchOption.AllDirectories));
        
        WindowsManager.MainWindow?.ShowNotification("ModulesHaveBeenReloaded");
    }

    public async void CheckUpdate()
    {
        if (WindowsManager.MainWindow is null) return;

        IsCheckUpdateProgram = true;
        (CheckUpdateResult checkUpdateResult, Version? newVersion) = await App.CheckUpdate();

        if (checkUpdateResult == CheckUpdateResult.HasUpdate)
        {
            IsCheckUpdateProgram = false;
            string result = await MessageBox.Show(
                WindowsManager.MainWindow, 
                $"{App.GetString("UpgradeProgramIsAvailable")}: {newVersion?.ToString()}", 
                "GoToTheWebsiteToDownloadNewUpdate",
                MessageBoxButtons.YesNo);
            
            if (result == "Yes")
                IoHelpers.OpenBrowserAsync("https://github.com/Onebeld/Regul/releases");
        }
        else if (checkUpdateResult == CheckUpdateResult.NoUpdate)
            WindowsManager.MainWindow.ShowNotification("NoUpdatesAtThisTime", NotificationType.Information, TimeSpan.FromSeconds(4));
        else
            WindowsManager.MainWindow.ShowNotification("FailedToCheckForUpdates", NotificationType.Error, TimeSpan.FromSeconds(4));
        IsCheckUpdateProgram = false;
    }

    #region Modules
    
    public async void InstallModule()
    {
        IReadOnlyList<IStorageFile> files = await WindowsManager.MainWindow?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = new []
            {
                new FilePickerFileType($"ZIP {App.GetString("FilesS")}")
                {
                    Patterns = new [] { "*.zip" }
                },
                new FilePickerFileType($"DLL {App.GetString("FilesS")}")
                {
                    Patterns = new [] { "*.dll" }
                }
            },
            AllowMultiple = true
        })!;

        App.InstallModules(files);
    }

    public async void CheckUpdateModules()
    {
        IsCheckUpdateModules = true;

        await Task.Run(() =>
        {
            Parallel.ForEach(ModuleManager.Modules, async module =>
            {
                try
                {
                    Version? updateVersion = await module.Instance.GetNewVersion(out string? link, out Version? requiredRegulVersion);

                    if (updateVersion <= module.Instance.Version)
                        return;

                    module.RegulVersionRequiered = requiredRegulVersion;
                    module.LinkToUpdate = link;
                    module.NewVersion = updateVersion;
                    module.HasUpdate = true;
                }
                catch
                {
                    // ignored
                }
            });
        });

        IsCheckUpdateModules = false;

        this.RaisePropertyChanged(nameof(HasUpdateInModules));

        if (ModuleManager.Modules.Any(x => x.HasUpdate))
            WindowsManager.MainWindow?.ShowNotification("UpdatesAreAvailableForModules", NotificationType.Success, TimeSpan.FromSeconds(4));
        else
            WindowsManager.MainWindow?.ShowNotification("ModulesHaveNoUpdate", timeSpan: TimeSpan.FromSeconds(4));
    }

    public async void BeginUpdatingModule(Module module)
    {
        if (WindowsManager.MainWindow is null) return;
        
        WindowsManager.MainWindow.RunLoading(100);

        await UpdateModule(module);

        WindowsManager.MainWindow.CloseLoading();
        WindowsManager.MainWindow.ShowNotification("NeedToRestartToFinishUpdatingModule", timeSpan: TimeSpan.FromSeconds(5));
    }

    public async void BeginUpdatingModules()
    {
        if (WindowsManager.MainWindow is null) return;

        WindowsManager.MainWindow.RunLoading(100);

        foreach (Module module in ModuleManager.Modules)
        {
            if (module.HasUpdate && !module.ReadyUpgrade)
                await UpdateModule(module);
        }
        
        WindowsManager.MainWindow.ChangeLoadingProgress(0, "PreparingM", true);
        
        bool b = await App.UnloadModules();
        if (!b)
        {
            WindowsManager.MainWindow.CloseLoading();
            return;
        }
        
        App.UpdateModules();
        App.LoadModules(Directory.EnumerateFiles(RegulDirectories.Modules, "*.dll", SearchOption.AllDirectories));

        WindowsManager.MainWindow.CloseLoading();

        WindowsManager.MainWindow.ShowNotification("NeedToRestartToFinishUpdatingModule", timeSpan: TimeSpan.FromSeconds(5));
    }

    private async Task UpdateModule(Module module)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (!Directory.Exists(RegulDirectories.Cache))
                    Directory.CreateDirectory(RegulDirectories.Cache);
                
                WindowsManager.MainWindow?.ChangeLoadingProgress(0, "PreparingM", true);

                string zipFile = Path.Combine(RegulDirectories.Cache, module.Instance.Name + ".zip");
                if (File.Exists(zipFile))
                {
                    ApplicationSettings.Current.UpdatableModules.Add(new UpdatableModule
                    {
                        Path = zipFile, PathToModule = RegulDirectories.Modules
                    });

                    _synchronizationContext?.Send(_ =>
                    {
                        module.ReadyUpgrade = true;
                    }, "");

                    return;
                }

                if (module.LinkToUpdate is null) return;

                using (HttpClientDownloadWithProgress client = new(module.LinkToUpdate, zipFile))
                {
                    client.ProgressChanged += (size, downloaded, percentage) =>
                    {
                        WindowsManager.MainWindow?.ChangeLoadingProgress(percentage ?? 0, $"{App.GetString("DownloadingM")}\n{downloaded / 1024}KB / {size / 1024}KB", false);
                    };

                    await client.StartDownload();
                }

                ApplicationSettings.Current.UpdatableModules.Add(new UpdatableModule
                {
                    Path = zipFile, PathToModule = RegulDirectories.Modules
                });
                _synchronizationContext?.Send(_ =>
                {
                    module.ReadyUpgrade = true;
                }, "");
            });
        }
        catch (Exception e)
        {
            Logger.Instance.WriteLog(LogType.Error, e.ToString());
        }
    }

    #endregion
}
