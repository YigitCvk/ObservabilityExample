using System.Diagnostics.Metrics;

namespace ObservabilityExample.Extensions.Helper
{
    public static class RegisterMetrics
    {
        // Meter'ı servis adı ve versiyon bilgisi
        private static readonly Meter Meter = new Meter("ObservabilityExample", "1.0.0");

        // WeatherForecast endpoint isteklerini sayacak bir counter oluşturuyoruz.
        public static readonly Counter<int> WeatherForecastRequests =
            Meter.CreateCounter<int>(
                "weatherforecast_requests",
                description: "The number of requests to the WeatherForecast endpoint");
    }
}
