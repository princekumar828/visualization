using ChartTestFramework.Shared.Models;

namespace ChartTestFramework.Shared.Interfaces;

/// <summary>
/// Interface for chart library adapters
/// Allows swapping chart implementations without changing core code
/// </summary>
public interface IChartAdapter : IAsyncDisposable
{
    /// <summary>
    /// Name of the chart library (e.g., "Apache ECharts", "Syncfusion")
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Render a box plot chart
    /// </summary>
    /// <param name="containerId">DOM element ID to render into</param>
    /// <param name="data">Box plot data from API</param>
    /// <returns>Render timing metrics</returns>
    Task<RenderMetrics> RenderBoxPlot(string containerId, BoxPlotData data);
    
    /// <summary>
    /// Enable rectangular selection tool
    /// </summary>
    /// <param name="onSelect">Callback when selection is made</param>
    Task EnableRectangularSelection(Action<SelectionRange> onSelect);
    
    /// <summary>
    /// Clear the current selection
    /// </summary>
    Task ClearSelection();
    
    /// <summary>
    /// Resize chart to fit container
    /// </summary>
    Task Resize();
    
    /// <summary>
    /// Destroy chart instance and cleanup
    /// </summary>
    Task Destroy();
}
