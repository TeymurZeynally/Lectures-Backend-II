using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Contract;
using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Repositories;
using Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;
using Lecture10.Tracing.Sync.AppointmentsApi.External.HttpClients;
using Lecture10.Tracing.Sync.AppointmentsApi.Tracing;

namespace Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IScheduleClient _scheduleClient;
    private readonly IPatientsClient _patientsClient;
    private readonly IPaymentsClient _paymentsClient;
    private readonly IAppointmentsRepository _repository;
    private readonly ILogger<AppointmentsService> _logger;

    public AppointmentsService(IScheduleClient scheduleClient, IPatientsClient patientsClient, IPaymentsClient paymentsClient, IAppointmentsRepository repository, ILogger<AppointmentsService> logger)
    {
        _scheduleClient = scheduleClient;
        _patientsClient = patientsClient;
        _paymentsClient = paymentsClient;
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateAppointmentResult> CreateAsync(CreateAppointmentRequest request)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsService.CreateAsync");

        await Task.Delay(Random.Shared.Next(40, 60));

        var slotId = await _scheduleClient.ReserveSlotAsync(request.DoctorId, request.StartsAt);
        if (slotId is null)
        {
            _logger.LogWarning("Slot of doctor {DoctorId} at {StartsAt:yyyy-MM-dd HH:mm} is already taken", request.DoctorId, request.StartsAt);
            return new CreateAppointmentResult(CreateAppointmentStatus.SlotTaken);
        }

        var card = await _patientsClient.GetMedicalCardAsync(request.PatientId);
        if (card is null)
        {
            _logger.LogWarning("Patient {PatientId} has no medical card", request.PatientId);
            return new CreateAppointmentResult(CreateAppointmentStatus.PatientNotFound);
        }

        var appointmentId = Guid.NewGuid();

        var payment = await _paymentsClient.ChargeAsync(appointmentId, 1000);
        if (payment.Status != "approved")
        {
            _logger.LogWarning("Prepayment for appointment {AppointmentId} was {Status}", appointmentId, payment.Status);
            return new CreateAppointmentResult(CreateAppointmentStatus.PaymentDeclined);
        }

        await _repository.InsertAsync(appointmentId, request.DoctorId, request.PatientId, request.StartsAt);

        _logger.LogInformation("Appointment {AppointmentId} created: doctor {DoctorId}, patient {Patient}, {StartsAt:yyyy-MM-dd HH:mm}", appointmentId, request.DoctorId, card.Name, request.StartsAt);

        return new CreateAppointmentResult(CreateAppointmentStatus.Created, new AppointmentResponse
        {
            Id = appointmentId,
            DoctorId = request.DoctorId,
            Patient = $"{card.Name} ({card.Species})",
            Owner = card.Owner,
            StartsAt = request.StartsAt,
            SlotId = slotId.Value,
            PaymentId = payment.PaymentId
        });
    }
}
