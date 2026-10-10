namespace Lecture10.Tracing.Async.Notifications.External.SmsGateway;

public interface ISmsGatewayClient
{
    Task SendAsync(Guid patientId, string text);
}
