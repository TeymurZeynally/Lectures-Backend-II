using System.Diagnostics;
using Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients.Models;
using Lecture10.Tracing.Sync.AppointmentsApi.Tracing;

namespace Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients;

public class PaymentsClient : IPaymentsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentsClient> _logger;

    public PaymentsClient(HttpClient httpClient, ILogger<PaymentsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> ChargeAsync(Guid orderId, decimal amount)
    {
        using var activity = AppTracing.Source.StartActivity("PaymentsClient.ChargeAsync");

        _logger.LogInformation("Prepayment of {Amount} for appointment {AppointmentId}: request to the payment gateway", amount, orderId);

        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient.PostAsJsonAsync("payments", new PaymentRequestDto { OrderId = orderId, Amount = amount });
        response.EnsureSuccessStatusCode();
        var payment = (await response.Content.ReadFromJsonAsync<PaymentResponseDto>())!;

        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            _logger.LogWarning("Payment gateway responded in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        }

        return payment;
    }
}
