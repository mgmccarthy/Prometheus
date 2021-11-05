using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using Prometheus;

class PrometheusFeature : Feature
{
    static ILog log = LogManager.GetLogger(nameof(PrometheusFeature));

    Dictionary<string, string> nameMapping = new Dictionary<string, string>
    {
        // https://prometheus.io/docs/practices/naming/
        {"# of msgs successfully processed / sec", "nservicebus_success_total"},
        {"# of msgs pulled from the input queue /sec", "nservicebus_fetched_total"},
        {"# of msgs failures / sec", "nservicebus_failure_total"},
        {"Critical Time", "nservicebus_criticaltime_seconds"},
        {"Processing Time", "nservicebus_processingtime_seconds"},
        {"Retries", "nservicebus_retries_total"},
    };

    public PrometheusFeature()
    {
        Defaults(settings =>
        {
            metricsOptions = settings.EnableMetrics();
        });
        EnableByDefault();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        var logicalAddress = settings.LogicalAddress();
        var discriminator = logicalAddress.EndpointInstance.Discriminator ?? "none";
        var labelValues = new[]
        {
            settings.EndpointName(),
            Environment.MachineName,
            Dns.GetHostName(),
            discriminator,
            settings.LocalAddress()
        };

        metricsOptions.RegisterObservers(
            register: probeContext =>
            {
                RegisterProbes(probeContext, labelValues);
            });

        context.RegisterStartupTask(new MetricServerTask());
    }

    public void RegisterProbes(ProbeContext context, string[] labelValues)
    {
        foreach (var duration in context.Durations)
        {
            if (!nameMapping.ContainsKey(duration.Name))
            {
                log.WarnFormat("Unsupported duration probe {0}", duration.Name);
                continue;
            }
            var prometheusName = nameMapping[duration.Name];
            var summary = Metrics.CreateSummary(prometheusName, duration.Description,
            new SummaryConfiguration
            {
                Objectives = new[]
                             {
                                 new QuantileEpsilonPair(0.5, 0.05),
                                 new QuantileEpsilonPair(0.9, 0.01),
                                 new QuantileEpsilonPair(0.99, 0.001)
                             },
                LabelNames = Labels
            });
            duration.Register((ref DurationEvent @event) => summary.Labels(labelValues).Observe(@event.Duration.TotalSeconds));
        }

        foreach (var signal in context.Signals)
        {
            if (!nameMapping.ContainsKey(signal.Name))
            {
                log.WarnFormat("Unsupported signal probe {0}", signal.Name);
                continue;
            }
            var prometheusName = nameMapping[signal.Name];
            var counter = Metrics.CreateCounter(prometheusName, signal.Description, Labels);
            signal.Register((ref SignalEvent @event) => counter.Labels(labelValues).Inc());
        }
    }

    class MetricServerTask : FeatureStartupTask
    {
        MetricServer metricServer = new MetricServer(port: 3030);

        protected override Task OnStart(IMessageSession session)
        {
            metricServer.Start();
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session)
        {
            metricServer.Stop();
            return Task.CompletedTask;
        }
    }

    MetricsOptions metricsOptions;

    static string[] Labels =
    {
        "endpoint",
        "machinename",
        "hostname",
        "endpointdiscriminator",
        "endpointqueue"
    };
}