namespace Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients.Models;

public class PaymentResponseDto
{
    public required Guid PaymentId { get; init; }
    public required string Status { get; init; }
}
