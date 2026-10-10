using Lecture10.Tracing.Sync.PaymentGateway.Api.Payments.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Lecture10.Tracing.Sync.PaymentGateway.Api.Payments.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ILogger<PaymentsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Charge(PaymentRequest request)
    {
        _logger.LogInformation("Payment of {Amount} for order {OrderId} received, traceparent header: {Traceparent} (ignored, this gateway is not traced)", request.Amount, request.OrderId, Request.Headers.TraceParent.ToString());

        var elapsedMs = Random.Shared.NextDouble() < 0.1 ? Random.Shared.Next(2500, 3000) : Random.Shared.Next(150, 400);
        await Task.Delay(elapsedMs);

        var payment = new PaymentResponse { PaymentId = Guid.NewGuid(), Status = Random.Shared.NextDouble() < 0.03 ? "declined" : "approved" };

        _logger.LogInformation("Payment {PaymentId} {Status} in {ElapsedMs} ms", payment.PaymentId, payment.Status, elapsedMs);

        return Ok(payment);
    }
}
