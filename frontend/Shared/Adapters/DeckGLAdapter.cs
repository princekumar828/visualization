using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using Microsoft.JSInterop;

namespace ChartTestFramework.Shared.Adapters;

/// <summary>
/// Deck.gl adapter for GPU-accelerated WebGL rendering
/// Extremely fast for millions of data points
/// </summary>
public class DeckGLAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private string? _currentContainerId;
    private DotNetObjectReference<DeckGLAdapter>? _dotNetRef;
    private Action<SelectionRange>? _onSelectionCallback;
    
    public string Name => "Deck.gl (WebGL)";
    
    public DeckGLAdapter(IJSRuntime jsRuntime)
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
            // Initialize Deck.gl
            var initStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.init", containerId);
            metrics.InitTimeMs = (DateTime.UtcNow - initStart).TotalMilliseconds;
            
            // Create dot net reference for callbacks
            _dotNetRef = DotNetObjectReference.Create(this);
            
            // Prepare point data for WebGL
            var bindStart = DateTime.UtcNow;
            var points = new List<object>();
            int xIndex = 0;
            
            foreach (var week in data.Weeks)
            {
                foreach (var lot in week.Lots)
                {
                    foreach (var wafer in lot.Wafers)
                    {
                        points.Add(new 
                        { 
                            position = new double[] { xIndex, wafer.Yield, 0 },
                            lot = lot.LotId, 
                            wafer = wafer.WaferId,
                            yield = wafer.Yield
                        });
                    }
                    xIndex++;
                }
            }
            
            var chartData = new
            {
                metadata = data.Metadata,
                points = points,
                totalLots = xIndex,
                yMin = 70,
                yMax = 100
            };
            metrics.DataBindingMs = (DateTime.UtcNow - bindStart).TotalMilliseconds;
            
            // Render with Deck.gl WebGL
            var renderStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.renderScatter", 
                containerId, chartData, _dotNetRef);
            metrics.RenderCompleteMs = (DateTime.UtcNow - renderStart).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeckGLAdapter] Error: {ex.Message}");
            throw;
        }
        
        metrics.TotalRenderMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        metrics.Timestamp = DateTime.UtcNow;
        
        return metrics;
    }
    
    [JSInvokable]
    public void OnSelection(double minX, double maxX, double minY, double maxY)
    {
        if (_onSelectionCallback != null)
        {
            var range = new SelectionRange
            {
                MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY
            };
            _onSelectionCallback(range);
        }
    }
    
    public async Task EnableRectangularSelection(Action<SelectionRange> onSelect)
    {
        _onSelectionCallback = onSelect;
        _dotNetRef ??= DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.enableSelection", _dotNetRef);
    }
    
    public async Task ClearSelection()
    {
        await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.clearSelection");
    }
    
    public async Task Resize()
    {
        await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.resize");
    }
    
    public async Task Destroy()
    {
        if (_currentContainerId != null)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.deckgl.destroy");
        }
        _dotNetRef?.Dispose();
    }
    
    public async ValueTask DisposeAsync()
    {
        await Destroy();
    }
}
