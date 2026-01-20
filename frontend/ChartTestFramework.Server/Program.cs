using ChartTestFramework.Shared.Adapters;
using ChartTestFramework.Server.Adapters;
using ChartTestFramework.Shared.Interfaces;
using ChartTestFramework.Shared.Services;
using ChartTestFramework.Server.Components;
using Microsoft.JSInterop;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HTTP client for API calls
builder.Services.AddHttpClient("ChartAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Register performance logging
builder.Services.AddSingleton<PerformanceLogger>();

// Register JS chart adapters
builder.Services.AddScoped<EChartsAdapter>();
builder.Services.AddScoped<SyncfusionAdapter>();

// Register GPU-accelerated adapters (WebGL)
builder.Services.AddScoped<EChartsGLAdapter>();
builder.Services.AddScoped<DeckGLAdapter>();

// Register .NET chart image generators (True SSR)
builder.Services.AddSingleton<ScottPlotGenerator>();
builder.Services.AddSingleton<OxyPlotGenerator>();
builder.Services.AddSingleton<SkiaSharpGenerator>();
builder.Services.AddScoped<EChartsSSRAdapter>();

// Factory for adapter selection - includes all available adapters
builder.Services.AddScoped<Func<string, IChartAdapter>>(sp => 
    libraryName => libraryName switch
    {
        // Browser JS rendering
        "ECharts" => sp.GetRequiredService<EChartsAdapter>(),
        "Syncfusion" => sp.GetRequiredService<SyncfusionAdapter>(),
        
        // GPU-accelerated (WebGL)
        "EChartsGL" => sp.GetRequiredService<EChartsGLAdapter>(),
        "DeckGL" => sp.GetRequiredService<DeckGLAdapter>(),
        
        // True SSR (Server-rendered images)
        // True SSR (Server-rendered images)
        "ScottPlot" => new ServerImageAdapter(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<ScottPlotGenerator>()
        ),
        "OxyPlot" => new ServerImageAdapter(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<OxyPlotGenerator>()
        ),
        "SkiaSharp" => new ServerImageAdapter(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<SkiaSharpGenerator>()
        ),
        "EChartsSSR" => sp.GetRequiredService<EChartsSSRAdapter>(),
        _ => throw new ArgumentException($"Unknown chart library: {libraryName}")
    });

// Register test runner
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<PerformanceLogger>();
    return new TestRunner(httpClientFactory.CreateClient("ChartAPI"), logger, "Server");
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
