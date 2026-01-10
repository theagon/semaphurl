# SemaphURL

> **Semaphore + URL** — Smart URL Router for Windows

Route URLs to different browsers based on configurable rules. Click a link anywhere, SemaphURL intercepts it and opens in the right browser automatically.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)
![Windows 11](https://img.shields.io/badge/Windows-11-blue)
![WPF](https://img.shields.io/badge/WPF-Fluent_UI-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

### 🔀 Smart URL Routing
- **Multiple pattern types**: Domain contains/equals, URL contains, Regex, and more
- **Priority-based rules**: Rules are evaluated in order, first match wins
- **Default browser fallback**: Unmatched URLs go to your default browser
- **Browser arguments**: Customize how URLs are passed to each browser

### 🛠️ Developer Mode
Enable advanced features for power users and developers:
- **Port-based routing**: Route `localhost:3000` to Chrome, `localhost:4200` to Firefox
- **Pattern types for developers**:
  - `HostPort` — exact host:port match (e.g., `localhost:3000`)
  - `PortEquals` — match any host with specific port (e.g., `3000`)
  - `PortRange` — match port range (e.g., `3000-3999`)
  - `Regex` — full regular expression support
- **URL Testing**: Test which rule matches a URL before saving
- **Keyboard shortcuts configuration**: Customize global hotkeys

### ⭐ Favorite Sites (Quick Launch)
- **Global hotkey** (default `Ctrl+Space`) — instantly open a grid of your favorite sites
- **Customizable grid**: Add, edit, delete, and reorder sites
- **Automatic favicons**: Icons are fetched and cached automatically
- **Smart routing**: Sites open through the routing engine, respecting your rules
- **Browser tooltip**: Hover to see which browser will open the site

### 📋 Clipboard URL Detection
- **Auto-detection**: Monitors clipboard for URLs automatically
- **Toast notification**: Shows alert when URL is copied
- **Quick open hotkey** (default `Ctrl+Shift+Space`) — open URL from clipboard instantly

### 📜 URL History
- **30-day history log**: All routed URLs are tracked
- **Search & filter**: Find URLs by domain, browser, or rule name
- **Quick rule creation**: Right-click any history entry → Create Rule from Domain
- **One-click re-open**: Open any URL from history again

### 🖥️ System Tray Integration
- **Minimizes to tray**: App runs quietly in the background
- **Quick access menu**: Right-click tray icon for Favorite Sites, URL History, Settings
- **Auto-start with Windows**: Optional startup on login (launches minimized)
- **Startup notification**: Shows toast when app starts

### ⌨️ Configurable Hotkeys
- **Favorite Sites hotkey**: Customize the shortcut (default `Ctrl+Space`)
- **Clipboard URL hotkey**: Customize the shortcut (default `Ctrl+Shift+Space`)
- **Live update**: Changes apply after saving settings

### 🎨 Modern UI
- **Fluent Design**: Windows 11 style with Mica backdrop
- **Dark/Light theme**: Follows system theme automatically
- **Smooth animations**: Polished user experience

### 🔧 Additional Features
- **Single instance**: Only one instance runs at a time
- **Browser auto-discovery**: Detects installed browsers automatically
- **Detailed logging**: Debug issues with comprehensive logs
- **Portable config**: Settings stored in `%APPDATA%\SemaphURL`

## 🚀 Quick Start

### Build & Run

```powershell
# Clone the repository
git clone https://github.com/theagon/semaphurl.git
cd semaphurl

# Build
dotnet build src/SemaphURL.csproj -c Release

# Run
dotnet run --project src/SemaphURL.csproj
```

### Setup

1. Launch SemaphURL
2. Click **"Register as Default Browser"** in Settings
3. Open Windows Settings → Apps → Default apps → Set SemaphURL as default browser
4. Add routing rules for your browsers
5. (Optional) Enable "Start with Windows" for auto-startup
6. (Optional) Enable "Developer Mode" in Application Settings for advanced features

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Space` | Open Favorite Sites popup (global, configurable) |
| `Ctrl+Shift+Space` | Open URL from clipboard (global, configurable) |
| `Escape` | Close Favorite Sites / Cancel editing |

## 📁 Configuration

Settings are stored in `%APPDATA%\SemaphURL\`:

```
SemaphURL/
├── config.json      # Main configuration and routing rules
├── history.json     # URL history (last 30 days)
├── favicons/        # Cached site icons
└── log.txt          # Application logs
```

### Pattern Types

| Pattern Type | Description | Example | Mode |
|--------------|-------------|---------|------|
| `DomainContains` | Domain contains string | `youtube` matches `youtube.com` | Basic |
| `DomainEquals` | Exact domain match | `github.com` | Basic |
| `UrlContains` | URL contains string | `/watch?v=` | Basic |
| `Regex` | Regular expression | `^https://.*\.google\.com` | Developer |
| `DomainStartsWith` | Domain starts with | `mail.` | Developer |
| `DomainEndsWith` | Domain ends with | `.edu` | Developer |
| `HostPort` | Exact host:port | `localhost:3000` | Developer |
| `PortEquals` | Any host, specific port | `3000` | Developer |
| `PortRange` | Port in range | `3000-3999` | Developer |

### Example Rule Configuration

```json
{
  "rules": [
    {
      "name": "Work Sites",
      "pattern": "company.com",
      "patternType": "DomainContains",
      "browserPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
      "enabled": true
    },
    {
      "name": "YouTube",
      "pattern": "youtube.com",
      "patternType": "DomainContains",
      "browserPath": "C:\\Program Files\\Mozilla Firefox\\firefox.exe",
      "enabled": true
    },
    {
      "name": "React Dev Server",
      "pattern": "localhost:3000",
      "patternType": "HostPort",
      "browserPath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
      "enabled": true
    },
    {
      "name": "All Dev Ports",
      "pattern": "3000-9999",
      "patternType": "PortRange",
      "browserPath": "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
      "enabled": true
    }
  ],
  "developerMode": true
}
```

## 🛠️ Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Administrator rights (for browser registration)

## 📖 Documentation

See [docs/README.md](docs/README.md) for detailed documentation (in Russian).

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

Made with ❤️ for productivity enthusiasts who use multiple browsers.
