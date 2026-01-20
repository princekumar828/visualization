using System.Diagnostics;
using ChartTestFramework.Shared.Models;
using SkiaSharp;

namespace ChartTestFramework.Shared.Services;

/// <summary>
/// SkiaSharp implementation for custom server-side chart rendering
/// GPU-accelerated, full control over rendering
/// </summary>
public class SkiaSharpGenerator : IChartImageGenerator
{
    public string Name => "SkiaSharp";
    
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
        var boxPlots = new List<(double Min, double Q1, double Median, double Q3, double Max, string Label)>();
        
        foreach (var week in data.Weeks)
        {
            foreach (var lot in week.Lots)
            {
                var yields = lot.Wafers.Select(w => w.Yield).OrderBy(y => y).ToArray();
                if (yields.Length > 0)
                {
                    boxPlots.Add((
                        yields.Min(),
                        Percentile(yields, 25),
                        Percentile(yields, 50),
                        Percentile(yields, 75),
                        yields.Max(),
                        lot.LotId
                    ));
                }
            }
        }
        extractSw.Stop();
        metrics.DataExtractionMs = extractSw.Elapsed.TotalMilliseconds;
        
        // Create chart
        var createSw = Stopwatch.StartNew();
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        // Background
        canvas.Clear(new SKColor(26, 26, 62));
        
        // Chart area
        float margin = 80;
        float chartWidth = width - 2 * margin;
        float chartHeight = height - 2 * margin;
        float chartLeft = margin;
        float chartTop = margin;
        float chartBottom = height - margin;
        float chartRight = width - margin;
        
        // Draw chart background
        using var chartBgPaint = new SKPaint { Color = new SKColor(37, 37, 96) };
        canvas.DrawRect(chartLeft, chartTop, chartWidth, chartHeight, chartBgPaint);
        
        // Draw grid
        using var gridPaint = new SKPaint 
        { 
            Color = new SKColor(64, 64, 128), 
            StrokeWidth = 1,
            IsAntialias = true
        };
        
        for (int y = 70; y <= 100; y += 5)
        {
            float yPos = chartBottom - (y - 70) / 30f * chartHeight;
            canvas.DrawLine(chartLeft, yPos, chartRight, yPos, gridPaint);
        }
        
        // Draw Y axis labels
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 12,
            IsAntialias = true
        };
        
        for (int y = 70; y <= 100; y += 10)
        {
            float yPos = chartBottom - (y - 70) / 30f * chartHeight;
            canvas.DrawText($"{y}%", chartLeft - 35, yPos + 4, textPaint);
        }
        
        // Draw title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 18,
            IsAntialias = true,
            FakeBoldText = true
        };
        canvas.DrawText($"Semiconductor Yield - Year {data.Metadata.Year}", chartLeft, 35, titlePaint);
        
        // Draw subtitle
        using var subtitlePaint = new SKPaint
        {
            Color = new SKColor(180, 180, 180),
            TextSize = 12,
            IsAntialias = true
        };
        canvas.DrawText($"{data.Metadata.TotalLots} Lots | {data.Metadata.TotalPoints:N0} Data Points", chartLeft, 55, subtitlePaint);
        
        createSw.Stop();
        metrics.ChartCreationMs = createSw.Elapsed.TotalMilliseconds;
        
        // Render box plots
        var renderSw = Stopwatch.StartNew();
        
        if (boxPlots.Count > 0)
        {
            float boxWidth = Math.Max(2, chartWidth / boxPlots.Count * 0.7f);
            float gap = chartWidth / boxPlots.Count;
            
            using var boxFillPaint = new SKPaint
            {
                Color = new SKColor(70, 130, 180, 200),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            
            using var boxStrokePaint = new SKPaint
            {
                Color = new SKColor(0, 0, 128),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            
            using var whiskerPaint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                IsAntialias = true
            };
            
            for (int i = 0; i < boxPlots.Count; i++)
            {
                var box = boxPlots[i];
                float x = chartLeft + gap * i + gap / 2;
                
                // Convert yield values to Y coordinates
                float yMin = chartBottom - (float)(box.Min - 70) / 30f * chartHeight;
                float yQ1 = chartBottom - (float)(box.Q1 - 70) / 30f * chartHeight;
                float yMedian = chartBottom - (float)(box.Median - 70) / 30f * chartHeight;
                float yQ3 = chartBottom - (float)(box.Q3 - 70) / 30f * chartHeight;
                float yMax = chartBottom - (float)(box.Max - 70) / 30f * chartHeight;
                
                // Draw whiskers
                canvas.DrawLine(x, yMax, x, yQ3, whiskerPaint);
                canvas.DrawLine(x, yQ1, x, yMin, whiskerPaint);
                canvas.DrawLine(x - boxWidth / 4, yMax, x + boxWidth / 4, yMax, whiskerPaint);
                canvas.DrawLine(x - boxWidth / 4, yMin, x + boxWidth / 4, yMin, whiskerPaint);
                
                // Draw box
                var boxRect = new SKRect(x - boxWidth / 2, yQ3, x + boxWidth / 2, yQ1);
                canvas.DrawRect(boxRect, boxFillPaint);
                canvas.DrawRect(boxRect, boxStrokePaint);
                
                // Draw median line
                using var medianPaint = new SKPaint
                {
                    Color = SKColors.Orange,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawLine(x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, medianPaint);
            }
        }
        
        // Draw axes labels
        canvas.DrawText("Lot Index", width / 2 - 30, height - 20, textPaint);
        
        // Save to render
        canvas.Flush();
        
        byte[] imageBytes;
        using (var image = surface.Snapshot())
        {
            var encodeFormat = format.ToLower() == "jpeg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
            using var encoded = image.Encode(encodeFormat, 90);
            imageBytes = encoded.ToArray();
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
