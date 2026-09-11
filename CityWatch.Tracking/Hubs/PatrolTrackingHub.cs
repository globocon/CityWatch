using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CityWatch.Tracking.Hubs
{
    /// <summary>
    /// Push channel for the control room (§10.2). Browsers join a group and receive one diff
    /// frame per second from <see cref="Hosted.BroadcastTicker"/> — never one message per
    /// position, and never <c>Clients.All</c> (the UpdateHub anti-pattern this hub exists to
    /// avoid repeating).
    ///
    /// Phase 1 scope note: the platform has no per-operator site scoping (§1.8 — the role
    /// model is a single IsAdmin flag, and every operator sees every site on today's map),
    /// so there is exactly one group. The ControlRoom entity partitions it in Phase 2; the
    /// group-key discipline is in place now precisely so that change is additive.
    ///
    /// [Authorize] is correct-by-design and works same-app with cookie auth. Cross-app
    /// connections (RadioCheck browser → Web hub) authenticate after the Phase 0 JWT
    /// migration; until then RadioCheck's map uses its own same-origin authenticated poll
    /// of /api/tracking/live (accepted technical debt, §13.1.1).
    /// </summary>
    [Authorize]
    public class PatrolTrackingHub : Hub
    {
        public const string OperatorsGroup = "tracking-operators";

        public async Task JoinControlRoom()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OperatorsGroup);
        }

        public async Task LeaveControlRoom()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OperatorsGroup);
        }
    }
}
