using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using Microsoft.JSInterop;

namespace ChartTestFramework.Shared.Adapters;

/// <summary>
/// Syncfusion Charts adapter implementation
/// </summary>
public class SyncfusionAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private Action<SelectionRange>? _onSelectCallback;
    private DotNetObjectReference<SyncfusionAdapter>? _dotNetRef;
    private bool _initialized;
    
    public string Name => "Syncfusion Charts";
    
    public SyncfusionAdapter(IJSRuntime jsRuntime)
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
        
        try
        {
            // Initialize chart
            var initStart = DateTime.UtcNow;
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.init", containerId);
            metrics.InitTimeMs = (DateTime.UtcNow - initStart).TotalMilliseconds;
            
            // Prepare data for Syncfusion box plot format
            var bindStart = DateTime.UtcNow;
            var chartData = PrepareChartData(data);
            metrics.DataBindingMs = (DateTime.UtcNow - bindStart).TotalMilliseconds;
            
            // Render chart
            var renderStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.renderBoxPlot", 
                containerId, chartData, _dotNetRef);
            metrics.RenderCompleteMs = (DateTime.UtcNow - renderStart).TotalMilliseconds;
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SyncfusionAdapter] Error: {ex.Message}");
            throw;
        }
        
        metrics.TotalRenderMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        metrics.Timestamp = DateTime.UtcNow;
        
        return metrics;
    }
    
    private object PrepareChartData(BoxPlotData data)
    {
        var series = new List<object>();
        
        int index = 0;
        foreach (var week in data.Weeks)
        {
            foreach (var lot in week.Lots)
            {
                series.Add(new
                {
                    x = $"{data.Metadata.Year}/W{week.WeekNo}/{lot.LotId}",
                    minimum = lot.Stats.Min,
                    maximum = lot.Stats.Max,
                    lowerQuartile = lot.Stats.Q1,
                    upperQuartile = lot.Stats.Q3,
                    median = lot.Stats.Median,
                    mean = lot.Stats.Mean,
                    outliers = lot.Wafers.Select(w => w.Yield).ToArray(),
                    index
                });
                index++;
            }
        }
        
        return new
        {
            metadata = data.Metadata,
            series
        };
    }
    
    public async Task EnableRectangularSelection(Action<SelectionRange> onSelect)
    {
        _onSelectCallback = onSelect;
        await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.enableSelection", _dotNetRef);
    }
    
    [JSInvokable]
    public void OnRectangularSelect(double minX, double maxX, double minY, double maxY)
    {
        var range = new SelectionRange
        {
            MinX = minX,
            MaxX = maxX,
            MinY = minY,
            MaxY = maxY
        };
        
        _onSelectCallback?.Invoke(range);
    }
    
    public async Task ClearSelection()
    {
        if (_initialized)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.clearSelection");
        }
    }
    
    public async Task Resize()
    {
        if (_initialized)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.resize");
        }
    }
    
    public async Task Destroy()
    {
        if (_initialized)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.syncfusion.destroy");
            _initialized = false;
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await Destroy();
        _dotNetRef?.Dispose();
    }
}
