using Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Contract;
using Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Services;
using Lecture10.Tracing.Async.AppointmentsApi.Tracing;
using Microsoft.AspNetCore.Mvc;

namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Controllers;

[ApiController]
[Route("appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _service;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentsService service, ILogger<AppointmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentRequest request)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsController.Create");

        _logger.LogInformation("Appointment requested: doctor {DoctorId}, patient {PatientId}, {StartsAt:yyyy-MM-dd HH:mm}", request.DoctorId, request.PatientId, request.StartsAt);

        var appointment = await _service.CreateAsync(request);

        return Created($"/appointments/{appointment.Id}", appointment);
    }
}
