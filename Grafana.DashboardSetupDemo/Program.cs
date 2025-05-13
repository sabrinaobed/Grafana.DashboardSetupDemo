using OpenTelemetry.Resources; //for defining service name, environment, and version
using OpenTelemetry.Trace; //for enabling tracing
using OpenTelemetry.Metrics; //for enabling metrics
using OpenTelemetry.Logs; //for enabling logging
using Grafana.OpenTelemetry;
using Microsoft.Extensions.Http.Logging; //for intergrating directly with Grafana cloud





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


            //Add authorization header for Grafana OTLP endpoint
            string auth = instanceId + ":" + apiToken; //Combine instance Id and API token into a single string using the format : instanceId: apiToken
            string base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(auth)); //Convert that auth string onto Bae64- encoded string  used in HTTP Basic Authentication
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS","Authorization=Basic "+ base64Auth);//Set the OTEL_EXPORTER_OTLP_HEADERS environment variable to include the Authorization header with the base64-encoded auth string

            //Register OpenTelemetry services in dependency injection conatiner
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>

                { //enable automatic tracing of incominh HTTP requests
                    tracing
                    .UseGrafana() //Adds Grafana-specific configuration for tracing
                    .AddAspNetCoreInstrumentation() //Enabless ASP.NET Core instrumentation for tracing incoming HTTP requests
                    .AddHttpClientInstrumentation() //Enables automatic tracing of outgoing HTTP requests
                    .AddOtlpExporter(); //send trace data to OTLP endpoint
                })
                .WithMetrics(metrics =>
                {
                    //enable metics collection and export
                    metrics
                    .UseGrafana() //Adds Grafana-specific configuration for tracing
                    .AddAspNetCoreInstrumentation() //Enabless ASP.NET Core instrumentation for tracing incoming HTTP requests
                    .AddHttpClientInstrumentation() //Enables automatic tracing of outgoing HTTP requests
                    .AddPrometheusExporter() //Makes metrics available via HTTP for Prometheus scraping
                    .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel") //Common built in meters
                    .AddOtlpExporter(); //send trace data to OTLP endpoint in Grafana

                });

            //Send logs to Grafana using OpenTelemetry logging support
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging
                .UseGrafana() //Adds Grafana-specific configuration for log handling
                .AddOtlpExporter(); //Send log data to OTLP endpoint in Grafana
            });
                



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
