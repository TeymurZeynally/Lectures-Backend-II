using System.Diagnostics;
using Lecture10.Tracing.Async.Notifications.Tracing;
using Microsoft.Extensions.Logging;

namespace Lecture10.Tracing.Async.Notifications.External.SmsGateway;

public class SmsGatewayClient(ILogger<SmsGatewayClient> logger) : ISmsGatewayClient
{
    public async Task SendAsync(Guid patientId, string text)
    {
        using var activity = AppTracing.Source.StartActivity("SmsGatewayClient.SendAsync");

        using var request = AppTracing.Source.StartActivity("POST /sms", ActivityKind.Client)
            ?.SetTag("peer.service", "sms-gateway")
            .SetTag("http.request.method", "POST")
            .SetTag("url.full", "https://sms.example.com/sms")
            .SetTag("server.address", "sms.example.com");

        var elapsedMs = Random.Shared.NextDouble() < 0.03 ? Random.Shared.Next(1000, 1500) : Random.Shared.Next(50, 120);
        await Task.Delay(elapsedMs);

        if (Random.Shared.NextDouble() < 0.05)
        {
            request?.SetTag("http.response.status_code", 503).SetStatus(ActivityStatusCode.Error, "SMS gateway is unavailable");
            logger.LogError("SMS to patient {PatientId} not sent after {ElapsedMs} ms: SMS gateway responded 503", patientId, elapsedMs);
            return;
        }

        request?.SetTag("http.response.status_code", 200);
        logger.LogInformation("SMS to patient {PatientId} sent in {ElapsedMs} ms: {Text}", patientId, elapsedMs, text);
    }
}
