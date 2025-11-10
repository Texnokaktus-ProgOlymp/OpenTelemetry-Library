using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry;

namespace Texnokaktus.ProgOlymp.OpenTelemetry;

public static class DiExtensions
{
    private const string ServiceNamespace = "Texnokaktus.ProgOlymp";
    private static readonly string ServiceInstanceId = Guid.NewGuid().ToString();
    private static AssemblyName? AssemblyName => Assembly.GetEntryAssembly()?.GetName();
    private static string? ServiceVersion => AssemblyName?.Version?.ToString();

    public static IServiceCollection AddTexnokaktusOpenTelemetry(this IServiceCollection services,
                                                                 string? serviceName,
                                                                 Action<TracerProviderBuilder>? tracerProviderConfigurationAction,
                                                                 Action<MeterProviderBuilder>? meterProviderConfigurationAction)
    {
        services.AddOpenTelemetry()
                .ConfigureResource(resourceBuilder => resourceBuilder.AddService(serviceName ?? AssemblyName?.Name!,
                                                                                 serviceNamespace: ServiceNamespace,
                                                                                 serviceVersion: ServiceVersion,
                                                                                 serviceInstanceId: ServiceInstanceId))
                .WithTracing(tracerProviderBuilder => tracerProviderBuilder.AddAspNetCoreInstrumentation()
                                                                           .AddHttpClientInstrumentation()
                                                                           .AddRedisInstrumentation()
                                                                           .AddEntityFrameworkCoreInstrumentation()
                                                                           .AddSqlClientInstrumentation()
                                                                           .AddGrpcClientInstrumentation()
                                                                           .Apply(tracerProviderConfigurationAction))
                .WithMetrics(meterProviderBuilder => meterProviderBuilder.AddAspNetCoreInstrumentation()
                                                                         .AddHttpClientInstrumentation()
                                                                         .AddSqlClientInstrumentation()
                                                                         .Apply(meterProviderConfigurationAction))
                .UseOtlpExporter();

        return services;
    }

    public static LoggerConfiguration AddOpenTelemetrySupport(this LoggerConfiguration loggerConfiguration,
                                                              string? serviceName) =>
        loggerConfiguration.Enrich.WithOpenTelemetryTraceId()
                           .Enrich.WithOpenTelemetrySpanId()
                           .WriteTo.OpenTelemetry(options => options.ResourceAttributes
                                                                    .AddNotNullValue("service.name", serviceName ?? AssemblyName?.Name)
                                                                    .AddNotNullValue("service.namespace", ServiceNamespace)
                                                                    .AddNotNullValue("service.version", AssemblyName?.Version?.ToString())
                                                                    .AddNotNullValue("service.instance.id", ServiceInstanceId));

    private static TBuilder Apply<TBuilder>(this TBuilder builder, Action<TBuilder>? action)
    {
        action?.Invoke(builder);
        return builder;
    }

    private static IDictionary<string, object> AddNotNullValue(this IDictionary<string, object> dictionary, string key, object? value)
    {
        if (value is not null)
            dictionary.Add(key, value);

        return dictionary;
    }
}
