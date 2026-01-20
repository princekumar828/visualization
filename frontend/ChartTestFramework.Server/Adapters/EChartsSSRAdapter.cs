using System.Diagnostics;
using System.Text.Json;
using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using ChartTestFramework.Shared.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.JSInterop;

namespace ChartTestFramework.Server.Adapters;

public class EChartsSSRAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IWebHostEnvironment _environment;
    private readonly PerformanceLogger? _logger;
    private string? _lastContainerId;

    public string Name => "ECharts SSR (Node)";

    public EChartsSSRAdapter(IJSRuntime jsRuntime, IWebHostEnvironment environment, PerformanceLogger? logger = null)
    {
        _jsRuntime = jsRuntime;
        _environment = environment;
        _logger = logger;
    }

    public async Task<RenderMetrics> RenderBoxPlot(string containerId, BoxPlotData data)
    {
        _lastContainerId = containerId;
        var metrics = new RenderMetrics 
        { 
            ChartLibrary = Name,
            DataPointCount = data.Metadata.TotalPoints,
            InitTimeMs = 0 
        };

        var sw = Stopwatch.StartNew();

        try 
        {
            // 1. Prepare Chart Option (same as client-side, but built in C#)
            var option = new 
            {
                title = new { text = $"Wafer Yield Distribution (Node SSR) - {data.Metadata.TotalPoints:N0} points" },
                tooltip = new { trigger = "item" },
                xAxis = new { 
                    type = "category", 
                    data = data.Weeks.Select(w => $"W{w.WeekNo}").ToArray() 
                },
                yAxis = new { type = "value" },
                series = new[] 
                {
                    new 
                    {
                        name = "Yield",
                        type = "boxplot",
                        data = data.Weeks.SelectMany(w => w.Lots.Select(l => new[] 
                        {
                            l.Stats.Min, l.Stats.Q1, l.Stats.Median, l.Stats.Q3, l.Stats.Max
                        })).ToArray()
                    }
                }
            };

            var payload = new 
            { 
                width = 800, 
                height = 600, 
                option = option 
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            metrics.DataBindingMs = sw.Elapsed.TotalMilliseconds; // Time to build data

            // 2. Invoke Node.js script
            var nodeScriptPath = Path.Combine(_environment.ContentRootPath, "NodeSSR", "render_echarts.js");
            var svgContent = await InvokeNodeRenderer(nodeScriptPath, jsonPayload);

            var renderTime = sw.Elapsed.TotalMilliseconds;
            metrics.TotalRenderMs = renderTime;
            metrics.RenderCompleteMs = renderTime - metrics.DataBindingMs;

            // 3. Inject SVG into DOM
            await _jsRuntime.InvokeVoidAsync("chartInterop.serverImage.renderSvg", containerId, svgContent, Name, metrics.TotalRenderMs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EChartsSSR] Error: {ex.Message}");
            await _jsRuntime.InvokeVoidAsync("console.error", $"[EChartsSSR] Error: {ex.Message}");
        }

        return metrics;
    }

    private async Task<string> InvokeNodeRenderer(string scriptPath, string inputJson)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"\"{scriptPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        await process.StandardInput.WriteAsync(inputJson);
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Node process failed with code {process.ExitCode}: {errorTask.Result}");
        }

        return outputTask.Result;
    }

    // SSR doesn't support interactive selection in this basic impl
    public Task EnableRectangularSelection(Action<SelectionRange> onSelection) => Task.CompletedTask;
    public Task ClearSelection() => Task.CompletedTask;
    public Task Resize() => Task.CompletedTask; // SSR is static image, resize not supported without re-render

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_lastContainerId))
        {
            try 
            {
                await _jsRuntime.InvokeVoidAsync("chartInterop.serverImage.destroy", _lastContainerId);
            }
            catch (JSDisconnectedException)
            {
                // Ignored
            }
        }
    }

    public Task Destroy() => DisposeAsync().AsTask();
}
