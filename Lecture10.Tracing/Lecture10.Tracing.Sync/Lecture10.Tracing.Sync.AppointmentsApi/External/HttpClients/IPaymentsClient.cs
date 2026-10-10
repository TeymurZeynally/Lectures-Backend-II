using Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients.Models;

namespace Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients;

public interface IPaymentsClient
{
    Task<PaymentResponseDto> ChargeAsync(Guid orderId, decimal amount);
}
