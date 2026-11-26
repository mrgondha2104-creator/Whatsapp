using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatsController : ControllerBase
    {
        private readonly WhatsappDbContext _context;

        public ChatsController(WhatsappDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChats([FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return BadRequest(new { success = false, message = "UserId is required." });
            }

            // Automatically ensure a 1-on-1 direct chat exists with every active in-house colleague
            var activeColleagueIds = await _context.Users
                .Where(u => u.Id != currentUserId && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            var existingDirectOtherUserIds = await _context.ChatMembers
                .Where(cm => cm.UserId == currentUserId && !cm.Chat.IsGroup)
                .SelectMany(cm => cm.Chat.Members)
                .Where(m => m.UserId != currentUserId)
                .Select(m => m.UserId)
                .ToListAsync();

            var missingIds = activeColleagueIds.Except(existingDirectOtherUserIds).ToList();
            if (missingIds.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var missingId in missingIds)
                {
                    var newChatId = "chat_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                    var newChat = new Chat
                    {
                        Id = newChatId,
                        IsGroup = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.Chats.Add(newChat);
                    _context.ChatMembers.Add(new ChatMember { ChatId = newChatId, UserId = currentUserId, Role = "Member" });
                    _context.ChatMembers.Add(new ChatMember { ChatId = newChatId, UserId = missingId, Role = "Member" });
                }
                await _context.SaveChangesAsync();
            }

            var memberships = await _context.ChatMembers
                .Where(cm => cm.UserId == currentUserId)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Members)
                        .ThenInclude(m => m.User)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Messages)
                        .ThenInclude(msg => msg.Sender)
                .OrderByDescending(cm => cm.IsPinned)
                .ThenByDescending(cm => cm.Chat.UpdatedAt)
                .ToListAsync();

            var result = memberships
                .Where(m => m.Chat.IsGroup || m.Chat.Members.Any(cm => cm.UserId != currentUserId && cm.User != null && cm.User.IsActive))
                .Select(m =>
            {
                var chat = m.Chat;
                var lastMessage = chat.Messages.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                string? displayName = chat.Name;
                string? displayAvatar = chat.GroupAvatar;
                UserDto? otherUserDto = null;

                if (!chat.IsGroup)
                {
                    var otherMember = chat.Members.FirstOrDefault(x => x.UserId != currentUserId);
                    if (otherMember?.User != null)
                    {
                        displayName = otherMember.User.Name;
                        displayAvatar = otherMember.User.Avatar;
                        otherUserDto = new UserDto
                        {
                            Id = otherMember.User.Id,
                            EmpCode = otherMember.User.EmpCode,
                            Phone = otherMember.User.Phone,
                            Name = otherMember.User.Name,
                            Department = otherMember.User.Department,
                            Designation = otherMember.User.Designation,
                            Email = otherMember.User.Email,
                            Role = otherMember.User.Role,
                            CanSendFiles = otherMember.User.CanSendFiles,
                            CanMakeCalls = otherMember.User.CanMakeCalls,
                            CanCreateGroups = otherMember.User.CanCreateGroups,
                            CanPostStatus = otherMember.User.CanPostStatus,
                            CanDeleteMessages = otherMember.User.CanDeleteMessages,
                            IsActive = otherMember.User.IsActive,
                            Avatar = otherMember.User.Avatar,
                            About = otherMember.User.About,
                            IsOnline = otherMember.User.IsOnline,
                            LastSeen = otherMember.User.LastSeen
                        };
                    }
                }

                int unreadCount = chat.Messages.Count(msg => msg.SenderId != currentUserId && (msg.Status == null || msg.Status.ToUpper() != "READ"));

                return new ChatDto
                {
                    Id = chat.Id,
                    IsGroup = chat.IsGroup,
                    Name = displayName,
                    Avatar = displayAvatar,
                    GroupDescription = chat.GroupDescription,
                    IsPinned = m.IsPinned,
                    IsMuted = m.IsMuted,
                    Role = m.Role,
                    OtherUser = otherUserDto,
                    Members = chat.Members.Where(cm => cm.User != null).Select(cm => new UserDto
                    {
                        Id = cm.User.Id,
                        EmpCode = cm.User.EmpCode,
                        Phone = cm.User.Phone,
                        Name = cm.User.Name,
                        Department = cm.User.Department,
                        Designation = cm.User.Designation,
                        Email = cm.User.Email,
                        Role = cm.User.Role,
                        CanSendFiles = cm.User.CanSendFiles,
                        CanMakeCalls = cm.User.CanMakeCalls,
                        CanCreateGroups = cm.User.CanCreateGroups,
                        CanPostStatus = cm.User.CanPostStatus,
                        CanDeleteMessages = cm.User.CanDeleteMessages,
                        IsActive = cm.User.IsActive,
                        Avatar = cm.User.Avatar,
                        About = cm.User.About,
                        IsOnline = cm.User.IsOnline,
                        LastSeen = cm.User.LastSeen
                    }).ToList(),
                    LastMessage = lastMessage != null ? new MessageDto
                    {
                        Id = lastMessage.Id,
                        ChatId = lastMessage.ChatId,
                        SenderId = lastMessage.SenderId,
                        SenderName = lastMessage.Sender?.Name,
                        Text = lastMessage.Text,
                        Type = lastMessage.Type,
                        FileName = lastMessage.FileName,
                        FileUrl = lastMessage.FileUrl,
                        Status = lastMessage.Status,
                        CreatedAt = lastMessage.CreatedAt
                    } : null,
                    UnreadCount = unreadCount,
                    UpdatedAt = chat.UpdatedAt
                };
            });

            return Ok(new { success = true, chats = result });
        }

        [HttpPost("direct")]
        public async Task<IActionResult> GetOrCreateDirectChat([FromQuery] string? userId, [FromBody] CreateDirectChatRequest request)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(request.RecipientId))
            {
                return BadRequest(new { success = false, message = "Current user and recipient are required." });
            }

            var existingChatId = await _context.Chats
                .Where(c => !c.IsGroup &&
                            c.Members.Any(m => m.UserId == currentUserId) &&
                            c.Members.Any(m => m.UserId == request.RecipientId))
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(existingChatId))
            {
                return Ok(new { success = true, chatId = existingChatId, created = false });
            }

            var newChatId = "chat_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            var newChat = new Chat
            {
                Id = newChatId,
                IsGroup = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(newChat);

            _context.ChatMembers.AddRange(
                new ChatMember { Id = Guid.NewGuid().ToString("N"), ChatId = newChatId, UserId = currentUserId, Role = "MEMBER" },
                new ChatMember { Id = Guid.NewGuid().ToString("N"), ChatId = newChatId, UserId = request.RecipientId, Role = "MEMBER" }
            );

            await _context.SaveChangesAsync();

            return Ok(new { success = true, chatId = newChatId, created = true });
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupChat([FromQuery] string? userId, [FromBody] CreateGroupChatRequest request)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, message = "Group name and creator are required." });
            }

            // Check if creator has CanCreateGroups permission
            var creator = await _context.Users.FindAsync(currentUserId);
            if (creator != null && !creator.CanCreateGroups)
            {
                return BadRequest(new { success = false, message = "You do not have permission to create groups." });
            }

            var groupChatId = "chat_grp_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            var groupChat = new Chat
            {
                Id = groupChatId,
                IsGroup = true,
                Name = request.Name.Trim(),
                GroupDescription = request.Description,
                GroupAvatar = !string.IsNullOrWhiteSpace(request.Avatar) ? request.Avatar : $"https://api.dicebear.com/7.x/identicon/svg?seed={Uri.EscapeDataString(request.Name)}",
                CreatedById = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Chats.Add(groupChat);

            var membersList = request.MemberIds.Distinct().ToList();
            if (!membersList.Contains(currentUserId)) membersList.Add(currentUserId);

            foreach (var memId in membersList)
            {
                _context.ChatMembers.Add(new ChatMember
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ChatId = groupChatId,
                    UserId = memId,
                    Role = memId == currentUserId ? "ADMIN" : "MEMBER"
                });
            }

            _context.Messages.Add(new Message
            {
                Id = "msg_sys_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                ChatId = groupChatId,
                SenderId = currentUserId,
                Type = "SYSTEM",
                Text = $"Group \"{request.Name.Trim()}\" was created.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, chatId = groupChatId });
        }

        [HttpPut("{chatId}/pin")]
        public async Task<IActionResult> TogglePin(string chatId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var member = await _context.ChatMembers.FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == currentUserId);
            if (member == null) return NotFound();

            member.IsPinned = !member.IsPinned;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isPinned = member.IsPinned });
        }

        [HttpPut("{chatId}/mute")]
        public async Task<IActionResult> ToggleMute(string chatId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var member = await _context.ChatMembers.FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == currentUserId);
            if (member == null) return NotFound();

            member.IsMuted = !member.IsMuted;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isMuted = member.IsMuted });
        }
    }
}
