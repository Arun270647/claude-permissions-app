using System.Diagnostics;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudePermissionAssistant.Core.Services;

/// <summary>
/// Handles automatic updates by checking GitHub releases.
/// Updates are mandatory - users must update before using the app.
/// </summary>
public class AutoUpdateService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private readonly string _platform;
    private readonly Timer? _updateCheckTimer;
    private readonly string _updateManifestUrl;

    private const string GITHUB_REPO = "Arun270647/claude-permissions-app";

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<UpdateProgressEventArgs>? UpdateProgress;
    public event EventHandler<string>? UpdateCheckFailed;

    public AutoUpdateService(string currentVersion, string platform)
    {
        _currentVersion = currentVersion;
        _platform = platform.ToLower();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"ClaudePrompter/{currentVersion}");

        // Determine correct manifest URL based on platform and architecture
        var manifestPlatform = _platform;
        if (_platform == "macos")
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            manifestPlatform = arch == System.Runtime.InteropServices.Architecture.Arm64
                ? "macos-arm64"
                : "macos-x64";
        }

        _updateManifestUrl = $"https://raw.githubusercontent.com/{GITHUB_REPO}/main/latest-{manifestPlatform}.json";

        // Background check every 30 minutes (for long-running sessions)
        _updateCheckTimer = new Timer(CheckForUpdatesCallback, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Check for updates immediately on startup. Returns update info if available.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_updateManifestUrl);
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);

            if (updateInfo == null)
                return null;

            if (IsNewerVersion(updateInfo.Version, _currentVersion))
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(updateInfo));
                return updateInfo;
            }

            return null;
        }
        catch (Exception ex)
        {
            UpdateCheckFailed?.Invoke(this, $"Failed to check for updates: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download update with progress reporting and security verification
    /// </summary>
    public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        try
        {
            // SECURITY FIX: Enforce HTTPS
            if (!updateInfo.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                UpdateCheckFailed?.Invoke(this, "Update URL must use HTTPS");
                return false;
            }

            // SECURITY FIX: Verify checksum is provided
            if (string.IsNullOrWhiteSpace(updateInfo.Sha256))
            {
                UpdateCheckFailed?.Invoke(this, "Update manifest missing SHA-256 checksum");
                return false;
            }

            var extension = _platform == "windows" ? ".exe" : ".dmg";
            var tempPath = Path.Combine(Path.GetTempPath(), $"ClaudePrompter-Update-{updateInfo.Version}{extension}");

            UpdateProgress?.Invoke(this, new UpdateProgressEventArgs("Downloading update...", 0));

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
                            UpdateProgress?.Invoke(this, new UpdateProgressEventArgs(
                                $"Downloading... {downloadedBytes / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB",
                                progressPercent));
                        }
                    }
                }
            }

            // SECURITY FIX: Verify SHA-256 checksum
            UpdateProgress?.Invoke(this, new UpdateProgressEventArgs("Verifying download integrity...", 95));

            if (!VerifyChecksum(tempPath, updateInfo.Sha256))
            {
                File.Delete(tempPath);
                UpdateCheckFailed?.Invoke(this, "Downloaded file checksum verification failed - possible tampering detected");
                return false;
            }

            UpdateProgress?.Invoke(this, new UpdateProgressEventArgs("Installing update...", 100));

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

    /// <summary>
    /// SECURITY FIX: Verify SHA-256 checksum of downloaded file
    /// </summary>
    private bool VerifyChecksum(string filePath, string expectedSha256)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            var actualChecksum = Convert.ToHexString(hash).ToLowerInvariant();
            var expectedChecksum = expectedSha256.Replace(":", "").Replace("-", "").ToLowerInvariant();

            return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// SECURITY FIX: Properly escape paths to prevent command injection
    /// </summary>
    private string EscapeWindowsBatchPath(string path)
    {
        // Validate path doesn't contain batch script metacharacters
        if (path.Contains('&') || path.Contains('|') || path.Contains(';') ||
            path.Contains('^') || path.Contains('<') || path.Contains('>'))
        {
            throw new SecurityException("Invalid characters in path");
        }

        // Return path with proper quoting
        return $"\"{path.Replace("\"", "\"\"")}\"";
    }

    private void ApplyWindowsUpdate(string updatePath)
    {
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(currentExe))
        {
            throw new InvalidOperationException("Cannot determine current executable path");
        }

        var batchPath = Path.Combine(Path.GetTempPath(), "claude-prompter-update.bat");

        // SECURITY FIX: Properly escape paths to prevent command injection
        var escapedUpdatePath = EscapeWindowsBatchPath(updatePath);
        var escapedCurrentExe = EscapeWindowsBatchPath(currentExe);
        var escapedBatchPath = EscapeWindowsBatchPath(batchPath);

        var batchContent = $@"@echo off
echo Updating Claude Prompter...
timeout /t 2 /nobreak > nul
taskkill /F /IM ClaudePrompter.exe > nul 2>&1
taskkill /F /IM ClaudePermissionAssistant.exe > nul 2>&1
timeout /t 1 /nobreak > nul
copy /Y {escapedUpdatePath} {escapedCurrentExe} > nul
if exist {escapedCurrentExe} (
    start """" {escapedCurrentExe}
    echo Update complete!
) else (
    echo Update failed!
)
del {escapedUpdatePath}
del {escapedBatchPath}
";

        File.WriteAllText(batchPath, batchContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        Environment.Exit(0);
    }

    /// <summary>
    /// SECURITY FIX: Properly escape bash paths to prevent command injection
    /// </summary>
    private string EscapeBashPath(string path)
    {
        // Validate path doesn't contain shell metacharacters that could break out of quotes
        if (path.Contains('\'') || path.Contains('$') || path.Contains('`') ||
            path.Contains(';') || path.Contains('|') || path.Contains('&'))
        {
            throw new SecurityException("Invalid characters in path");
        }

        // Use single quotes for bash (stronger than double quotes)
        // Single quotes prevent all expansion except you can't include a single quote
        return $"'{path.Replace("'", "'\\''")}'";
    }

    private void ApplyMacOSUpdate(string updatePath)
    {
        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(currentExe))
        {
            throw new InvalidOperationException("Cannot determine current executable path");
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), "claude-prompter-update.sh");

        // SECURITY FIX: Properly escape paths to prevent command injection
        var escapedUpdatePath = EscapeBashPath(updatePath);
        var escapedCurrentExe = EscapeBashPath(currentExe);
        var escapedScriptPath = EscapeBashPath(scriptPath);

        var scriptContent = $@"#!/bin/bash
echo 'Updating Claude Prompter...'
sleep 2
killall ClaudePrompter 2>/dev/null
killall ClaudePermissionAssistant 2>/dev/null
sleep 1

# Handle DMG update
if [[ {escapedUpdatePath} == *.dmg ]]; then
    MOUNT_DIR=$(hdiutil attach {escapedUpdatePath} -nobrowse | tail -1 | awk '{{print $NF}}')
    if [ -d ""$MOUNT_DIR/ClaudePrompter.app"" ]; then
        rm -rf /Applications/ClaudePrompter.app
        cp -R ""$MOUNT_DIR/ClaudePrompter.app"" /Applications/
        hdiutil detach ""$MOUNT_DIR"" -quiet
        open /Applications/ClaudePrompter.app
    fi
else
    cp -f {escapedUpdatePath} {escapedCurrentExe}
    chmod +x {escapedCurrentExe}
    open {escapedCurrentExe}
fi

echo 'Update complete!'
rm -f {escapedUpdatePath}
rm -f {escapedScriptPath}
";

        File.WriteAllText(scriptPath, scriptContent);

        var chmodProcess = Process.Start("chmod", $"+x {EscapeBashPath(scriptPath)}");
        chmodProcess?.WaitForExit();

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

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("changelog")]
    public string Changelog { get; set; } = string.Empty;

    [JsonPropertyName("patchNotes")]
    public string PatchNotes { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public string PublishedAt { get; set; } = string.Empty;

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; } = true;
}

public class UpdateAvailableEventArgs : EventArgs
{
    public UpdateInfo UpdateInfo { get; }

    public UpdateAvailableEventArgs(UpdateInfo updateInfo)
    {
        UpdateInfo = updateInfo;
    }
}

public class UpdateProgressEventArgs : EventArgs
{
    public string Message { get; }
    public int ProgressPercent { get; }

    public UpdateProgressEventArgs(string message, int progressPercent)
    {
        Message = message;
        ProgressPercent = progressPercent;
    }
}
