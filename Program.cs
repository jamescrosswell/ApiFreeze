using System.Globalization;
using ApiFreeze;
using OpenTelemetry.Trace;
using Quartz;
using Sentry.AspNetCore;
using Sentry.OpenTelemetry;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Host
    .UseSerilog((_, loggerConfiguration) =>
    {
        loggerConfiguration.WriteTo
            .Sentry(o =>
            {
                o.InitializeSdk = false;
                o.MinimumEventLevel = LogEventLevel.Verbose;
                o.MinimumBreadcrumbLevel = LogEventLevel.Verbose;
                o.FormatProvider = CultureInfo.InvariantCulture;
            });
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSentry();
    });

builder.WebHost
    .UseSentry(options =>
    {
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.Dsn = "https://b887218a80114d26a9b1a51c5f88e0b4@o447951.ingest.us.sentry.io/6601807";

        options.SendDefaultPii = true;
        options.TracesSampleRate = 1.0f;
        // options.ProfilesSampleRate = 1.0f;

        // options.AddDiagnosticSourceIntegration();
        options.UseOpenTelemetry();

        options.ExperimentalMetrics = new ExperimentalMetricsOptions
        {
            EnableCodeLocations = true,
            CaptureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All
        };
        // options.AddIntegration(new ProfilingIntegration());
    });

builder.Services.AddQuartz(configurator =>
{
    var scheduler = Guid.NewGuid();
    configurator.SchedulerId = $"default-id-{scheduler}";
    configurator.SchedulerName = $"default-name-{scheduler}";
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.ConfigureOptions<ConfigureProcessOutboxJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}