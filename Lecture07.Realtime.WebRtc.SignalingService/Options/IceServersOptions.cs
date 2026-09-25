using Lecture07.Realtime.WebRtc.SignalingService.Api.Signaling.Contract;

namespace Lecture07.Realtime.WebRtc.SignalingService.Options;

public sealed class IceServersOptions
{
    public List<IceServer> Stun { get; init; } = new();

    public List<IceServer> Turn { get; init; } = new();
}
