using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry;

namespace Texnokaktus.ProgOlymp.OpenTelemetry;

public static class DiExtensions
{
    public static IServiceCollection AddTexnokaktusOpenTelemetry(this IServiceCollection services,
                                                                 IConfiguration configuration,
                                                                 string? serviceName,
                                                                 Action<TracerProviderBuilder>? tracerProviderConfigurationAction,
                                                                 Action<MeterProviderBuilder>? meterProviderConfigurationAction)
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName();

        services.AddOpenTelemetry()
                .ConfigureResource(resourceBuilder => resourceBuilder.AddService(serviceName ?? assemblyName?.Name!,
                                                                                 serviceNamespace: "Texnokaktus.ProgOlymp",
                                                                                 serviceVersion: assemblyName?.Version?.ToString()))
                .WithTracing(tracerProviderBuilder =>
                 {
                     tracerProviderBuilder.AddAspNetCoreInstrumentation()
                                          .AddHttpClientInstrumentation()
                                          .AddRedisInstrumentation()
                                          .AddSqlClientInstrumentation()
                                          .AddGrpcClientInstrumentation()
                                          .AddOtlpExporter(options => options.ConfigureOtlpExporter(configuration));

                     tracerProviderConfigurationAction?.Invoke(tracerProviderBuilder);
                 })
                .WithMetrics(meterProviderBuilder =>
                 {
                     meterProviderBuilder.AddAspNetCoreInstrumentation()
                                         .AddHttpClientInstrumentation()
                                         .AddSqlClientInstrumentation()
                                         .AddOtlpExporter(options => options.ConfigureOtlpExporter(configuration));

                     meterProviderConfigurationAction?.Invoke(meterProviderBuilder);
                 });

        return services;
    }

    public static LoggerConfiguration AddOpenTelemetrySupport(this LoggerConfiguration loggerConfiguration, IConfiguration configuration) =>
        loggerConfiguration.Enrich.WithOpenTelemetryTraceId()
                           .Enrich.WithOpenTelemetrySpanId()
                           .WriteTo.OpenTelemetry(options =>
                            {
                                options.Endpoint = configuration.GetOtlpEndpoint();
                                options.Protocol = OtlpProtocol.Grpc;
                            });
}

file static class ConfigurationExtensions
{
    public static void ConfigureOtlpExporter(this OtlpExporterOptions exporterOptions, IConfiguration configuration)
    {
        exporterOptions.Endpoint = new(GetOtlpEndpoint(configuration));
        exporterOptions.Protocol = OtlpExportProtocol.Grpc;
    }

    public static string GetOtlpEndpoint(this IConfiguration configuration) =>
        configuration.GetConnectionString("OtlpReceiver") ?? "http://otel-collector:4317";
}
