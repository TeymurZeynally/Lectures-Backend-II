namespace Lecture10.Tracing.Sync.PaymentGateway.Api.Payments.Contract;

public class PaymentRequest
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
}
