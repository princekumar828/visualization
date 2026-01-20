using System.Diagnostics;
using ChartTestFramework.Shared.Models;
using ScottPlot;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// ScottPlot implementation for server-side chart rendering
/// Fast, pure .NET, optimized for large datasets
/// </summary>
public class ScottPlotGenerator : IChartImageGenerator
{
    public string Name => "ScottPlot";
    
    public Task<(byte[] ImageBytes, ChartImageMetrics Metrics)> GenerateBoxPlotImage(
        BoxPlotData data,
        int width = 1200,
        int height = 600,
        string format = "png")
    {
        var metrics = new ChartImageMetrics
        {
            Library = Name,
            DataPoints = data.Metadata.TotalPoints
        };
        
        var totalSw = Stopwatch.StartNew();
        
        // Extract data
        var extractSw = Stopwatch.StartNew();
        var boxPlotItems = new List<ScottPlot.Box>();
        int boxIndex = 0;
        
        foreach (var week in data.Weeks)
        {
            foreach (var lot in week.Lots)
            {
                var yields = lot.Wafers.Select(w => w.Yield).OrderBy(y => y).ToArray();
                if (yields.Length > 0)
                {
                    boxPlotItems.Add(new ScottPlot.Box
                    {
                        Position = boxIndex++,
                        BoxMin = Percentile(yields, 25),
                        BoxMax = Percentile(yields, 75),
                        WhiskerMin = yields.Min(),
                        WhiskerMax = yields.Max(),
                        BoxMiddle = Percentile(yields, 50)
                    });
                }
            }
        }
        extractSw.Stop();
        metrics.DataExtractionMs = extractSw.Elapsed.TotalMilliseconds;
        
        // Create chart
        var createSw = Stopwatch.StartNew();
        var plt = new ScottPlot.Plot();
        plt.Title($"Semiconductor Yield - Year {data.Metadata.Year}");
        plt.XLabel("Lot Index");
        plt.YLabel("Yield (%)");
        
        // Add boxes
        var boxes = plt.Add.Boxes(boxPlotItems.ToArray());
        boxes.LineColor = ScottPlot.Colors.Navy;
        boxes.FillColor = ScottPlot.Colors.SteelBlue;
        
        // Set Y axis limits
        plt.Axes.SetLimitsY(70, 100);
        
        // Style for dark theme
        plt.FigureBackground.Color = ScottPlot.Color.FromHex("#1a1a3e");
        plt.DataBackground.Color = ScottPlot.Color.FromHex("#252560");
        plt.Axes.Color(ScottPlot.Colors.White);
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#404080");
        
        createSw.Stop();
        metrics.ChartCreationMs = createSw.Elapsed.TotalMilliseconds;
        
        // Render to image
        var renderSw = Stopwatch.StartNew();
        byte[] imageBytes;
        
        if (format.ToLower() == "svg")
        {
            var svg = plt.GetSvgXml(width, height);
            imageBytes = System.Text.Encoding.UTF8.GetBytes(svg);
        }
        else
        {
            imageBytes = plt.GetImageBytes(width, height, ScottPlot.ImageFormat.Png);
        }
        
        renderSw.Stop();
        metrics.RenderingMs = renderSw.Elapsed.TotalMilliseconds;
        
        totalSw.Stop();
        metrics.TotalMs = totalSw.Elapsed.TotalMilliseconds;
        metrics.ImageSizeBytes = imageBytes.Length;
        
        return Task.FromResult((imageBytes, metrics));
    }
    
    private static double Percentile(double[] sortedData, double percentile)
    {
        if (sortedData.Length == 0) return 0;
        if (sortedData.Length == 1) return sortedData[0];
        
        double n = (sortedData.Length - 1) * percentile / 100.0;
        int k = (int)n;
        double d = n - k;
        
        if (k >= sortedData.Length - 1) return sortedData[sortedData.Length - 1];
        return sortedData[k] + d * (sortedData[k + 1] - sortedData[k]);
    }
}
