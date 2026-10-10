using System.Diagnostics;

namespace Lecture10.Tracing.Sync.AppointmentsApi.Tracing;

public static class AppTracing
{
    public static readonly ActivitySource Source = new("Lecture10.Tracing.Sync.AppointmentsApi");
}
