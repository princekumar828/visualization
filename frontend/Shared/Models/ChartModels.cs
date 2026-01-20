using System.Text.Json.Serialization;

namespace ChartTestFramework.Shared.Models;

/// <summary>
/// Represents timing metrics for render operations
/// </summary>
public class RenderMetrics
{
    public double InitTimeMs { get; set; }
    public double DataBindingMs { get; set; }
    public double RenderCompleteMs { get; set; }
    public double TotalRenderMs { get; set; }
    public int DataPointCount { get; set; }
    public string ChartLibrary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a range selected by user
/// </summary>
public class SelectionRange
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    public List<WaferDataPoint> SelectedPoints { get; set; } = new();
}

/// <summary>
/// Individual wafer data point
/// </summary>
public class WaferDataPoint
{
    [JsonPropertyName("Wafer_id")]
    public string WaferId { get; set; } = string.Empty;
    
    [JsonPropertyName("Yield")]
    public double Yield { get; set; }
}

/// <summary>
/// Box plot statistics for a single lot
/// </summary>
public class BoxPlotStats
{
    [JsonPropertyName("min")]
    public double Min { get; set; }
    
    [JsonPropertyName("q1")]
    public double Q1 { get; set; }
    
    [JsonPropertyName("median")]
    public double Median { get; set; }
    
    [JsonPropertyName("q3")]
    public double Q3 { get; set; }
    
    [JsonPropertyName("max")]
    public double Max { get; set; }
    
    [JsonPropertyName("mean")]
    public double Mean { get; set; }
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Lot data including wafers and statistics
/// </summary>
public class LotData
{
    [JsonPropertyName("lot_id")]
    public string LotId { get; set; } = string.Empty;
    
    [JsonPropertyName("wafers")]
    public List<WaferDataPoint> Wafers { get; set; } = new();
    
    [JsonPropertyName("stats")]
    public BoxPlotStats Stats { get; set; } = new();
}

/// <summary>
/// Week data containing multiple lots
/// </summary>
public class WeekData
{
    [JsonPropertyName("week_no")]
    public int WeekNo { get; set; }
    
    [JsonPropertyName("lots")]
    public List<LotData> Lots { get; set; } = new();
}

/// <summary>
/// Box plot metadata
/// </summary>
public class BoxPlotMetadata
{
    [JsonPropertyName("year")]
    public int Year { get; set; }
    
    [JsonPropertyName("total_weeks")]
    public int TotalWeeks { get; set; }
    
    [JsonPropertyName("total_lots")]
    public int TotalLots { get; set; }
    
    [JsonPropertyName("wafers_per_lot")]
    public int WafersPerLot { get; set; }
    
    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }
}

/// <summary>
/// Complete box plot data response
/// </summary>
public class BoxPlotData
{
    [JsonPropertyName("metadata")]
    public BoxPlotMetadata Metadata { get; set; } = new();
    
    [JsonPropertyName("weeks")]
    public List<WeekData> Weeks { get; set; } = new();
}

/// <summary>
/// Server timing metrics from API response
/// </summary>
public class ServerTiming
{
    [JsonPropertyName("array_generation_ms")]
    public double ArrayGenerationMs { get; set; }
    
    [JsonPropertyName("yield_generation_ms")]
    public double YieldGenerationMs { get; set; }
    
    [JsonPropertyName("dataframe_creation_ms")]
    public double DataframeCreationMs { get; set; }
    
    [JsonPropertyName("boxplot_transformation_ms")]
    public double BoxplotTransformationMs { get; set; }
    
    [JsonPropertyName("endpoint_total_ms")]
    public double EndpointTotalMs { get; set; }
    
    [JsonPropertyName("total_data_points")]
    public int TotalDataPoints { get; set; }
}

/// <summary>
/// Complete API response with data and timing
/// </summary>
public class BoxPlotApiResponse
{
    [JsonPropertyName("data")]
    public BoxPlotData Data { get; set; } = new();
    
    [JsonPropertyName("timing")]
    public TimingWrapper Timing { get; set; } = new();
}

public class TimingWrapper
{
    [JsonPropertyName("server")]
    public ServerTiming Server { get; set; } = new();
}

/// <summary>
/// Complete test result combining server and client metrics
/// </summary>
public class TestResult
{
    public string TestId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ChartLibrary { get; set; } = string.Empty;
    public string RenderMode { get; set; } = string.Empty; // "Server" or "WASM"
    public int DataPoints { get; set; }
    public int Weeks { get; set; }
    public int LotsPerWeek { get; set; }
    public int WafersPerLot { get; set; }
    
    // Server metrics (detailed)
    public double ServerArrayGenerationMs { get; set; }
    public double ServerYieldGenerationMs { get; set; }
    public double ServerDataframeCreationMs { get; set; }
    public double ServerTransformationMs { get; set; }
    public double ServerTotalMs { get; set; }
    
    // Computed server data generation total
    public double ServerDataGenerationMs => ServerArrayGenerationMs + ServerYieldGenerationMs + ServerDataframeCreationMs;
    
    // Client metrics
    public double FetchTimeMs { get; set; }
    public double ParseTimeMs { get; set; }
    public double ChartInitMs { get; set; }
    public double DataBindingMs { get; set; }
    public double RenderCompleteMs { get; set; }
    public double TotalClientMs { get; set; }
    
    // Totals
    public double TotalEndToEndMs => ServerTotalMs + FetchTimeMs + ParseTimeMs + RenderCompleteMs;
}
