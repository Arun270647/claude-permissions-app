using System.Windows.Automation;
using ClaudePermissionAssistant.Core.Interfaces;
using ClaudePermissionAssistant.Core.Models;

namespace ClaudePermissionAssistant.Automation.Services;

public class ClaudePromptDetector : IClaudePromptDetector
{
    private readonly IClaudePromptParser _parser;
    private readonly object _cacheLock = new();
    private readonly Dictionary<IntPtr, CachedAutomationElement> _elementCache = new();
    private const int MaxCacheAgeSeconds = 30; // Refresh cache every 30 seconds

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
    /// Remove stale cache entries (> 5 minutes old) to prevent memory bloat
    /// </summary>
    public void CleanupStaleCache()
    {
        lock (_cacheLock)
        {
            var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
            var staleKeys = _elementCache
                .Where(kvp => kvp.Value.CachedAt < staleThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in staleKeys)
            {
                _elementCache.Remove(key);
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
}
