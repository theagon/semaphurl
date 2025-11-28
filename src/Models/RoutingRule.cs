using System.Text.Json.Serialization;

namespace SemaphURL.Models;

/// <summary>
/// Pattern matching type for URL routing
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatternType
{
    DomainContains,
    UrlContains,
    Regex,
    DomainEquals,
    DomainStartsWith,
    DomainEndsWith
}

/// <summary>
/// Represents a single routing rule
/// </summary>
public class RoutingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public PatternType PatternType { get; set; } = PatternType.DomainContains;
    public string Pattern { get; set; } = string.Empty;
    public string BrowserPath { get; set; } = string.Empty;
    public string BrowserArgumentsTemplate { get; set; } = "\"{url}\"";
    public int Order { get; set; }

    public RoutingRule Clone() => new()
    {
        Id = Id,
        Enabled = Enabled,
        Name = Name,
        PatternType = PatternType,
        Pattern = Pattern,
        BrowserPath = BrowserPath,
        BrowserArgumentsTemplate = BrowserArgumentsTemplate,
        Order = Order
    };
}

