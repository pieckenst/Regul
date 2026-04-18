namespace Regul.Other;

public static class RegulDirectories
{
    public static readonly string Modules = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");
    public static readonly string Cache = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
    public static readonly string Settings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
    public static readonly string Themes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
}
