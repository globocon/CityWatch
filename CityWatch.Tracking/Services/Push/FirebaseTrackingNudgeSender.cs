using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services.Push
{
    /// <summary>
    /// FirebaseAdmin-backed sender. The service-account key lives in a file OUTSIDE the
    /// site folder and outside source control (Tracking:Fcm:ServiceAccountJsonPath);
    /// initialisation is lazy and failure-tolerant — a missing or bad key file makes
    /// IsConfigured false and never throws into a request path.
    /// </summary>
    public sealed class FirebaseTrackingNudgeSender : ITrackingNudgeSender
    {
        private readonly ILogger<FirebaseTrackingNudgeSender> _logger;
        private readonly Lazy<FirebaseApp?> _app;

        public FirebaseTrackingNudgeSender(TrackingOptions options, ILogger<FirebaseTrackingNudgeSender> logger)
        {
            _logger = logger;
            _app = new Lazy<FirebaseApp?>(() =>
            {
                try
                {
                    var path = options.Fcm.ServiceAccountJsonPath;
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    {
                        _logger.LogWarning("FCM service-account file not found at '{Path}'; push nudges disabled.", path);
                        return null;
                    }
                    return FirebaseApp.DefaultInstance
                        ?? FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(path) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Firebase initialisation failed; push nudges disabled.");
                    return null;
                }
            });
        }

        public bool IsConfigured => _app.Value != null;

        public async Task<NudgeSendStatus> SendNudgeAsync(string fcmToken, int unitId, string reason,
            string requestId, CancellationToken ct)
        {
            if (_app.Value == null)
                return NudgeSendStatus.Failed;

            try
            {
                /* DATA message, high priority: high priority is what buys the ~10 s Doze
                   execution window; a data payload keeps the wake silent — the device shows
                   nothing, it just takes a fix. TTL is short on purpose: a nudge delivered
                   an hour late would upload a "fresh" position nobody asked for anymore. */
                var message = new Message
                {
                    Token = fcmToken,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        TimeToLive = TimeSpan.FromMinutes(5)
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = "TrackingNudge",
                        ["reason"] = reason,
                        ["unitId"] = unitId.ToString(),
                        ["requestId"] = requestId
                    }
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
                _logger.LogInformation("TrackingNudgeSent unit {Unit} request {RequestId} reason {Reason}.",
                    unitId, requestId, reason);
                return NudgeSendStatus.Sent;
            }
            catch (FirebaseMessagingException ex) when (
                ex.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                _logger.LogInformation("TrackingNudge token invalid for unit {Unit} ({Code}).",
                    unitId, ex.MessagingErrorCode);
                return NudgeSendStatus.InvalidToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TrackingNudgeFailed unit {Unit} request {RequestId}.", unitId, requestId);
                return NudgeSendStatus.Failed;
            }
        }
    }
}
