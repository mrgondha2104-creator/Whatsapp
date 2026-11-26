using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;

namespace Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly WhatsappDbContext _context;
        // Map userId to ConnectionIds
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        public ChatHub(WhatsappDbContext context)
        {
            _context = context;
        }

        public async Task JoinUser(string userId)
        {
            OnlineUsers[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Update user online in database
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await Clients.Others.SendAsync("UserPresenceChanged", new { userId, isOnline = true, lastSeen = user.LastSeen });
            }
        }

        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        public async Task LeaveChat(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        public async Task SendMessageToChat(string chatId, MessageDto message)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", message);
        }

        public async Task SendTyping(string chatId, string userId, string userName, bool isTyping)
        {
            await Clients.OthersInGroup($"chat_{chatId}").SendAsync("UserTyping", new { chatId, userId, userName, isTyping });
        }

        public async Task MarkAsRead(string chatId, string userId)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("MessagesMarkedRead", new { chatId, userId });
        }

        public async Task SendReaction(string chatId, string messageId, object reaction, string action)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("ReactionReceived", new { chatId, messageId, reaction, action });
        }

        // WebRTC Signaling for Video & Audio Calls
        public async Task CallUser(string targetUserId, string callerId, string callerName, string callerAvatar, string callType, string? chatId)
        {
            await Clients.Group($"user_{targetUserId}").SendAsync("IncomingCall", new
            {
                callerId,
                callerName,
                callerAvatar,
                callType, // "AUDIO" or "VIDEO"
                chatId
            });
        }

        public async Task AcceptCall(string callerUserId, string targetUserId)
        {
            await Clients.Group($"user_{callerUserId}").SendAsync("CallAccepted", new { targetUserId });
        }

        public async Task RejectCall(string callerUserId, string reason)
        {
            await Clients.Group($"user_{callerUserId}").SendAsync("CallRejected", new { reason });
        }

        public async Task SendSignal(string targetUserId, object signal)
        {
            await Clients.Group($"user_{targetUserId}").SendAsync("ReceiveSignal", new { signal, senderConnectionId = Context.ConnectionId });
        }

        public async Task EndCall(string targetUserId)
        {
            await Clients.Group($"user_{targetUserId}").SendAsync("CallEnded");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Find user id by connection id
            string? disconnectedUserId = null;
            foreach (var kvp in OnlineUsers)
            {
                if (kvp.Value == Context.ConnectionId)
                {
                    disconnectedUserId = kvp.Key;
                    OnlineUsers.TryRemove(kvp.Key, out _);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(disconnectedUserId))
            {
                var user = await _context.Users.FindAsync(disconnectedUserId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await Clients.Others.SendAsync("UserPresenceChanged", new { userId = disconnectedUserId, isOnline = false, lastSeen = user.LastSeen });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
