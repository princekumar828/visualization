using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using ChartTestFramework.Shared.Services;
using Microsoft.JSInterop;

namespace ChartTestFramework.Shared.Adapters;

/// <summary>
/// Server-rendered image adapter using .NET chart libraries
/// Chart is rendered on the server as an image - TRUE SSR
/// Supports: ScottPlot, OxyPlot, SkiaSharp
/// </summary>
public class ServerImageAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IChartImageGenerator _generator;
    private string? _currentContainerId;
    
    public string Name => $"Server Image ({_generator.Name})";
    
    public ServerImageAdapter(IJSRuntime jsRuntime, IChartImageGenerator generator)
    {
        _jsRuntime = jsRuntime;
        _generator = generator;
    }
    
    public async Task<RenderMetrics> RenderBoxPlot(string containerId, BoxPlotData data)
    {
        var metrics = new RenderMetrics
        {
            ChartLibrary = Name,
            DataPointCount = data.Metadata.TotalPoints
        };
        
        var startTime = DateTime.UtcNow;
        _currentContainerId = containerId;
        
        try
        {
            // Generate image using the .NET chart library
            var initStart = DateTime.UtcNow;
            var (imageBytes, imgMetrics) = await _generator.GenerateBoxPlotImage(data);
            metrics.InitTimeMs = imgMetrics.TotalMs;
            
            // Convert to base64 for display
            var bindStart = DateTime.UtcNow;
            var base64 = Convert.ToBase64String(imageBytes);
            var dataUri = $"data:image/png;base64,{base64}";
            metrics.DataBindingMs = (DateTime.UtcNow - bindStart).TotalMilliseconds;
            
            // Display image in container via JS
            var renderStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.serverImage.render", 
                containerId, 
                dataUri,
                _generator.Name,
                imgMetrics.TotalMs);
            metrics.RenderCompleteMs = (DateTime.UtcNow - renderStart).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServerImageAdapter] Error: {ex.Message}");
            throw;
        }
        
        metrics.TotalRenderMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        metrics.Timestamp = DateTime.UtcNow;
        
        return metrics;
    }
    
    public Task EnableRectangularSelection(Action<SelectionRange> onSelect)
    {
        // Server-rendered images don't support interactive selection
        Console.WriteLine($"[ServerImageAdapter/{_generator.Name}] Selection not supported in image mode");
        return Task.CompletedTask;
    }
    
    public Task ClearSelection()
    {
        return Task.CompletedTask;
    }
    
    public async Task Resize()
    {
        await Task.CompletedTask;
    }
    
    public async Task Destroy()
    {
        if (_currentContainerId != null)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.serverImage.destroy", _currentContainerId);
            _currentContainerId = null;
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await Destroy();
    }
}
