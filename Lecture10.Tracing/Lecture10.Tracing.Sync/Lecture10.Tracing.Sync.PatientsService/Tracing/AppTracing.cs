using System.Diagnostics;

namespace Lecture10.Tracing.Sync.PatientsService.Tracing;

public static class AppTracing
{
    public static readonly ActivitySource Source = new("Lecture10.Tracing.Sync.PatientsService");
}
