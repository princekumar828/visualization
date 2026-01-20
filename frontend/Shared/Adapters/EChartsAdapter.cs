using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Models;
using Microsoft.JSInterop;

namespace ChartTestFramework.Shared.Adapters;

/// <summary>
/// Apache ECharts adapter implementation
/// </summary>
public class EChartsAdapter : IChartAdapter
{
    private readonly IJSRuntime _jsRuntime;
    private Action<SelectionRange>? _onSelectCallback;
    private DotNetObjectReference<EChartsAdapter>? _dotNetRef;
    private bool _initialized;
    
    public string Name => "Apache ECharts";
    
    public EChartsAdapter(IJSRuntime jsRuntime)
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
            await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.init", containerId);
            metrics.InitTimeMs = (DateTime.UtcNow - initStart).TotalMilliseconds;
            
            // Prepare data for ECharts box plot format
            var bindStart = DateTime.UtcNow;
            var chartData = PrepareChartData(data);
            metrics.DataBindingMs = (DateTime.UtcNow - bindStart).TotalMilliseconds;
            
            // Render chart
            var renderStart = DateTime.UtcNow;
            await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.renderBoxPlot", 
                containerId, chartData, _dotNetRef);
            metrics.RenderCompleteMs = (DateTime.UtcNow - renderStart).TotalMilliseconds;
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EChartsAdapter] Error: {ex.Message}");
            throw;
        }
        
        metrics.TotalRenderMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        metrics.Timestamp = DateTime.UtcNow;
        
        return metrics;
    }
    
    private object PrepareChartData(BoxPlotData data)
    {
        // Prepare hierarchical axis categories
        var yearLabels = new List<string>();
        var weekLabels = new List<string>();
        var lotLabels = new List<string>();
        var boxplotData = new List<double[]>();
        var scatterData = new List<object>();
        
        int index = 0;
        foreach (var week in data.Weeks)
        {
            foreach (var lot in week.Lots)
            {
                yearLabels.Add(data.Metadata.Year.ToString());
                weekLabels.Add($"W{week.WeekNo}");
                lotLabels.Add(lot.LotId);
                
                // Box plot data: [min, Q1, median, Q3, max]
                boxplotData.Add(new[] 
                { 
                    lot.Stats.Min, 
                    lot.Stats.Q1, 
                    lot.Stats.Median, 
                    lot.Stats.Q3, 
                    lot.Stats.Max 
                });
                
                // Individual wafer points for scatter overlay
                foreach (var wafer in lot.Wafers)
                {
                    scatterData.Add(new { x = index, y = wafer.Yield, wafer = wafer.WaferId, lot = lot.LotId });
                }
                
                index++;
            }
        }
        
        return new
        {
            metadata = data.Metadata,
            yearLabels,
            weekLabels,
            lotLabels,
            boxplotData,
            scatterData
        };
    }
    
    public async Task EnableRectangularSelection(Action<SelectionRange> onSelect)
    {
        _onSelectCallback = onSelect;
        await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.enableBrush", _dotNetRef);
    }
    
    [JSInvokable]
    public void OnBrushSelect(double minX, double maxX, double minY, double maxY, object[] selectedPoints)
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
            await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.clearBrush");
        }
    }
    
    public async Task Resize()
    {
        if (_initialized)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.resize");
        }
    }
    
    public async Task Destroy()
    {
        if (_initialized)
        {
            await _jsRuntime.InvokeVoidAsync("chartInterop.echarts.destroy");
            _initialized = false;
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await Destroy();
        _dotNetRef?.Dispose();
    }
}
