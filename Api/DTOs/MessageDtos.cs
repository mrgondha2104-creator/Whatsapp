using System;
using System.Collections.Generic;

namespace Api.DTOs
{
    public class SendMessageRequest
    {
        public string ChatId { get; set; } = string.Empty;
        public string? Text { get; set; }
        public string Type { get; set; } = "TEXT";
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? FileMime { get; set; }
        public double? Duration { get; set; }
        public string? ReplyToId { get; set; }
    }

    public class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public string? Text { get; set; }
        public string Type { get; set; } = "TEXT";
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? FileMime { get; set; }
        public double? Duration { get; set; }
        public string Status { get; set; } = "SENT";
        public bool IsDeleted { get; set; }
        public bool IsStarred { get; set; }
        public string? ReplyToId { get; set; }
        public ReplyMessageDto? ReplyTo { get; set; }
        public List<ReactionDto> Reactions { get; set; } = new List<ReactionDto>();
        public DateTime CreatedAt { get; set; }
    }

    public class ReplyMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Text { get; set; }
        public string Type { get; set; } = "TEXT";
        public string? FileName { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? SenderName { get; set; }
    }

    public class ReactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
    }

    public class ReactMessageRequest
    {
        public string Emoji { get; set; } = string.Empty;
    }

    public class ForwardMessagesRequest
    {
        public List<string> MessageIds { get; set; } = new List<string>();
        public List<string> TargetChatIds { get; set; } = new List<string>();
    }
}
