using System.Diagnostics;

namespace Lecture10.Tracing.Async.Notifications.Tracing;

public static class AppTracing
{
    public static readonly ActivitySource Source = new("Lecture10.Tracing.Async.Notifications");
}
