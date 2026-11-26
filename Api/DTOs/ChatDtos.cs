using System;
using System.Collections.Generic;

namespace Api.DTOs
{
    public class CreateDirectChatRequest
    {
        public string RecipientId { get; set; } = string.Empty;
    }

    public class CreateGroupChatRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Avatar { get; set; }
        public List<string> MemberIds { get; set; } = new List<string>();
    }

    public class ChatDto
    {
        public string Id { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? GroupDescription { get; set; }
        public bool IsPinned { get; set; }
        public bool IsMuted { get; set; }
        public string Role { get; set; } = "MEMBER";
        public UserDto? OtherUser { get; set; }
        public List<UserDto> Members { get; set; } = new List<UserDto>();
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
