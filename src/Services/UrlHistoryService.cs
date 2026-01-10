using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SemaphURL.Models;

namespace SemaphURL.Services;

public interface IUrlHistoryService
{
    IReadOnlyList<UrlHistoryEntry> Entries { get; }
    int EntryCount { get; }
    UrlHistoryEntry? LastEntry { get; }
    
    Task LoadAsync();
    Task SaveAsync();
    Task AddEntryAsync(UrlHistoryEntry entry);
    Task AddEntryAsync(string url, string browserPath, string browserName, string? ruleName);
    IEnumerable<UrlHistoryEntry> GetHistoryByDomain(string domain);
    IEnumerable<UrlHistoryEntry> Search(string searchText);
    IEnumerable<UrlHistoryEntry> GetRecentUrls(int count = 5);
    Task DeleteEntryAsync(Guid id);
    Task ClearHistoryAsync();
}

/// <summary>
/// Service for managing URL history with 7-day retention
/// </summary>
public class UrlHistoryService : IUrlHistoryService
{
    private const int RetentionDays = 7;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILoggingService _logger;
    private readonly string _historyPath;
    private UrlHistoryData _data = new();
    private readonly object _lock = new();

    public IReadOnlyList<UrlHistoryEntry> Entries => _data.Entries.AsReadOnly();
    public int EntryCount => _data.Entries.Count;
    public UrlHistoryEntry? LastEntry => _data.Entries.OrderByDescending(e => e.Timestamp).FirstOrDefault();

    public UrlHistoryService(ILoggingService logger)
    {
        _logger = logger;
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appDataPath, "SemaphURL");
        Directory.CreateDirectory(dataDir);
        _historyPath = Path.Combine(dataDir, "history.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = await File.ReadAllTextAsync(_historyPath);
                _data = JsonSerializer.Deserialize<UrlHistoryData>(json, JsonOptions) ?? new UrlHistoryData();
                
                // Cleanup old entries if needed
                await CleanupOldEntriesAsync();
                
                _logger.LogInfo($"Loaded {_data.Entries.Count} URL history entries");
            }
            else
            {
                _data = new UrlHistoryData();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load URL history", ex);
            _data = new UrlHistoryData();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(_historyPath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save URL history", ex);
        }
        
        await Task.CompletedTask;
    }

    public async Task AddEntryAsync(UrlHistoryEntry entry)
    {
        lock (_lock)
        {
            _data.Entries.Add(entry);
        }
        
        await SaveAsync();
    }

    public async Task AddEntryAsync(string url, string browserPath, string browserName, string? ruleName)
    {
        var entry = new UrlHistoryEntry(url, browserPath, browserName, ruleName);
        await AddEntryAsync(entry);
    }

    public IEnumerable<UrlHistoryEntry> GetHistoryByDomain(string domain)
    {
        return _data.Entries
            .Where(e => e.Domain.Contains(domain, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp);
    }

    public IEnumerable<UrlHistoryEntry> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _data.Entries.OrderByDescending(e => e.Timestamp);

        return _data.Entries
            .Where(e =>
                e.Url.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                e.Domain.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (e.RuleName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                e.BrowserName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp);
    }

    public IEnumerable<UrlHistoryEntry> GetRecentUrls(int count = 5)
    {
        return _data.Entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count);
    }

    public async Task DeleteEntryAsync(Guid id)
    {
        lock (_lock)
        {
            _data.Entries.RemoveAll(e => e.Id == id);
        }
        
        await SaveAsync();
    }

    public async Task ClearHistoryAsync()
    {
        lock (_lock)
        {
            _data.Entries.Clear();
            _data.LastCleanup = DateTime.UtcNow;
        }
        
        await SaveAsync();
        _logger.LogInfo("URL history cleared");
    }

    private async Task CleanupOldEntriesAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);
        
        lock (_lock)
        {
            var removedCount = _data.Entries.RemoveAll(e => e.Timestamp < cutoffDate);
            
            if (removedCount > 0)
            {
                _logger.LogInfo($"Cleaned up {removedCount} old URL history entries");
            }
            
            _data.LastCleanup = DateTime.UtcNow;
        }
        
        if (_data.Entries.Count > 0)
        {
            await SaveAsync();
        }
    }
}

