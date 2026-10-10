using System.Diagnostics;

namespace Lecture10.Tracing.SimpleApp.Tracing;

public static class AppTracing
{
    public static readonly ActivitySource Source = new ActivitySource("Lecture10.Tracing.SimpleApp");
}
