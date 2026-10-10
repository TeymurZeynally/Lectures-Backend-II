using System.Diagnostics;
using Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories.Entities;
using Lecture10.Tracing.Sync.PatientsService.Tracing;

namespace Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories;

public class MedicalCardsRepository : IMedicalCardsRepository
{
    private static readonly List<Patient> Patients =
    [
        new(Guid.Parse("0a1b2c3d-1111-4a5b-8c9d-0e1f2a3b4c01"), "Барсик", "кот", "Иванова Елена"),
        new(Guid.Parse("0a1b2c3d-2222-4a5b-8c9d-0e1f2a3b4c02"), "Рекс", "собака", "Петров Сергей"),
        new(Guid.Parse("0a1b2c3d-3333-4a5b-8c9d-0e1f2a3b4c03"), "Мурка", "кошка", "Смирнова Ольга"),
        new(Guid.Parse("0a1b2c3d-4444-4a5b-8c9d-0e1f2a3b4c04"), "Арчи", "собака", "Кузнецов Андрей"),
        new(Guid.Parse("0a1b2c3d-5555-4a5b-8c9d-0e1f2a3b4c05"), "Кеша", "кролик", "Попова Наталья")
    ];

    private readonly ILogger<MedicalCardsRepository> _logger;

    public MedicalCardsRepository(ILogger<MedicalCardsRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Patient?> FindAsync(Guid patientId)
    {
        using var activity = AppTracing.Source.StartActivity("MedicalCardsRepository.FindAsync");

        using var query = AppTracing.Source.StartActivity("SELECT medical_cards", ActivityKind.Client)
            ?.SetTag("db.system.name", "postgresql")
            .SetTag("db.namespace", "patients-db")
            .SetTag("db.query.text", "SELECT patient_id, name, species, owner FROM medical_cards WHERE patient_id = $1");

        var elapsedMs = Random.Shared.Next(10, 35);
        await Task.Delay(elapsedMs);

        var patient = Patients.FirstOrDefault(p => p.Id == patientId);
        query?.SetTag("db.response.returned_rows", patient is null ? 0 : 1);

        _logger.LogInformation("SELECT medical_cards completed in {ElapsedMs} ms, rows: {Rows}", elapsedMs, patient is null ? 0 : 1);

        return patient;
    }
}
