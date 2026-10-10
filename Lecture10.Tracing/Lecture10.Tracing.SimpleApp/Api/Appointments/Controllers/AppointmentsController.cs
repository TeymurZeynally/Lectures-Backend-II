using System.Diagnostics;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Services;
using Lecture10.Tracing.SimpleApp.Tracing;
using Microsoft.AspNetCore.Mvc;

namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> GetByDoctor(Guid doctorId, DateOnly date)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsController.GetByDoctor");

        _logger.LogInformation("Schedule requested for doctor {DoctorId} on {Date:yyyy-MM-dd}", doctorId, date);


        var day = await _service.GetDoctorDayAsync(doctorId, date);
        if (day is null)
        {
            _logger.LogWarning("Doctor {DoctorId} not found, responding with 404", doctorId);
            return NotFound();
        }

        _logger.LogInformation("Schedule of doctor {Doctor} for {Date:yyyy-MM-dd} sent: {Count} appointments", day.Doctor, date, day.Appointments.Count);

        return Ok(day);
    }
}
