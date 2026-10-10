namespace Lecture10.Tracing.Sync.PaymentGateway.Api.Payments.Contract;

public class PaymentResponse
{
    public required Guid PaymentId { get; init; }
    public required string Status { get; init; }
}
