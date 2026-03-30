using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FitPlay.Api.Hubs;

[Authorize(Roles = "GymAdmin")]
public class GymPresenceHub : Hub
{
    /// <summary>
    /// Allows the client to join a group for receiving updates about a specific gym's presence data.
    /// The group name will be "gym-{gymId}".
    /// </summary>
    /// <param name="gymId">The ID of the gym to monitor</param>
    public async Task JoinGymGroup(int gymId)
    {
        var groupName = $"gym-{gymId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Allows the client to leave a gym's group.
    /// </summary>
    /// <param name="gymId">The ID of the gym to stop monitoring</param>
    public async Task LeaveGymGroup(int gymId)
    {
        var groupName = $"gym-{gymId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up is automatically handled by SignalR for groups
        await base.OnDisconnectedAsync(exception);
    }
}