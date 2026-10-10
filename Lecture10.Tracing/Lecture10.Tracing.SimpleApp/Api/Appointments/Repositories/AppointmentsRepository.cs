using System.Diagnostics;
using Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories.Entities;
using Lecture10.Tracing.SimpleApp.Tracing;

namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories;

public class AppointmentsRepository : IAppointmentsRepository
{
    private static readonly List<Doctor> Doctors =
    [
        new(Guid.Parse("51d0f3c4-8a2e-4b7f-9c1d-3e5a7b9f2c60"), "Соколова Анна Викторовна", "терапевт"),
        new(Guid.Parse("7e2b9c41-5d3a-4f68-a1b2-c4d5e6f70812"), "Морозов Илья Сергеевич", "хирург"),
        new(Guid.Parse("a3c58e17-2b4d-4c9a-8f61-0d2e3b4c5a69"), "Лебедева Ольга Павловна", "дерматолог"),
        new(Guid.Parse("c9f14d26-7e8b-4a35-b0c2-6d1e9f8a7b43"), "Кузнецов Дмитрий Андреевич", "кардиолог"),
        new(Guid.Parse("e4a7b392-1c6f-4d8e-9a05-b3c2d1e0f987"), "Новикова Мария Игоревна", "офтальмолог")
    ];

    private static readonly string[] Patients =
    [
        "Барсик (кот)", "Рекс (собака)", "Мурка (кошка)", "Граф (собака)", "Симба (кот)", "Луна (кошка)",
        "Арчи (собака)", "Тоша (попугай)", "Бублик (хомяк)", "Ника (собака)", "Кеша (кролик)", "Джесси (собака)"
    ];

    private static readonly string[] Reasons =
    [
        "Плановая вакцинация", "Первичный осмотр", "Повторный приём", "Обработка от паразитов",
        "Чистка зубов", "Снятие швов", "Анализ крови", "УЗИ брюшной полости", "Чипирование"
    ];

    private readonly ILogger<AppointmentsRepository> _logger;

    public AppointmentsRepository(ILogger<AppointmentsRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Doctor?> GetDoctorAsync(Guid doctorId)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsRepository.GetDoctorAsync");

        using var query = StartQuery("SELECT doctors", "SELECT id, full_name, specialization FROM doctors WHERE id = $1");

        var elapsedMs = Random.Shared.Next(5, 15);
        await Task.Delay(elapsedMs);

        var doctor = Doctors.FirstOrDefault(d => d.Id == doctorId);

        query?.SetTag("db.response.returned_rows", doctor is null ? 0 : 1);

        _logger.LogInformation("SELECT doctors completed in {ElapsedMs} ms, rows: {Rows}", elapsedMs, doctor is null ? 0 : 1);

        return doctor;
    }

    public async Task<List<Appointment>> FindByDoctorAndDateAsync(Guid doctorId, DateOnly date)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsRepository.FindByDoctorAndDateAsync");

        using var query = StartQuery("SELECT appointments", "SELECT id, patient_id, starts_at FROM appointments WHERE doctor_id = $1 AND starts_at::date = $2");

        var elapsedMs = Random.Shared.NextDouble() < 0.05 ? Random.Shared.Next(1500, 1900) : Random.Shared.Next(20, 60);
        await Task.Delay(elapsedMs);

        if (Random.Shared.NextDouble() < 0.02)
        {
            var exception = new TimeoutException("Timeout during reading attempt");
            query?.AddException(exception);
            query?.SetStatus(ActivityStatusCode.Error, exception.Message);

            _logger.LogError(exception, "SELECT appointments failed after {ElapsedMs} ms: the database did not respond", elapsedMs);
            throw exception;
        }

        var appointments = Enumerable.Range(0, Random.Shared.Next(6, 16))
            .Select(i => new Appointment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Patients[Random.Shared.Next(Patients.Length)],
                date.ToDateTime(new TimeOnly(9, 0)).AddMinutes(30 * i),
                Reasons[Random.Shared.Next(Reasons.Length)]))
            .ToList();

        query?.SetTag("db.response.returned_rows", appointments.Count);

        if (elapsedMs > 1000)
        {
            _logger.LogWarning("Slow query: SELECT appointments took {ElapsedMs} ms, rows: {Rows}", elapsedMs, appointments.Count);
        }
        else
        {
            _logger.LogInformation("SELECT appointments completed in {ElapsedMs} ms, rows: {Rows}", elapsedMs, appointments.Count);
        }

        return appointments;
    }

    private static Activity? StartQuery(string name, string text)
    {
        return AppTracing.Source.StartActivity(name, ActivityKind.Client)
            ?.SetTag("db.system.name", "postgresql")
            .SetTag("db.namespace", "clinic-db")
            .SetTag("db.query.text", text);
    }
}
