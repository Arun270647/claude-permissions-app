# Development watch script - auto-rebuild and restart on file changes
Write-Host "========================================"
Write-Host "Claude Permission Assistant - Dev Watch"
Write-Host "========================================"
Write-Host ""

$projectRoot = Get-Location
$srcPath = Join-Path $projectRoot "src"
$exePath = Join-Path $projectRoot "publish\win-x64\ClaudePermissionAssistant.exe"
$processName = "ClaudePermissionAssistant"

# Function to kill running app
function Stop-App {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Stopping running app..." -ForegroundColor Yellow
        $processes | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}

# Function to rebuild and restart
function Rebuild-And-Restart {
    Write-Host ""
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ========== REBUILDING ==========" -ForegroundColor Cyan

    # Stop running app
    Stop-App

    # Rebuild
    & "$projectRoot\rebuild.bat" | Out-Host

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Build successful! Starting app..." -ForegroundColor Green

        # Start the published exe
        if (Test-Path $exePath) {
            Start-Process -FilePath $exePath
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] App started. Watching for changes..." -ForegroundColor Green
        } else {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: Executable not found at $exePath" -ForegroundColor Red
        }
    } else {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Build failed! Fix errors and save to retry." -ForegroundColor Red
    }

    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ===============================" -ForegroundColor Cyan
    Write-Host ""
}

# Initial build and start
Write-Host "Starting initial build..." -ForegroundColor Cyan
Rebuild-And-Restart

# Create file watcher
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $srcPath
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

# Watch for C#, XAML, and csproj changes
$watcher.Filter = "*.*"

# Debounce: wait 1 second after last change before rebuilding
$lastChange = [DateTime]::Now
$debounceSeconds = 1

# Define the action to take when a file changes
$action = {
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    $extension = [System.IO.Path]::GetExtension($path)

    # Only rebuild for relevant file types
    if ($extension -in @('.cs', '.xaml', '.csproj', '.xml')) {
        $global:lastChange = [DateTime]::Now
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Changed: $path" -ForegroundColor Gray
    }
}

# Register event handlers
Register-ObjectEvent -InputObject $watcher -EventName Changed -Action $action | Out-Null
Register-ObjectEvent -InputObject $watcher -EventName Created -Action $action | Out-Null
Register-ObjectEvent -InputObject $watcher -EventName Deleted -Action $action | Out-Null
Register-ObjectEvent -InputObject $watcher -EventName Renamed -Action $action | Out-Null

Write-Host "Watching for changes in: $srcPath"
Write-Host "Press Ctrl+C to stop"
Write-Host ""

# Monitor for changes and rebuild after debounce period
try {
    while ($true) {
        Start-Sleep -Milliseconds 500

        $timeSinceLastChange = ([DateTime]::Now - $lastChange).TotalSeconds

        # If changes detected and debounce period passed, rebuild
        if ($timeSinceLastChange -lt $debounceSeconds -and $timeSinceLastChange -gt 0) {
            # Still within debounce window, keep waiting
        } elseif ($timeSinceLastChange -ge $debounceSeconds -and $timeSinceLastChange -lt ($debounceSeconds + 0.5)) {
            # Debounce window passed, trigger rebuild
            Rebuild-And-Restart
            $lastChange = [DateTime]::Now.AddYears(-1) # Reset to prevent repeated rebuilds
        }
    }
}
finally {
    # Cleanup on exit
    Write-Host ""
    Write-Host "Stopping watcher..." -ForegroundColor Yellow
    $watcher.EnableRaisingEvents = $false
    $watcher.Dispose()
    Get-EventSubscriber | Unregister-Event
    Stop-App
}
