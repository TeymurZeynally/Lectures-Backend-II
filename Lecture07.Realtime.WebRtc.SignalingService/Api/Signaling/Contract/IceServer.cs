namespace Lecture07.Realtime.WebRtc.SignalingService.Api.Signaling.Contract;

// Same shape as RTCIceServer in the browser, so the page passes it to RTCPeerConnection as is
public class IceServer
{
    public string Urls { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
