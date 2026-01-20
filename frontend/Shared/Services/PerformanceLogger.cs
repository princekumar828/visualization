using System.Diagnostics;
using System.Text.Json;
using ChartTestFramework.Shared.Models;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// Client-side performance logging service
/// Tracks timing metrics for data fetch, parse, and render operations
/// </summary>
public class PerformanceLogger
{
    private readonly List<TestResult> _results = new();
    private readonly Dictionary<string, Stopwatch> _activeTimers = new();
    
    public event Action? OnMetricsUpdated;
    
    /// <summary>
    /// Start a named timer
    /// </summary>
    public void StartTimer(string name)
    {
        var sw = new Stopwatch();
        sw.Start();
        _activeTimers[name] = sw;
        Log($"Timer started: {name}");
    }
    
    /// <summary>
    /// Stop a named timer and return elapsed milliseconds
    /// </summary>
    public double StopTimer(string name)
    {
        if (_activeTimers.TryGetValue(name, out var sw))
        {
            sw.Stop();
            var elapsed = sw.Elapsed.TotalMilliseconds;
            _activeTimers.Remove(name);
            Log($"Timer stopped: {name} = {elapsed:F2}ms");
            return elapsed;
        }
        return 0;
    }
    
    /// <summary>
    /// Get elapsed time without stopping timer
    /// </summary>
    public double GetElapsed(string name)
    {
        if (_activeTimers.TryGetValue(name, out var sw))
        {
            return sw.Elapsed.TotalMilliseconds;
        }
        return 0;
    }
    
    /// <summary>
    /// Log a test result
    /// </summary>
    public void LogResult(TestResult result)
    {
        _results.Add(result);
        Log($"Test complete: {result.ChartLibrary} | {result.DataPoints} points | " +
            $"Total: {result.TotalEndToEndMs:F2}ms");
        OnMetricsUpdated?.Invoke();
    }
    
    /// <summary>
    /// Get all logged results
    /// </summary>
    public IReadOnlyList<TestResult> GetResults() => _results.AsReadOnly();
    
    /// <summary>
    /// Get results filtered by chart library
    /// </summary>
    public IEnumerable<TestResult> GetResultsByLibrary(string library) =>
        _results.Where(r => r.ChartLibrary == library);
    
    /// <summary>
    /// Get results filtered by render mode
    /// </summary>
    public IEnumerable<TestResult> GetResultsByRenderMode(string mode) =>
        _results.Where(r => r.RenderMode == mode);
    
    /// <summary>
    /// Clear all stored results
    /// </summary>
    public void Clear()
    {
        _results.Clear();
        _activeTimers.Clear();
        OnMetricsUpdated?.Invoke();
    }
    
    /// <summary>
    /// Export results as JSON
    /// </summary>
    public string ExportAsJson()
    {
        return JsonSerializer.Serialize(_results, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
    
    /// <summary>
    /// Export results as CSV
    /// </summary>
    public string ExportAsCsv()
    {
        var lines = new List<string>
        {
            "TestId,Timestamp,ChartLibrary,RenderMode,DataPoints,ServerTotalMs,FetchTimeMs,ParseTimeMs,RenderCompleteMs,TotalEndToEndMs"
        };
        
        foreach (var r in _results)
        {
            lines.Add($"{r.TestId},{r.Timestamp:O},{r.ChartLibrary},{r.RenderMode},{r.DataPoints}," +
                     $"{r.ServerTotalMs:F2},{r.FetchTimeMs:F2},{r.ParseTimeMs:F2},{r.RenderCompleteMs:F2},{r.TotalEndToEndMs:F2}");
        }
        
        return string.Join("\n", lines);
    }
    
    private void Log(string message)
    {
        Console.WriteLine($"[PerformanceLogger] {DateTime.Now:HH:mm:ss.fff} | {message}");
    }
}
