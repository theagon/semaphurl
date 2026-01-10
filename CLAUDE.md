# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```powershell
# Build
dotnet build src/SemaphURL.csproj

# Build release
dotnet build src/SemaphURL.csproj -c Release

# Run
dotnet run --project src/SemaphURL.csproj

# Run tests
dotnet test tests/SemaphURL.Tests/SemaphURL.Tests.csproj

# Run single test
dotnet test tests/SemaphURL.Tests/SemaphURL.Tests.csproj --filter "FullyQualifiedName~PatternMatcherTests.DomainContains_ShouldMatchCorrectly"
```

## Architecture Overview

SemaphURL is a Windows WPF application (.NET 8) that acts as a URL router - it registers as the system default browser and routes clicked URLs to different browsers based on configurable pattern-matching rules.

### Core Flow

1. **URL Interception**: When registered as default browser, Windows sends clicked URLs to `SemaphURL.exe` as command-line arguments
2. **Single Instance**: `SingleInstanceService` ensures only one instance runs; additional invocations send URLs via named pipes to the running instance
3. **Routing**: `RoutingService.Route()` evaluates rules in priority order using `PatternMatcher` to find matching rule
4. **Execution**: `RoutingService.ExecuteRoutingAsync()` launches the target browser with the URL

### Key Services (all registered as singletons via DI in App.xaml.cs)

- **RoutingService** (`Services/RoutingService.cs`): Core routing logic, evaluates rules and launches browsers
- **PatternMatcher** (`Services/PatternMatcher.cs`): Static class with URL matching logic for all pattern types (DomainContains, Regex, PortRange, etc.) - extracted for testability
- **ConfigurationService** (`Services/ConfigurationService.cs`): Loads/saves `AppConfig` from `%APPDATA%\SemaphURL\config.json`
- **BrowserDiscoveryService**: Auto-detects installed browsers from registry
- **HotkeyService**: Global hotkey registration (Win32 interop)
- **ClipboardService**: Monitors clipboard for URLs

### Data Models (in `Models/`)

- **AppConfig**: Root config object with rules, favorite sites, hotkeys, settings
- **RoutingRule**: Pattern type, pattern string, target browser path, priority order
- **PatternType** enum: DomainContains, DomainEquals, UrlContains, Regex, HostPort, PortEquals, PortRange, etc.

### MVVM Structure

- ViewModels use CommunityToolkit.Mvvm for `[ObservableProperty]` and `[RelayCommand]` source generators
- Main windows: MainWindow (settings), FavoriteSitesWindow (quick launch grid), UrlHistoryWindow
- Uses WPF-UI (Fluent Design) for modern Windows 11 styling

## Testing

Tests use xUnit. Pattern matching logic is the main testable unit - see `tests/SemaphURL.Tests/PatternMatcherTests.cs` for examples of testing all pattern types.

## Development Roadmap

See `ROADMAP.md` for the prioritized feature plan with checkboxes. Mark tasks `[x]` as completed.

**Current Phase:** Phase 1 - MVP Polish (onboarding, simplification, UX improvements)

## Configuration Location

All user data stored in `%APPDATA%\SemaphURL\`:
- `config.json` - main configuration and routing rules
- `history.json` - URL history (30 days)
- `favicons/` - cached site icons
- `log.txt` - application logs
