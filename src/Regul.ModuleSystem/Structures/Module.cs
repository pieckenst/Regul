using McMaster.NETCore.Plugins;
using ReactiveUI;

namespace Regul.ModuleSystem.Structures;

/// <summary>
/// The class that contains the module instance itself and its assembly
/// </summary>
public class Module : ReactiveObject
{
    private IModule _instance = null!;
    private string? _linkToUpdate;
    private bool _hasUpdate;
    private bool _readyUpgrade;
    private Version? _newVersion;
    private Version? _regulVersionRequiered;
    private PluginLoader _pluginLoader = null!;

    public IModule Instance
    {
        get => _instance;
        private init => this.RaiseAndSetIfChanged(ref _instance, value);
    }

    public PluginLoader PluginLoader
    {
        get => _pluginLoader;
        set => this.RaiseAndSetIfChanged(ref _pluginLoader, value);
    }

    public string? LinkToUpdate
    {
        get => _linkToUpdate;
        set => this.RaiseAndSetIfChanged(ref _linkToUpdate, value);
    }
    public bool HasUpdate
    {
        get => _hasUpdate;
        set => this.RaiseAndSetIfChanged(ref _hasUpdate, value);
    }
    public bool ReadyUpgrade
    {
        get => _readyUpgrade;
        set => this.RaiseAndSetIfChanged(ref _readyUpgrade, value);
    }
    public Version? NewVersion
    {
        get => _newVersion;
        set => this.RaiseAndSetIfChanged(ref _newVersion, value);
    }
    public Version? RegulVersionRequiered
    {
        get => _regulVersionRequiered;
        set => this.RaiseAndSetIfChanged(ref _regulVersionRequiered, value);
    }

    public Module(IModule instance, PluginLoader pluginLoader)
    {
        Instance = instance;
        PluginLoader = pluginLoader;
    }
}
