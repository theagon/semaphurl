# SemaphURL Browser Unregistration Script
# Run this script as Administrator to remove SemaphURL registration

# Check for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "Removing SemaphURL registration..." -ForegroundColor Cyan

try {
    # Remove URL Protocol Handler
    if (Test-Path "HKCU:\Software\Classes\SemaphURL") {
        Remove-Item -Path "HKCU:\Software\Classes\SemaphURL" -Recurse -Force
        Write-Host "Removed URL Protocol Handler" -ForegroundColor Gray
    }

    # Remove StartMenuInternet Registration
    if (Test-Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL") {
        Remove-Item -Path "HKCU:\Software\Clients\StartMenuInternet\SemaphURL" -Recurse -Force
        Write-Host "Removed StartMenuInternet registration" -ForegroundColor Gray
    }

    # Remove from RegisteredApplications (HKCU)
    try {
        $regApps = Get-ItemProperty -Path "HKCU:\Software\RegisteredApplications" -ErrorAction SilentlyContinue
        if ($regApps.SemaphURL) {
            Remove-ItemProperty -Path "HKCU:\Software\RegisteredApplications" -Name "SemaphURL" -Force
            Write-Host "Removed from HKCU RegisteredApplications" -ForegroundColor Gray
        }
    }
    catch { }

    # Remove from RegisteredApplications (HKLM)
    try {
        $regApps = Get-ItemProperty -Path "HKLM:\SOFTWARE\RegisteredApplications" -ErrorAction SilentlyContinue
        if ($regApps.SemaphURL) {
            Remove-ItemProperty -Path "HKLM:\SOFTWARE\RegisteredApplications" -Name "SemaphURL" -Force
            Write-Host "Removed from HKLM RegisteredApplications" -ForegroundColor Gray
        }
    }
    catch { }

    Write-Host ""
    Write-Host "SemaphURL has been unregistered." -ForegroundColor Green
    Write-Host "You may need to select a new default browser in Windows Settings." -ForegroundColor Yellow
}
catch {
    Write-Host "Error during unregistration: $_" -ForegroundColor Red
    exit 1
}
