using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "ObservabilityExample";
var serviceVersion = "1.0.0";

// OpenTelemetry Kaynak Tanımlama
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

// Metrik ve İzleme için OpenTelemetry yapılandırması
var meter = new Meter(serviceName, serviceVersion);
var greetingsCounter = meter.CreateCounter<int>("greetings_count"); // ✅ ÖNEMLİ: Metrik Tanımı

var activitySource = new ActivitySource(serviceName);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddSource(activitySource.Name)
               .SetResourceBuilder(resourceBuilder)
               .AddConsoleExporter()
               .AddOtlpExporter(opt =>
               {
                   opt.Endpoint = new Uri("http://jaeger:4317");
               });
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddMeter(meter.Name) // 📌 **ÖNEMLİ**
               .AddPrometheusExporter(); // 📌 **Prometheus Exporter eklenmeli**
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// 📌 **Prometheus için /metrics endpoint'ini doğru şekilde aç**
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapGet("/hello", () =>
{
    using var activity = activitySource.StartActivity("HelloWorldActivity");

    greetingsCounter.Add(1); // ✅ **ÖNEMLİ: Metrik Değerini Artırıyoruz**

    activity?.SetTag("customTag", "HelloWorld");

    return "Hello, World!";
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
