using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace ObservabilityExample.Extensions
{
    public static class ObservabilityGlobals
    {
        // ActivitySource tanımı: Tracing için
        public static readonly ActivitySource GreeterActivitySource = new ActivitySource("ObservabilityExample.Greeter");

        // Meter ve Counter tanımı: Metrics için
        private static readonly Meter Meter = new Meter("ObservabilityExample", "1.0.0");
        public static readonly Counter<int> CountGreetings = Meter.CreateCounter<int>(
            "nested_greetings_count",
            description: "Counts the number of nested greetings sent");
    }
}
