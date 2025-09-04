using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Diagnostics;

namespace OrderService.Observability.Tracing;

/// <summary>
/// OpenTelemetry tracing setup and configuration
/// </summary>
public static class TracingSetup
{
    private static bool _tracingEnabled = true;

    /// <summary>
    /// Configure OpenTelemetry tracing services
    /// </summary>
    public static IServiceCollection AddDistributedTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        _tracingEnabled = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_TRACING") ?? "true") && environment != "Test";

        if (!_tracingEnabled)
        {
            TracingHelpers.SetTracingEnabled(false);
            return services;
        }

        var serviceInfo = TracingHelpers.GetServiceInfo(configuration);
        
        // Create activity source for the service
        var activitySource = new ActivitySource(serviceInfo.ServiceName, serviceInfo.ServiceVersion);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(
                    serviceName: serviceInfo.ServiceName,
                    serviceVersion: serviceInfo.ServiceVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceInfo.ServiceName)
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.method", request.Method);
                            activity.SetTag("http.request.path", request.Path);
                            activity.SetTag("correlation.id", request.HttpContext.Items["CorrelationId"]?.ToString() ?? "");
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.status_code", response.StatusCode);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.client.method", request.Method?.Method ?? "");
                            activity.SetTag("http.client.url", request.RequestUri?.ToString() ?? "");
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            activity.SetTag("http.client.status_code", (int)response.StatusCode);
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    });

                // Add OTLP exporter
                var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? 
                                  configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? 
                                  "http://localhost:4318";

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{otlpEndpoint}/v1/traces");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
                }

                // Console exporter is not available in OpenTelemetry .NET SDK
                // Traces will be exported via OTLP or other configured exporters
            });

        TracingHelpers.SetTracingEnabled(true);
        return services;
    }

    /// <summary>
    /// Check if tracing is enabled
    /// </summary>
    public static bool IsTracingEnabled() => _tracingEnabled;
}
