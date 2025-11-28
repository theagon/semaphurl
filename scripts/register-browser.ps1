# SemaphURL Browser Registration Script
# Run this script as Administrator to register SemaphURL as a browser

param(
    [Parameter(Mandatory=$false)]
    [string]$ExePath
)

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

# Find the executable
if ([string]::IsNullOrEmpty($ExePath)) {
    $possiblePaths = @(
        (Join-Path $PSScriptRoot "..\src\bin\Release\net8.0-windows\win-x64\SemaphURL.exe"),
        (Join-Path $PSScriptRoot "..\src\bin\Debug\net8.0-windows\win-x64\SemaphURL.exe"),
        (Join-Path $PSScriptRoot "..\publish\SemaphURL.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\SemaphURL\SemaphURL.exe")
    )
    
    foreach ($path in $possiblePaths) {
        $resolved = Resolve-Path $path -ErrorAction SilentlyContinue
        if ($resolved -and (Test-Path $resolved)) {
            $ExePath = $resolved.Path
            break
        }
    }
}

if ([string]::IsNullOrEmpty($ExePath) -or -not (Test-Path $ExePath)) {
    Write-Host "SemaphURL.exe not found!" -ForegroundColor Red
    Write-Host "Please provide the path: .\register-browser.ps1 -ExePath 'C:\Path\To\SemaphURL.exe'" -ForegroundColor Yellow
    exit 1
}

$ExePath = (Resolve-Path $ExePath).Path
Write-Host "Registering SemaphURL from: $ExePath" -ForegroundColor Cyan

try {
    # User Classes - URL Protocol Handler
    Write-Host "Creating URL Protocol Handler..." -ForegroundColor Gray
    New-Item -Path "HKCU:\Software\Classes\SemaphURL" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Classes\SemaphURL" -Name "(Default)" -Value "SemaphURL URL Handler"
    Set-ItemProperty -Path "HKCU:\Software\Classes\SemaphURL" -Name "URL Protocol" -Value ""
    
    New-Item -Path "HKCU:\Software\Classes\SemaphURL\DefaultIcon" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Classes\SemaphURL\DefaultIcon" -Name "(Default)" -Value "`"$ExePath`",0"
    
    New-Item -Path "HKCU:\Software\Classes\SemaphURL\shell\open\command" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Classes\SemaphURL\shell\open\command" -Name "(Default)" -Value "`"$ExePath`" `"%1`""

    # StartMenuInternet Registration
    Write-Host "Registering as StartMenuInternet browser..." -ForegroundColor Gray
    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL" -Name "(Default)" -Value "SemaphURL"

    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\DefaultIcon" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\DefaultIcon" -Name "(Default)" -Value "`"$ExePath`",0"

    # Capabilities
    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities" -Name "ApplicationName" -Value "SemaphURL"
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities" -Name "ApplicationDescription" -Value "Smart URL Router"
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities" -Name "ApplicationIcon" -Value "`"$ExePath`",0"

    # URL Associations
    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\URLAssociations" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\URLAssociations" -Name "http" -Value "SemaphURL"
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\URLAssociations" -Name "https" -Value "SemaphURL"

    # File Associations
    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\FileAssociations" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\FileAssociations" -Name ".htm" -Value "SemaphURL"
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\Capabilities\FileAssociations" -Name ".html" -Value "SemaphURL"

    # Shell open command
    New-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\shell\open\command" -Force | Out-Null
    Set-ItemProperty -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL\shell\open\command" -Name "(Default)" -Value "`"$ExePath`""

    # Register in HKLM (requires admin)
    Set-ItemProperty -Path "HKLM:\SOFTWARE\RegisteredApplications" -Name "SemaphURL" -Value "Software\Clients\StartMenuInternet\SemaphURL\Capabilities"

    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host "Registration complete!" -ForegroundColor Green
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Open Windows Settings (Win + I)" -ForegroundColor White
    Write-Host "2. Go to Apps -> Default apps" -ForegroundColor White
    Write-Host "3. Search for 'SemaphURL'" -ForegroundColor White
    Write-Host "4. Set it as default for HTTP and HTTPS" -ForegroundColor White
    Write-Host ""

    $openSettings = Read-Host "Open Default Apps settings now? (Y/N)"
    if ($openSettings -eq 'Y' -or $openSettings -eq 'y') {
        Start-Process "ms-settings:defaultapps"
    }
}
catch {
    Write-Host "Error during registration: $_" -ForegroundColor Red
    exit 1
}
