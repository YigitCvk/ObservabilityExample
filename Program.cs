using Microsoft.AspNetCore.Mvc;
using ObservabilityExample.Extensions.Helper;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);
var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
builder.Services.AddSingleton<GreetingService>();

var serviceName = "ObservabilityExample"; // 📌 Servis Adını Burada Tanımla
var serviceVersion = "1.0.0";

// OpenTelemetry Kaynak Tanımlama
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);

// OpenTelemetry İzleme (Tracing) için ActivitySource oluştur
var activitySource = new ActivitySource(serviceName);

// OpenTelemetry Metrik Sistemi (Metrics)
var meter = new Meter(serviceName, serviceVersion);
var greetingsCounter = meter.CreateCounter<int>("greetings_count");

// 📌 **OpenTelemetry Konfigürasyonu**
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName)) // ✅ Servis adı buraya tanımlanmalı
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()  // 📌 HTTP isteklerini takip et
            .AddHttpClientInstrumentation() // 📌 Dış HTTP isteklerini takip et
            .AddSource(activitySource.Name) // 📌 Activity Source ekle
            .SetResourceBuilder(resourceBuilder)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://jaeger:4317"); // 📌 Jaeger gRPC bağlantısı
                opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            })
            .AddConsoleExporter();  // 📌 Terminale log yazdır
        if (tracingOtlpEndpoint != null)
        {
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
            });
        }
        else
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter(meter.Name)
            .AddPrometheusExporter(); // 📌 Prometheus Exporter
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 📌 **Prometheus için /metrics endpoint'ini doğru şekilde aç**
app.UseOpenTelemetryPrometheusScrapingEndpoint();
// Configure the Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();
app.MapGet("/hello", () =>
{
    using var activity = activitySource.StartActivity("HelloWorldActivity"); // 📌 **Jaeger Tracing Başlat**

    greetingsCounter.Add(1); // **Metriği artır**

    activity?.SetTag("customTag", "HelloWorld");
    activity?.SetTag("response", "Hello, World!"); // **Jaeger İçin Etiket Ekle**

    return "Hello, World!";
});
// Nested Greeting endpoint'i
app.MapGet("/nested-greeting", async (
    HttpContext context,
    [FromServices] ILogger<Program> logger,
    [FromServices] IHttpClientFactory clientFactory,
    [FromServices] GreetingService greetingService
    ) =>
{
    int nestlevel = 5;
    if (context.Request.Query.ContainsKey("nestlevel") &&
        int.TryParse(context.Request.Query["nestlevel"], out int level))
    {
        nestlevel = level;
    }
    await greetingService.SendNestedGreetingAsync(nestlevel, logger, context, clientFactory);
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
