using OpenTelemetry.Resources; //for defining service name, environment, and version
using OpenTelemetry.Trace; //for enabling tracing
using OpenTelemetry.Metrics; //for enabling metrics
using OpenTelemetry.Logs; //for enabling logging
using Grafana.OpenTelemetry; //for intergrating directly with Grafana cloud





namespace Grafana.DashboardSetupDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            //Read OpenTelemetry settings from appsettings.json
            var config = builder.Configuration;

            var serviceName = config["OpenTelemetry:ServiceName"]; //	The name of your app (shown in Grafana)
            var oltpEndpoint = config["OpenTelemetry:OtlpEndpoint"]; //The URL Grafana gives you to receive telemetry
            var protocol = config["OpenTelemetry:Protocol"]; //	Protocol for sending data (usually http/protobuf)
            var apiToken = config["OpenTelemetry:ApiToken"]; //	Your secret key to authenticate to Grafana
            var instanceId = config["OpenTelemetry:InstanceId"]; //	Your unique Grafana Cloud instance ID
            var attributes = config["OpenTelemetry:Attributes"]; //	Any additional attributes you want to add to your telemetry,Extra info like deployment.environment=production

            //Set required envirnoment variables for OpenTelemetry and Grafana
            Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", attributes); //Set service metadata like envirnoment = production
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", serviceName); //Set the name of your app which which will appear in Grafana
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", oltpEndpoint); //Set the URL Grafana gives where telemetry data will be sent
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", protocol); //Set the communication protocol for sending data (usually http/protobuf)


            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

            app.Run();
        }
    }
}
