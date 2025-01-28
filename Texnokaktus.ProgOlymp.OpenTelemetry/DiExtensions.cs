using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Texnokaktus.ProgOlymp.OpenTelemetry;

public static class DiExtensions
{
    public static IServiceCollection AddTexnokaktusOpenTelemetry(this IServiceCollection services,
                                                                 IConfiguration configuration,
                                                                 Action<TracerProviderBuilder>? tracerProviderConfigurationAction,
                                                                 Action<MeterProviderBuilder>? meterProviderConfigurationAction)
    {
        services.AddOpenTelemetry()
                .ConfigureResource(builder => builder.AddService(Process.GetCurrentProcess().ProcessName))
                .WithTracing(tracerProviderBuilder =>
                 {
                     tracerProviderBuilder.AddAspNetCoreInstrumentation()
                                          .AddHttpClientInstrumentation()
                                          .AddRedisInstrumentation()
                                          .AddSqlClientInstrumentation()
                                          .AddGrpcClientInstrumentation()
                                          .AddOtlpExporter(options =>
                                           {
                                               options.Endpoint =
                                                   new(configuration.GetConnectionString("OtlpReceiver")!);
                                               options.Protocol = OtlpExportProtocol.Grpc;
                                           });

                     tracerProviderConfigurationAction?.Invoke(tracerProviderBuilder);
                 })
                .WithMetrics(meterProviderBuilder =>
                 {
                     meterProviderBuilder.AddAspNetCoreInstrumentation()
                                         .AddSqlClientInstrumentation()
                                         .AddHttpClientInstrumentation()
                                         .AddPrometheusExporter();

                     meterProviderConfigurationAction?.Invoke(meterProviderBuilder);
                 });

        return services;
    }
}
