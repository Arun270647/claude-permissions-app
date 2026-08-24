using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudePermissionAssistant.Core.Services;

/// <summary>
/// Handles automatic updates by checking GitHub releases
/// </summary>
public class AutoUpdateService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private readonly string _platform; // "windows" or "macos"
    private readonly Timer? _updateCheckTimer;
    private readonly string _updateManifestUrl;

    private const string GITHUB_REPO = "Arun270647/claude-permissions-app";

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<string>? UpdateCheckFailed;

    public AutoUpdateService(string currentVersion, string platform)
    {
        _currentVersion = currentVersion;
        _platform = platform.ToLower();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"ClaudePermissionAssistant/{currentVersion}");

        // Check for updates every 4 hours
        _updateCheckTimer = new Timer(CheckForUpdatesCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(4));

        _updateManifestUrl = $"https://raw.githubusercontent.com/{GITHUB_REPO}/main/latest-{_platform}.json";
    }

    /// <summary>
    /// Check for updates immediately
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            // Fetch the latest version info from GitHub
            var response = await _httpClient.GetStringAsync(_updateManifestUrl);
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);

            if (updateInfo == null)
                return null;

            // Compare versions
            if (IsNewerVersion(updateInfo.Version, _currentVersion))
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(updateInfo));
                return updateInfo;
            }

            return null; // Already up to date
        }
        catch (Exception ex)
        {
            UpdateCheckFailed?.Invoke(this, $"Failed to check for updates: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download and apply update
    /// </summary>
    public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ClaudePermissionAssistant-Update-{updateInfo.Version}");

            if (_platform == "windows")
                tempPath += ".exe";

            // Download the update
            using (var response = await _httpClient.GetAsync(updateInfo.Url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadedBytes = 0L;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(tempPath))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progressPercent = (int)((downloadedBytes * 100) / totalBytes);
                            progress?.Report(progressPercent);
                        }
                    }
                }
            }

            // Make executable on Unix-like systems
            if (_platform == "macos")
            {
                var chmodProcess = Process.Start("chmod", $"+x \"{tempPath}\"");
                chmodProcess?.WaitForExit();
            }

            // Apply the update based on platform
            if (_platform == "windows")
            {
                ApplyWindowsUpdate(tempPath);
            }
            else if (_platform == "macos")
            {
                ApplyMacOSUpdate(tempPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            UpdateCheckFailed?.Invoke(this, $"Failed to download update: {ex.Message}");
            return false;
        }
    }

    private void ApplyWindowsUpdate(string updatePath)
    {
        // Create a batch script to replace the exe after the app exits
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        var batchPath = Path.Combine(Path.GetTempPath(), "update.bat");

        var batchContent = $@"@echo off
echo Updating Claude Permission Assistant...
timeout /t 2 /nobreak > nul
taskkill /F /IM ClaudePermissionAssistant.exe > nul 2>&1
timeout /t 1 /nobreak > nul
copy /Y ""{updatePath}"" ""{currentExe}"" > nul
if exist ""{currentExe}"" (
    start """" ""{currentExe}""
    echo Update complete!
) else (
    echo Update failed!
)
del ""{updatePath}""
del ""{batchPath}""
";

        File.WriteAllText(batchPath, batchContent);

        // Start the batch script and exit current app
        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        Environment.Exit(0);
    }

    private void ApplyMacOSUpdate(string updatePath)
    {
        // Create a shell script to replace the app after it exits
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        var scriptPath = Path.Combine(Path.GetTempPath(), "update.sh");

        var scriptContent = $@"#!/bin/bash
echo ""Updating Claude Permission Assistant...""
sleep 2
killall ClaudePermissionAssistant 2>/dev/null
sleep 1
cp -f ""{updatePath}"" ""{currentExe}""
chmod +x ""{currentExe}""
if [ -f ""{currentExe}"" ]; then
    open ""{currentExe}""
    echo ""Update complete!""
else
    echo ""Update failed!""
fi
rm ""{updatePath}""
rm ""{scriptPath}""
";

        File.WriteAllText(scriptPath, scriptContent);

        // Make script executable
        var chmodProcess = Process.Start("chmod", $"+x \"{scriptPath}\"");
        chmodProcess?.WaitForExit();

        // Start the script and exit current app
        Process.Start(new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Environment.Exit(0);
    }

    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        try
        {
            // Remove 'v' prefix if present
            latestVersion = latestVersion.TrimStart('v');
            currentVersion = currentVersion.TrimStart('v');

            var latest = new Version(latestVersion);
            var current = new Version(currentVersion);

            return latest > current;
        }
        catch
        {
            return false;
        }
    }

    private void CheckForUpdatesCallback(object? state)
    {
        Task.Run(async () => await CheckForUpdatesAsync());
    }

    public void Dispose()
    {
        _updateCheckTimer?.Dispose();
        _httpClient?.Dispose();
    }
}

public class UpdateInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public string PublishedAt { get; set; } = string.Empty;
}

public class UpdateAvailableEventArgs : EventArgs
{
    public UpdateInfo UpdateInfo { get; }

    public UpdateAvailableEventArgs(UpdateInfo updateInfo)
    {
        UpdateInfo = updateInfo;
    }
}
