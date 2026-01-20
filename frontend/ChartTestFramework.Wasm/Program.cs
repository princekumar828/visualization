using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChartTestFramework.Wasm;
using ChartTestFramework.Shared.Adapters;
using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Services;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API calls
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8000";
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromMinutes(5)
});

// Register performance logging
builder.Services.AddSingleton<PerformanceLogger>();

// Register JS chart adapters
builder.Services.AddScoped<EChartsAdapter>();
builder.Services.AddScoped<SyncfusionAdapter>();

// Register GPU-accelerated adapters (WebGL)
builder.Services.AddScoped<EChartsGLAdapter>();
builder.Services.AddScoped<DeckGLAdapter>();

// Note: Server-side image generators (ScottPlot, OxyPlot, SkiaSharp) 
// are NOT available in WASM as they require server-side rendering.
// They use native libraries that don't run in the browser.

// Factory for adapter selection - Browser-compatible adapters only
builder.Services.AddScoped<Func<string, IChartAdapter>>(sp => 
    libraryName => libraryName switch
    {
        // Browser JS rendering
        "ECharts" => sp.GetRequiredService<EChartsAdapter>(),
        "Syncfusion" => sp.GetRequiredService<SyncfusionAdapter>(),
        
        // GPU-accelerated (WebGL)
        "EChartsGL" => sp.GetRequiredService<EChartsGLAdapter>(),
        "DeckGL" => sp.GetRequiredService<DeckGLAdapter>(),
        
        _ => throw new ArgumentException($"Unknown chart library: {libraryName}")
    });

// Register test runner
builder.Services.AddScoped(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<PerformanceLogger>();
    return new TestRunner(httpClient, logger, "WASM");
});

await builder.Build().RunAsync();
