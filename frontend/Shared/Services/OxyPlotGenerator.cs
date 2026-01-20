using System.Diagnostics;
using ChartTestFramework.Shared.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// OxyPlot implementation for server-side chart rendering
/// Scientific charts with excellent export capabilities
/// </summary>
public class OxyPlotGenerator : IChartImageGenerator
{
    public string Name => "OxyPlot";
    
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
        var boxPlotItems = new List<BoxPlotItem>();
        var categories = new List<string>();
        int index = 0;
        
        foreach (var week in data.Weeks)
        {
            foreach (var lot in week.Lots)
            {
                var yields = lot.Wafers.Select(w => w.Yield).OrderBy(y => y).ToArray();
                if (yields.Length > 0)
                {
                    var q1 = Percentile(yields, 25);
                    var q3 = Percentile(yields, 75);
                    
                    boxPlotItems.Add(new BoxPlotItem(
                        index,
                        yields.Min(),
                        q1,
                        Percentile(yields, 50),
                        q3,
                        yields.Max()
                    ));
                    
                    categories.Add(lot.LotId);
                    index++;
                }
            }
        }
        extractSw.Stop();
        metrics.DataExtractionMs = extractSw.Elapsed.TotalMilliseconds;
        
        // Create chart
        var createSw = Stopwatch.StartNew();
        var model = new PlotModel
        {
            Title = $"Semiconductor Yield - Year {data.Metadata.Year}",
            Subtitle = $"{data.Metadata.TotalLots} Lots | {data.Metadata.TotalPoints:N0} Data Points",
            Background = OxyColor.FromRgb(26, 26, 62),
            PlotAreaBackground = OxyColor.FromRgb(37, 37, 96),
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            SubtitleColor = OxyColors.LightGray
        };
        
        // Add axes
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Lot ID",
            Key = "CategoryAxis",
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.White,
            TicklineColor = OxyColors.White
        };
        
        // Add only every Nth category for readability
        int step = Math.Max(1, categories.Count / 20);
        for (int i = 0; i < categories.Count; i += step)
        {
            categoryAxis.Labels.Add(categories[i]);
        }
        
        model.Axes.Add(categoryAxis);
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Yield (%)",
            Minimum = 70,
            Maximum = 100,
            TextColor = OxyColors.White,
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.White,
            TicklineColor = OxyColors.White,
            MajorGridlineColor = OxyColor.FromRgb(64, 64, 128),
            MajorGridlineStyle = LineStyle.Solid
        });
        
        // Add box plot series
        var boxSeries = new BoxPlotSeries
        {
            Fill = OxyColor.FromRgb(70, 130, 180),
            Stroke = OxyColors.Navy,
            StrokeThickness = 1,
            WhiskerWidth = 0.5
        };
        
        foreach (var item in boxPlotItems)
        {
            boxSeries.Items.Add(item);
        }
        
        model.Series.Add(boxSeries);
        createSw.Stop();
        metrics.ChartCreationMs = createSw.Elapsed.TotalMilliseconds;
        
        // Render to image
        var renderSw = Stopwatch.StartNew();
        byte[] imageBytes;
        
        using (var stream = new MemoryStream())
        {
            if (format.ToLower() == "svg")
            {
                var exporter = new OxyPlot.SkiaSharp.SvgExporter { Width = width, Height = height };
                exporter.Export(model, stream);
            }
            else
            {
                var exporter = new OxyPlot.SkiaSharp.PngExporter { Width = width, Height = height };
                exporter.Export(model, stream);
            }
            
            imageBytes = stream.ToArray();
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
