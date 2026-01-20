using System.Diagnostics;
using System.Net.Http.Json;
using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// Test runner service that orchestrates chart performance tests
/// </summary>
public class TestRunner
{
    private readonly HttpClient _httpClient;
    private readonly PerformanceLogger _logger;
    private readonly string _renderMode;
    
    public string ApiBaseUrl { get; set; } = "http://localhost:8000";
    
    public TestRunner(HttpClient httpClient, PerformanceLogger logger, string renderMode)
    {
        _httpClient = httpClient;
        _logger = logger;
        _renderMode = renderMode;
    }
    
    /// <summary>
    /// Run a single test with specified parameters
    /// </summary>
    public async Task<TestResult> RunTest(
        IChartAdapter adapter,
        string containerId,
        int weeks = 52,
        int lotsPerWeek = 10,
        int wafersPerLot = 25)
    {
        var result = new TestResult
        {
            ChartLibrary = adapter.Name,
            RenderMode = _renderMode,
            Weeks = weeks,
            LotsPerWeek = lotsPerWeek,
            WafersPerLot = wafersPerLot
        };
        
        try
        {
            // Fetch data
            _logger.StartTimer("fetch");
            var url = $"{ApiBaseUrl}/api/charts/boxplot?weeks={weeks}&lots_per_week={lotsPerWeek}&wafers_per_lot={wafersPerLot}";
            var response = await _httpClient.GetAsync(url);
            result.FetchTimeMs = _logger.StopTimer("fetch");
            
            // Parse response
            _logger.StartTimer("parse");
            var apiResponse = await response.Content.ReadFromJsonAsync<BoxPlotApiResponse>();
            result.ParseTimeMs = _logger.StopTimer("parse");
            
            if (apiResponse?.Data == null)
            {
                throw new Exception("Invalid API response");
            }
            
            // Extract server timing (detailed breakdown)
            result.ServerArrayGenerationMs = apiResponse.Timing.Server.ArrayGenerationMs;
            result.ServerYieldGenerationMs = apiResponse.Timing.Server.YieldGenerationMs;
            result.ServerDataframeCreationMs = apiResponse.Timing.Server.DataframeCreationMs;
            result.ServerTransformationMs = apiResponse.Timing.Server.BoxplotTransformationMs;
            result.ServerTotalMs = apiResponse.Timing.Server.EndpointTotalMs;
            result.DataPoints = apiResponse.Timing.Server.TotalDataPoints;
            
            // Render chart
            var renderMetrics = await adapter.RenderBoxPlot(containerId, apiResponse.Data);
            result.ChartInitMs = renderMetrics.InitTimeMs;
            result.DataBindingMs = renderMetrics.DataBindingMs;
            result.RenderCompleteMs = renderMetrics.TotalRenderMs;
            result.TotalClientMs = result.FetchTimeMs + result.ParseTimeMs + result.RenderCompleteMs;
            
            _logger.LogResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestRunner] Error: {ex.Message}");
            throw;
        }
        
        return result;
    }
    
    /// <summary>
    /// Run multiple iterations of a test
    /// </summary>
    public async Task<List<TestResult>> RunTestIterations(
        IChartAdapter adapter,
        string containerId,
        int iterations,
        int weeks = 52,
        int lotsPerWeek = 10,
        int wafersPerLot = 25)
    {
        var results = new List<TestResult>();
        
        for (int i = 0; i < iterations; i++)
        {
            Console.WriteLine($"[TestRunner] Iteration {i + 1}/{iterations}");
            await adapter.Destroy(); // Clean up previous render
            var result = await RunTest(adapter, containerId, weeks, lotsPerWeek, wafersPerLot);
            results.Add(result);
            
            // Small delay between iterations
            await Task.Delay(100);
        }
        
        return results;
    }
    
    /// <summary>
    /// Compare two chart adapters with same parameters
    /// </summary>
    public async Task<Dictionary<string, List<TestResult>>> CompareAdapters(
        IEnumerable<IChartAdapter> adapters,
        string containerId,
        int iterations = 3,
        int weeks = 52,
        int lotsPerWeek = 10,
        int wafersPerLot = 25)
    {
        var comparison = new Dictionary<string, List<TestResult>>();
        
        foreach (var adapter in adapters)
        {
            Console.WriteLine($"[TestRunner] Testing {adapter.Name}...");
            var results = await RunTestIterations(
                adapter, containerId, iterations, weeks, lotsPerWeek, wafersPerLot);
            comparison[adapter.Name] = results;
        }
        
        return comparison;
    }
}
