using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Tracking.Services.Push
{
    public enum NudgeSendStatus
    {
        Sent,

        /// <summary>FCM says this token is dead (uninstall/reinstall) — deactivate it.</summary>
        InvalidToken,

        /// <summary>Transient or configuration failure; the token stays active.</summary>
        Failed
    }

    /// <summary>
    /// Sends the TrackingNudge push. FCM IS THE ACCELERATOR, THE INGEST RESPONSE IS THE
    /// GUARANTEE: a successful send means "the message was accepted by FCM", never "the
    /// device woke" and never "a position was obtained". Success of a nudge is observed
    /// exclusively as a fresh position arriving on the ingest path.
    /// </summary>
    public interface ITrackingNudgeSender
    {
        /// <summary>False when no service-account key is configured — /ping refuses
        /// loudly instead of pretending to send (a button must never silently do nothing).</summary>
        bool IsConfigured { get; }

        Task<NudgeSendStatus> SendNudgeAsync(string fcmToken, int unitId, string reason,
            string requestId, CancellationToken ct);
    }

    /// <summary>Registered when Tracking:Fcm:ServiceAccountJsonPath is absent: push is off,
    /// everything else about tracking behaves identically (flag-off discipline, §3.2).</summary>
    public sealed class NullTrackingNudgeSender : ITrackingNudgeSender
    {
        public bool IsConfigured => false;

        public Task<NudgeSendStatus> SendNudgeAsync(string fcmToken, int unitId, string reason,
            string requestId, CancellationToken ct) => Task.FromResult(NudgeSendStatus.Failed);
    }
}
