using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Collections;
using ReactiveUI;
using Regul.Enums;
using Regul.Other;
using Regul.Structures;

namespace Regul;

[DataContract]
public class ApplicationSettings : ReactiveObject
{
    public static ApplicationSettings Current = new();

    private string _language = null!;
    private string _creatorName = string.Empty;
    private string _virusTotalApiKey = string.Empty;
    private string _key = Guid.NewGuid().ToString("N");
    private bool _hardwareAcceleration = true;
    private bool _scanForVirus;

    private CheckUpdateInterval _checkUpdateInterval = CheckUpdateInterval.EveryWeek;

    private AvaloniaList<Project> _projects = new();
    private AvaloniaList<UpdatableModule> _updatableModules = new();
    private AvaloniaList<EditorRelatedExtension> _editorRelatedExtensions = new();

    [DataMember]
    public string Key
    {
        get => _key;
        set => this.RaiseAndSetIfChanged(ref _key, value);
    }

    [DataMember]
    public string Language
    {
        get => _language;
        set => this.RaiseAndSetIfChanged(ref _language, value);
    }

    [DataMember]
    public bool HardwareAcceleration
    {
        get => _hardwareAcceleration;
        set => this.RaiseAndSetIfChanged(ref _hardwareAcceleration, value);
    }

    [DataMember]
    public AvaloniaList<Project> Projects
    {
        get => _projects;
        set => this.RaiseAndSetIfChanged(ref _projects, value);
    }

    [DataMember]
    public AvaloniaList<UpdatableModule> UpdatableModules
    {
        get => _updatableModules;
        set => this.RaiseAndSetIfChanged(ref _updatableModules, value);
    }

    [DataMember]
    public AvaloniaList<EditorRelatedExtension> EditorRelatedExtensions
    {
        get => _editorRelatedExtensions;
        set => this.RaiseAndSetIfChanged(ref _editorRelatedExtensions, value);
    }

    [DataMember]
    public string CreatorName
    {
        get => _creatorName;
        set => this.RaiseAndSetIfChanged(ref _creatorName, value);
    }

    [DataMember]
    public string VirusTotalApiKey
    {
        get => _virusTotalApiKey;
        set => this.RaiseAndSetIfChanged(ref _virusTotalApiKey, value);
    }

    [DataMember]
    public CheckUpdateInterval CheckUpdateInterval
    {
        get => _checkUpdateInterval;
        set => this.RaiseAndSetIfChanged(ref _checkUpdateInterval, value);
    }
    
    [DataMember]
    public bool ScanForVirus
    {
        get => _scanForVirus;
        set => this.RaiseAndSetIfChanged(ref _scanForVirus, value);
    }

    [DataMember]
    public string? DateOfLastUpdateCheck { get; set; }

    [DataMember]
    public bool UserAgreementAdopted { get; set; }

    [JsonIgnore]
    internal bool ExceptionCalled { get; set; }

    [JsonIgnore]
    internal bool RestartingApp { get; set; }

    [JsonIgnore]
    internal string ExceptionText { get; set; } = string.Empty;

    public ApplicationSettings() => Setup();

    private void Setup() => Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

    public static void Load()
    {
        if (!Directory.Exists(RegulDirectories.Settings))
            Directory.CreateDirectory(RegulDirectories.Settings);

        string appSettings = Path.Combine(RegulDirectories.Settings, "settings.json");
        System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] Loading from: {appSettings}");
        System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] File exists: {File.Exists(appSettings)}");
        
        if (!File.Exists(appSettings)) return;

        using FileStream fileStream = File.OpenRead(appSettings);
        try
        {
            Current = JsonSerializer.Deserialize<ApplicationSettings>(fileStream)!;
            System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] Loaded successfully. RestartingApp: {Current.RestartingApp}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] Load failed: {ex.Message}");
        }
        
        if (string.IsNullOrWhiteSpace(Current.VirusTotalApiKey) || Current.VirusTotalApiKey.Length < 64)
            Current.ScanForVirus = false;
    }

    public static void Save()
    {
        string settingsPath = Path.Combine(RegulDirectories.Settings, "settings.json");
        System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] Saving to: {settingsPath}");
        System.Diagnostics.Debug.WriteLine($"[ApplicationSettings] RestartingApp flag: {Current.RestartingApp}");
        
        using FileStream fileStream = File.Create(settingsPath);
        JsonSerializer.Serialize(fileStream, Current);
        
        System.Diagnostics.Debug.WriteLine("[ApplicationSettings] Save completed");
    }

    public static void Reset()
    {
        Current.Setup();

        Language? language = App.Languages.FirstOrDefault(x =>
            x.Key == Current.Language ||
            x.AdditionalKeys.Any(lang => lang == Current.Language));

        string? key = language.Value.Key;

        if (string.IsNullOrWhiteSpace(key))
            key = "en";

        Current.Language = key;

        Current.HardwareAcceleration = true;
    }
}
