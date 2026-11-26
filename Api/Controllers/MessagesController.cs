using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Hubs;
using Api.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly WhatsappDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(WhatsappDbContext context, IWebHostEnvironment env, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _env = env;
            _hubContext = hubContext;
        }

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetChatMessages(string chatId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .Include(m => m.ReplyTo)
                    .ThenInclude(r => r!.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .Include(m => m.StarredBy)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var result = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name,
                SenderAvatar = m.Sender?.Avatar,
                Text = m.Text,
                Type = m.Type,
                FileUrl = m.FileUrl,
                FileName = m.FileName,
                FileSize = m.FileSize,
                FileMime = m.FileMime,
                Duration = m.Duration,
                Status = m.Status,
                IsDeleted = m.IsDeleted,
                IsStarred = !string.IsNullOrEmpty(currentUserId) && m.StarredBy.Any(s => s.UserId == currentUserId),
                ReplyToId = m.ReplyToId,
                ReplyTo = m.ReplyTo != null ? new ReplyMessageDto
                {
                    Id = m.ReplyTo.Id,
                    Text = m.ReplyTo.Text,
                    Type = m.ReplyTo.Type,
                    FileName = m.ReplyTo.FileName,
                    SenderId = m.ReplyTo.SenderId,
                    SenderName = m.ReplyTo.Sender?.Name
                } : null,
                Reactions = m.Reactions.Where(r => r.User != null).Select(r => new ReactionDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    Emoji = r.Emoji
                }).ToList(),
                CreatedAt = m.CreatedAt
            });

            return Ok(new { success = true, messages = result });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(request.ChatId))
            {
                return BadRequest(new { success = false, message = "ChatId and Sender are required." });
            }

            var sender = await _context.Users.FindAsync(currentUserId);
            if (sender == null || !sender.IsActive)
            {
                return BadRequest(new { success = false, message = "Active employee account required." });
            }

            var msg = new Message
            {
                Id = "msg_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                ChatId = request.ChatId,
                SenderId = currentUserId,
                Text = request.Text,
                Type = request.Type ?? "TEXT",
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileSize = request.FileSize,
                FileMime = request.FileMime,
                Duration = request.Duration,
                ReplyToId = request.ReplyToId,
                Status = "SENT",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);

            var chat = await _context.Chats.FindAsync(request.ChatId);
            if (chat != null) chat.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var replyToMsg = !string.IsNullOrEmpty(request.ReplyToId) ? await _context.Messages.Include(x => x.Sender).FirstOrDefaultAsync(x => x.Id == request.ReplyToId) : null;

            var dto = new MessageDto
            {
                Id = msg.Id,
                ChatId = msg.ChatId,
                SenderId = msg.SenderId,
                SenderName = sender?.Name,
                SenderAvatar = sender?.Avatar,
                Text = msg.Text,
                Type = msg.Type,
                FileUrl = msg.FileUrl,
                FileName = msg.FileName,
                FileSize = msg.FileSize,
                FileMime = msg.FileMime,
                Duration = msg.Duration,
                Status = msg.Status,
                IsDeleted = false,
                IsStarred = false,
                ReplyToId = msg.ReplyToId,
                ReplyTo = replyToMsg != null ? new ReplyMessageDto
                {
                    Id = replyToMsg.Id,
                    Text = replyToMsg.Text,
                    Type = replyToMsg.Type,
                    FileName = replyToMsg.FileName,
                    SenderId = replyToMsg.SenderId,
                    SenderName = replyToMsg.Sender?.Name
                } : null,
                Reactions = new List<ReactionDto>(),
                CreatedAt = msg.CreatedAt
            };

            await _hubContext.Clients.Group($"chat_{msg.ChatId}").SendAsync("ReceiveMessage", dto);

            var memberUserIds = await _context.ChatMembers
                .Where(cm => cm.ChatId == msg.ChatId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            foreach (var memberId in memberUserIds)
            {
                await _hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveMessage", dto);
            }

            return Ok(new { success = true, message = dto });
        }

        // Upload any file: audio, video, photos, zip, bin, exe, apk, docx, pdf, etc.
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file,
            [FromForm] string chatId,
            [FromForm] string? text,
            [FromForm] string? customType,
            [FromForm] double? duration,
            [FromForm] string? replyToId,
            [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (file == null || file.Length == 0 || string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(currentUserId))
            {
                return BadRequest(new { success = false, message = "File, chatId, and sender are required." });
            }

            var sender = await _context.Users.FindAsync(currentUserId);
            if (sender == null || !sender.IsActive)
            {
                return BadRequest(new { success = false, message = "Active employee account required." });
            }

            // Check employee permission to send files
            if (!sender.CanSendFiles)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Your employee account does not have permission to send files." });
            }

            var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var rawName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = System.Text.RegularExpressions.Regex.Replace(rawName, @"[^a-zA-Z0-9_\-]", "_");
            var uniqueFileName = $"{safeName}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{uniqueFileName}";

            string msgType = "DOCUMENT";
            if (!string.IsNullOrEmpty(customType))
            {
                msgType = customType.ToUpper();
            }
            else
            {
                var mime = (file.ContentType ?? string.Empty).ToLower();
                if (mime.StartsWith("image/")) msgType = "IMAGE";
                else if (mime.StartsWith("video/")) msgType = "VIDEO";
                else if (mime.StartsWith("audio/") || ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".m4a" || ext == ".webm") msgType = "AUDIO";
            }

            var msg = new Message
            {
                Id = "msg_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                ChatId = chatId,
                SenderId = currentUserId,
                Text = text,
                Type = msgType,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                FileMime = file.ContentType,
                Duration = duration,
                ReplyToId = replyToId,
                Status = "SENT",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);

            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null) chat.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new MessageDto
            {
                Id = msg.Id,
                ChatId = msg.ChatId,
                SenderId = msg.SenderId,
                SenderName = sender.Name,
                SenderAvatar = sender.Avatar,
                Text = msg.Text,
                Type = msg.Type,
                FileUrl = msg.FileUrl,
                FileName = msg.FileName,
                FileSize = msg.FileSize,
                FileMime = msg.FileMime,
                Duration = msg.Duration,
                Status = msg.Status,
                CreatedAt = msg.CreatedAt
            };

            await _hubContext.Clients.Group($"chat_{msg.ChatId}").SendAsync("ReceiveMessage", dto);

            var memberUserIds = await _context.ChatMembers
                .Where(cm => cm.ChatId == msg.ChatId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            foreach (var memberId in memberUserIds)
            {
                await _hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveMessage", dto);
            }

            return Ok(new { success = true, message = dto });
        }

        [HttpPost("{messageId}/react")]
        public async Task<IActionResult> ReactToMessage(string messageId, [FromBody] ReactMessageRequest request, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(request.Emoji))
            {
                return BadRequest(new { success = false, message = "Emoji and User are required." });
            }

            var msg = await _context.Messages.FindAsync(messageId);
            if (msg == null) return NotFound();

            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == currentUserId);

            string action = "added";
            ReactionDto? reactionDto = null;

            if (existingReaction != null)
            {
                if (existingReaction.Emoji == request.Emoji)
                {
                    _context.MessageReactions.Remove(existingReaction);
                    action = "removed";
                }
                else
                {
                    existingReaction.Emoji = request.Emoji;
                    action = "updated";
                    reactionDto = new ReactionDto
                    {
                        Id = existingReaction.Id,
                        UserId = currentUserId,
                        UserName = (await _context.Users.FindAsync(currentUserId))?.Name ?? "User",
                        Emoji = existingReaction.Emoji
                    };
                }
            }
            else
            {
                var newReaction = new MessageReaction
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MessageId = messageId,
                    UserId = currentUserId,
                    Emoji = request.Emoji,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MessageReactions.Add(newReaction);
                reactionDto = new ReactionDto
                {
                    Id = newReaction.Id,
                    UserId = currentUserId,
                    UserName = (await _context.Users.FindAsync(currentUserId))?.Name ?? "User",
                    Emoji = newReaction.Emoji
                };
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"chat_{msg.ChatId}").SendAsync("ReactionReceived", new
            {
                chatId = msg.ChatId,
                messageId,
                reaction = reactionDto,
                action,
                userId = currentUserId,
                emoji = request.Emoji
            });

            return Ok(new { success = true, action, reaction = reactionDto });
        }

        [HttpPost("{messageId}/star")]
        public async Task<IActionResult> ToggleStar(string messageId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return BadRequest();

            var existing = await _context.StarredMessages
                .FirstOrDefaultAsync(s => s.MessageId == messageId && s.UserId == currentUserId);

            bool isStarred;
            if (existing != null)
            {
                _context.StarredMessages.Remove(existing);
                isStarred = false;
            }
            else
            {
                _context.StarredMessages.Add(new StarredMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserId = currentUserId,
                    MessageId = messageId,
                    CreatedAt = DateTime.UtcNow
                });
                isStarred = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, isStarred, messageId });
        }

        [HttpGet("starred")]
        public async Task<IActionResult> GetStarredMessages([FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return BadRequest();

            var starred = await _context.StarredMessages
                .Where(s => s.UserId == currentUserId)
                .Include(s => s.Message)
                    .ThenInclude(m => m.Sender)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new MessageDto
                {
                    Id = s.Message.Id,
                    ChatId = s.Message.ChatId,
                    SenderId = s.Message.SenderId,
                    SenderName = s.Message.Sender != null ? s.Message.Sender.Name : null,
                    SenderAvatar = s.Message.Sender != null ? s.Message.Sender.Avatar : null,
                    Text = s.Message.Text,
                    Type = s.Message.Type,
                    FileUrl = s.Message.FileUrl,
                    FileName = s.Message.FileName,
                    FileSize = s.Message.FileSize,
                    Status = s.Message.Status,
                    IsStarred = true,
                    CreatedAt = s.Message.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, starredMessages = starred });
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(string messageId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return NotFound();

            var sender = await _context.Users.FindAsync(currentUserId);
            if (message.SenderId != currentUserId && sender?.Role != "Admin")
            {
                return Forbid();
            }

            if (sender != null && !sender.CanDeleteMessages)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Permission to delete messages denied." });
            }

            message.IsDeleted = true;
            message.Text = "This message was deleted";
            message.FileUrl = null;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"chat_{message.ChatId}").SendAsync("MessageDeleted", new { messageId, chatId = message.ChatId });

            return Ok(new { success = true, messageId });
        }

        [HttpPut("{chatId}/read")]
        public async Task<IActionResult> MarkAsRead(string chatId, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return BadRequest();

            var unreadMessages = await _context.Messages
                .Where(m => m.ChatId == chatId && m.SenderId != currentUserId && m.Status != "READ")
                .ToListAsync();

            foreach (var m in unreadMessages)
            {
                m.Status = "READ";
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"chat_{chatId}").SendAsync("MessagesMarkedRead", new { chatId, userId = currentUserId });

            return Ok(new { success = true, count = unreadMessages.Count });
        }

        [HttpPost("forward")]
        public async Task<IActionResult> ForwardMessages([FromBody] ForwardMessagesRequest request, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || request.MessageIds.Count == 0 || request.TargetChatIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "Messages and target chats are required." });
            }

            var sender = await _context.Users.FindAsync(currentUserId);
            if (sender == null || !sender.IsActive)
            {
                return BadRequest(new { success = false, message = "Active employee account required." });
            }

            var sourceMessages = await _context.Messages
                .Where(m => request.MessageIds.Contains(m.Id) && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (sourceMessages.Count == 0)
            {
                return NotFound(new { success = false, message = "No valid messages found to forward." });
            }

            var createdDtos = new List<MessageDto>();
            var baseTime = DateTime.UtcNow;
            int offsetMs = 0;

            foreach (var targetChatId in request.TargetChatIds.Distinct())
            {
                var targetChat = await _context.Chats.FindAsync(targetChatId);
                if (targetChat == null) continue;

                var memberUserIds = await _context.ChatMembers
                    .Where(cm => cm.ChatId == targetChatId)
                    .Select(cm => cm.UserId)
                    .ToListAsync();

                foreach (var src in sourceMessages)
                {
                    var timestamp = baseTime.AddMilliseconds(offsetMs++);
                    var newMsg = new Message
                    {
                        Id = "msg_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                        ChatId = targetChatId,
                        SenderId = currentUserId,
                        Text = src.Text,
                        Type = src.Type,
                        FileUrl = src.FileUrl,
                        FileName = src.FileName,
                        FileSize = src.FileSize,
                        FileMime = src.FileMime,
                        Duration = src.Duration,
                        Status = "SENT",
                        CreatedAt = timestamp,
                        UpdatedAt = timestamp
                    };

                    _context.Messages.Add(newMsg);
                    targetChat.UpdatedAt = timestamp;

                    var dto = new MessageDto
                    {
                        Id = newMsg.Id,
                        ChatId = newMsg.ChatId,
                        SenderId = newMsg.SenderId,
                        SenderName = sender.Name,
                        SenderAvatar = sender.Avatar,
                        Text = newMsg.Text,
                        Type = newMsg.Type,
                        FileUrl = newMsg.FileUrl,
                        FileName = newMsg.FileName,
                        FileSize = newMsg.FileSize,
                        FileMime = newMsg.FileMime,
                        Duration = newMsg.Duration,
                        Status = newMsg.Status,
                        IsDeleted = false,
                        IsStarred = false,
                        Reactions = new List<ReactionDto>(),
                        CreatedAt = newMsg.CreatedAt
                    };

                    createdDtos.Add(dto);
                }

                await _context.SaveChangesAsync();

                // Broadcast newly forwarded messages to the chat and its members
                foreach (var dto in createdDtos.Where(d => d.ChatId == targetChatId))
                {
                    await _hubContext.Clients.Group($"chat_{targetChatId}").SendAsync("ReceiveMessage", dto);
                    foreach (var memberId in memberUserIds)
                    {
                        await _hubContext.Clients.Group($"user_{memberId}").SendAsync("ReceiveMessage", dto);
                    }
                }
            }

            return Ok(new { success = true, messages = createdDtos });
        }
    }
}
