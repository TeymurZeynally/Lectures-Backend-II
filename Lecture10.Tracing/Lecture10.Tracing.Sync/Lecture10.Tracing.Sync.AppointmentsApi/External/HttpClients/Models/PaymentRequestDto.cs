namespace Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients.Models;

public class PaymentRequestDto
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
}
