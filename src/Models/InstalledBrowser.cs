namespace SemaphURL.Models;

/// <summary>
/// Represents an installed browser detected in the system
/// </summary>
public class InstalledBrowser
{
    public required string Name { get; init; }
    public required string ExePath { get; init; }
    public string? IconPath { get; init; }
    public string RegistryKey { get; init; } = string.Empty;

    public override string ToString() => Name;
}

