using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace ObservabilityExample.Extensions.Helper
{
    public class GreetingService
    {
        // Global tanımlamalarınızı veya sınıfa özel tanımlamaları burada yapabilirsiniz.
        private static readonly ActivitySource GreeterActivitySource = new ActivitySource("ObservabilityExample.Greeter");
        private static readonly Meter Meter = new Meter("ObservabilityExample", "1.0.0");
        private static readonly Counter<int> CountGreetings = Meter.CreateCounter<int>(
            "nested_greetings_count",
            description: "Counts the number of nested greetings sent");

        public async Task SendNestedGreetingAsync(int nestlevel, ILogger logger, HttpContext context, IHttpClientFactory clientFactory)
        {
            using var activity = GreeterActivitySource.StartActivity("GreeterActivity");

            if (nestlevel <= 5)
            {
                logger.LogInformation("Sending greeting, level {nestlevel}", nestlevel);
                CountGreetings.Add(1);
                activity?.SetTag("nest-level", nestlevel);

                await context.Response.WriteAsync($"Nested Greeting, level: {nestlevel}\r\n");

                if (nestlevel > 0)
                {
                    var request = context.Request;
                    var url = new Uri($"{request.Scheme}://{request.Host}{request.Path}?nestlevel={nestlevel - 1}");
                    var nestedResult = await clientFactory.CreateClient().GetStringAsync(url);
                    await context.Response.WriteAsync(nestedResult);
                }
            }
            else
            {
                logger.LogError("Greeting nest level {nestlevel} too high", nestlevel);
                await context.Response.WriteAsync("Nest level too high, max is 5");
            }
        }
    }
}

