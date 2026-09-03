using System.Runtime.InteropServices;
using System.Windows.Automation;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudePromptDetector : IClaudePromptDetector, IDisposable
{
    private readonly IClaudePromptParser _parser;
    private readonly object _cacheLock = new();
    private readonly Dictionary<IntPtr, CachedAutomationElement> _elementCache = new();
    private const int MaxCacheAgeSeconds = 15; // PHASE 1 FIX: Reduced from 30s for faster refresh
    private const int MaxCacheSize = 10; // PHASE 1 FIX: Bounded cache to prevent unbounded growth
    private bool _disposed = false;

    public ClaudePromptDetector(IClaudePromptParser parser)
    {
        _parser = parser;
    }

    private class CachedAutomationElement
    {
        public AutomationElement? Element { get; set; }
        public DateTime CachedAt { get; set; }
        public int ConsecutiveFailures { get; set; }
    }

    public DetectedPrompt? DetectPrompt(ClaudeSession session)
    {
        if (!session.IsVerified)
            return null;

        var text = GetTerminalText(session.TerminalWindowHandle);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!_parser.ContainsPromptMarkers(text))
            return null;

        var request = _parser.ParsePermissionRequest(text);
        if (request == null || !request.IsValid)
            return null;

        return new DetectedPrompt
        {
            Session = session,
            RawText = text,
            Request = request
        };
    }

    public string? GetTerminalText(IntPtr windowHandle)
    {
        // SECURITY: Verify window handle is valid before attempting text extraction
        if (windowHandle == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("[ClaudePromptDetector] SECURITY: Invalid window handle (Zero)");
            return null;
        }

        // PHASE 1 FIX: Validate window still exists and is visible
        if (!IsWindowStillValid(windowHandle))
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] Window 0x{windowHandle.ToInt64():X} is no longer valid - clearing cache");
            lock (_cacheLock)
            {
                if (_elementCache.TryGetValue(windowHandle, out var stale))
                {
                    stale.Element = null;  // Release COM reference
                }
                _elementCache.Remove(windowHandle);
            }
            return null;
        }

        AutomationElement? element = null;
        bool needsRefresh = false;

        lock (_cacheLock)
        {
            if (_elementCache.TryGetValue(windowHandle, out var cached))
            {
                var age = (DateTime.UtcNow - cached.CachedAt).TotalSeconds;

                // Force refresh if cache is old OR if we've had consecutive failures
                if (age > MaxCacheAgeSeconds || cached.ConsecutiveFailures >= 3)
                {
                    needsRefresh = true;
                    // Nullify old element before refresh
                    cached.Element = null;
                }
                else
                {
                    element = cached.Element;
                }
            }
            else
            {
                needsRefresh = true;
            }
        }

        // Refresh automation element if needed
        if (needsRefresh || element == null)
        {
            try
            {
                element = AutomationElement.FromHandle(windowHandle);

                lock (_cacheLock)
                {
                    _elementCache[windowHandle] = new CachedAutomationElement
                    {
                        Element = element,
                        CachedAt = DateTime.UtcNow,
                        ConsecutiveFailures = 0
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] Failed to get AutomationElement: {ex.GetType().Name}: {ex.Message}");

                lock (_cacheLock)
                {
                    if (_elementCache.TryGetValue(windowHandle, out var cached))
                    {
                        cached.ConsecutiveFailures++;
                    }
                }
                return null;
            }
        }

        if (element == null)
            return null;

        try
        {
            var text = TryGetTextViaTextPattern(element);
            if (!string.IsNullOrWhiteSpace(text))
            {
                ResetFailureCount(windowHandle);
                return text;
            }

            text = TryGetTextViaValuePattern(element);
            if (!string.IsNullOrWhiteSpace(text))
            {
                ResetFailureCount(windowHandle);
                return text;
            }

            text = TryGetTextFromChildren(element);
            if (!string.IsNullOrWhiteSpace(text))
            {
                ResetFailureCount(windowHandle);
                return text;
            }

            // All methods returned null/empty - increment failure count
            IncrementFailureCount(windowHandle);
            return text;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] Text extraction failed: {ex.GetType().Name}: {ex.Message}");
            IncrementFailureCount(windowHandle);
            return null;
        }
    }

    private void ResetFailureCount(IntPtr windowHandle)
    {
        lock (_cacheLock)
        {
            if (_elementCache.TryGetValue(windowHandle, out var cached))
            {
                cached.ConsecutiveFailures = 0;
            }
        }
    }

    private void IncrementFailureCount(IntPtr windowHandle)
    {
        lock (_cacheLock)
        {
            if (_elementCache.TryGetValue(windowHandle, out var cached))
            {
                cached.ConsecutiveFailures++;
            }
        }
    }

    /// <summary>
    /// Manually clear the cache for a specific window (useful for recovery scenarios)
    /// </summary>
    public void ClearCache(IntPtr windowHandle)
    {
        lock (_cacheLock)
        {
            _elementCache.Remove(windowHandle);
        }
    }

    /// <summary>
    /// Clear all cached automation elements (full reset)
    /// </summary>
    public void ClearAllCaches()
    {
        lock (_cacheLock)
        {
            _elementCache.Clear();
        }
    }

    /// <summary>
    /// PHASE 1 FIX: Aggressive cache cleanup with LRU eviction
    /// Remove stale cache entries (> 2 minutes old) and enforce bounded size
    /// </summary>
    public void CleanupStaleCache()
    {
        lock (_cacheLock)
        {
            // PHASE 1 FIX: More aggressive stale threshold (2 minutes instead of 5)
            var staleThreshold = DateTime.UtcNow.AddMinutes(-2);
            var staleKeys = _elementCache
                .Where(kvp => kvp.Value.CachedAt < staleThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            // Nullify elements before removing to release COM references
            foreach (var key in staleKeys)
            {
                if (_elementCache.TryGetValue(key, out var cached))
                {
                    cached.Element = null;
                }
                _elementCache.Remove(key);
            }

            // PHASE 1 FIX: LRU eviction if cache exceeds max size
            if (_elementCache.Count > MaxCacheSize)
            {
                var excessCount = _elementCache.Count - MaxCacheSize;
                var oldestKeys = _elementCache
                    .OrderBy(kvp => kvp.Value.CachedAt)
                    .Take(excessCount)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldestKeys)
                {
                    if (_elementCache.TryGetValue(key, out var cached))
                    {
                        cached.Element = null;
                    }
                    _elementCache.Remove(key);
                }

                System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] LRU evicted {excessCount} oldest entries (cache limit: {MaxCacheSize})");
            }

            if (staleKeys.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] Cleaned up {staleKeys.Count} stale cache entries");
            }
        }
    }

    public bool CanAccessTerminalText(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            if (element == null)
                return false;

            if (SupportsPattern(element, TextPattern.Pattern))
                return true;

            if (SupportsPattern(element, ValuePattern.Pattern))
                return true;

            var editElement = FindEditControl(element);
            if (editElement != null)
            {
                if (SupportsPattern(editElement, TextPattern.Pattern))
                    return true;
                if (SupportsPattern(editElement, ValuePattern.Pattern))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private string? TryGetTextViaTextPattern(AutomationElement element)
    {
        try
        {
            if (!SupportsPattern(element, TextPattern.Pattern))
                return null;

            var textPattern = element.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (textPattern == null)
                return null;

            var documentRange = textPattern.DocumentRange;
            return documentRange?.GetText(-1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] TextPattern extraction failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private string? TryGetTextViaValuePattern(AutomationElement element)
    {
        try
        {
            if (!SupportsPattern(element, ValuePattern.Pattern))
                return null;

            var valuePattern = element.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            return valuePattern?.Current.Value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] ValuePattern extraction failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private string? TryGetTextFromChildren(AutomationElement element)
    {
        try
        {
            var editElement = FindEditControl(element);
            if (editElement == null)
                return null;

            var text = TryGetTextViaTextPattern(editElement);
            if (!string.IsNullOrWhiteSpace(text))
                return text;

            text = TryGetTextViaValuePattern(editElement);
            return text;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudePromptDetector] Child element text extraction failed: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private AutomationElement? FindEditControl(AutomationElement parent)
    {
        try
        {
            var editCondition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
            );

            var editElement = parent.FindFirst(TreeScope.Descendants, editCondition);
            return editElement;
        }
        catch
        {
            return null;
        }
    }

    private bool SupportsPattern(AutomationElement element, AutomationPattern pattern)
    {
        try
        {
            var supportedPatterns = element.GetSupportedPatterns();
            return supportedPatterns.Contains(pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// PHASE 1 FIX: Validate window handle before use
    /// </summary>
    private bool IsWindowStillValid(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        if (!IsWindow(windowHandle))
            return false;

        if (!IsWindowVisible(windowHandle))
            return false;

        return true;
    }

    /// <summary>
    /// PHASE 1 FIX: Dispose pattern to release COM objects
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_cacheLock)
        {
            // Nullify all AutomationElement references to release COM objects
            foreach (var cached in _elementCache.Values)
            {
                cached.Element = null;
            }
            _elementCache.Clear();
        }

        // Force garbage collection to release COM objects immediately
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _disposed = true;
    }

    #region Windows API for Window Validation

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    #endregion
}
