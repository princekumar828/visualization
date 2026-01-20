using System.Diagnostics;
using ChartTestFramework.Shared.Models;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// Interface for server-side chart image generation
/// </summary>
public interface IChartImageGenerator
{
    string Name { get; }
    Task<(byte[] ImageBytes, ChartImageMetrics Metrics)> GenerateBoxPlotImage(
        BoxPlotData data,
        int width = 1200,
        int height = 600,
        string format = "png"
    );
}

/// <summary>
/// Metrics for chart image generation
/// </summary>
public class ChartImageMetrics
{
    public string Library { get; set; } = "";
    public double DataExtractionMs { get; set; }
    public double ChartCreationMs { get; set; }
    public double RenderingMs { get; set; }
    public double TotalMs { get; set; }
    public int ImageSizeBytes { get; set; }
    public int DataPoints { get; set; }
}
