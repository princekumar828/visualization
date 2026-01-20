using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using Microsoft.JSInterop;

namespace ChartTestFramework.Shared.Adapters;

/// <summary>
/// ECharts-GL adapter for GPU-accelerated WebGL rendering
/// Uses scatterGL and other GL series for massive datasets
/// </summary>
public class EChartsGLAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private string? _currentContainerId;
    private DotNetObjectReference<EChartsGLAdapter>? _dotNetRef;
    private Action<SelectionRange>? _onSelectionCallback;
    
    public string Name => "ECharts-GL (WebGL)";
    
    public EChartsGLAdapter(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
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
            // Initialize ECharts with WebGL
            var initStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.init", containerId);
            metrics.InitTimeMs = (DateTime.UtcNow - initStart).TotalMilliseconds;
            
            // Create dot net reference for callbacks
            _dotNetRef = DotNetObjectReference.Create(this);
            
            // Prepare scatter data for WebGL (all wafer points)
            var bindStart = DateTime.UtcNow;
            var scatterData = new List<object>();
            int xIndex = 0;
            
            foreach (var week in data.Weeks)
            {
                foreach (var lot in week.Lots)
                {
                    foreach (var wafer in lot.Wafers)
                    {
                        scatterData.Add(new { x = xIndex, y = wafer.Yield, lot = lot.LotId, wafer = wafer.WaferId });
                    }
                    xIndex++;
                }
            }
            
            var chartData = new
            {
                metadata = data.Metadata,
                scatterData = scatterData,
                totalLots = xIndex
            };
            metrics.DataBindingMs = (DateTime.UtcNow - bindStart).TotalMilliseconds;
            
            // Render with WebGL
            var renderStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.renderScatterGL", 
                containerId, chartData, _dotNetRef);
            metrics.RenderCompleteMs = (DateTime.UtcNow - renderStart).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EChartsGLAdapter] Error: {ex.Message}");
            throw;
        }
        
        metrics.TotalRenderMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        metrics.Timestamp = DateTime.UtcNow;
        
        return metrics;
    }
    
    [JSInvokable]
    public void OnBrushSelect(double minX, double maxX, double minY, double maxY, int[] indices)
    {
        if (_onSelectionCallback != null)
        {
            var range = new SelectionRange
            {
                MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY
                // Note: indices available but SelectedPoints would need data lookup
            };
            _onSelectionCallback(range);
        }
    }
    
    public async Task EnableRectangularSelection(Action<SelectionRange> onSelect)
    {
        _onSelectionCallback = onSelect;
        _dotNetRef ??= DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.enableBrush", _dotNetRef);
    }
    
    public async Task ClearSelection()
    {
        await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.clearBrush");
    }
    
    public async Task Resize()
    {
        await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.resize");
    }
    
    public async Task Destroy()
    {
        if (_currentContainerId != null)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.echartsGL.destroy");
        }
        _dotNetRef?.Dispose();
    }
    
    public async ValueTask DisposeAsync()
    {
        await Destroy();
    }
}
