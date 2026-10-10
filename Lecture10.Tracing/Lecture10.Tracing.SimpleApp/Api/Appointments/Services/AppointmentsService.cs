using System.Diagnostics;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Contract;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories.Entities;
using Lecture10.Tracing.SimpleApp.Tracing;

namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IAppointmentsRepository _repository;
    private readonly ILogger<AppointmentsService> _logger;

    public AppointmentsService(IAppointmentsRepository repository, ILogger<AppointmentsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DoctorDayResponse?> GetDoctorDayAsync(Guid doctorId, DateOnly date)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsService.GetDoctorDayAsync");

        var doctor = await CheckAccessAsync(doctorId);
        if (doctor is null)
        {
            return null;
        }

        var appointments = await _repository.FindByDoctorAndDateAsync(doctorId, date);

        return MapToResponse(doctor, date, appointments);
    }

    private async Task<Doctor?> CheckAccessAsync(Guid doctorId)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsService.CheckAccessAsync");

        var doctor = await _repository.GetDoctorAsync(doctorId);
        if (doctor is null)
        {
            _logger.LogWarning("Schedule access denied: doctor {DoctorId} is not in the directory", doctorId);
            return null;
        }

        _logger.LogInformation("Schedule access granted for doctor {Doctor} ({Specialization})", doctor.FullName, doctor.Specialization);

        return doctor;
    }

    private DoctorDayResponse MapToResponse(Doctor doctor, DateOnly date, List<Appointment> appointments)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsService.MapToResponse");

        Thread.Sleep(Random.Shared.Next(5, 25));

        _logger.LogInformation("Schedule of doctor {Doctor} for {Date:yyyy-MM-dd} built: {Count} appointments, first at {FirstTime:HH:mm}", doctor.FullName, date, appointments.Count, appointments.Min(a => a.StartsAt));

        return new DoctorDayResponse
        {
            Doctor = doctor.FullName,
            Specialization = doctor.Specialization,
            Date = date,
            Appointments = appointments
                .Select(a => new AppointmentResponse
                {
                    Id = a.Id,
                    Time = TimeOnly.FromDateTime(a.StartsAt),
                    Patient = a.PatientName,
                    Reason = a.Reason
                })
                .ToList()
        };
    }
}
