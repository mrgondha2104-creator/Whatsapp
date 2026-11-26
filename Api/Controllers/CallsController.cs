using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallsController : ControllerBase
    {
        private readonly WhatsappDbContext _context;

        public CallsController(WhatsappDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCallLogs([FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return BadRequest();

            var logs = await _context.CallLogs
                .Where(c => c.CallerId == currentUserId || c.ReceiverId == currentUserId)
                .Include(c => c.Caller)
                .Include(c => c.Receiver)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    id = c.Id,
                    callerId = c.CallerId,
                    callerName = c.Caller.Name,
                    callerAvatar = c.Caller.Avatar,
                    receiverId = c.ReceiverId,
                    receiverName = c.Receiver.Name,
                    receiverAvatar = c.Receiver.Avatar,
                    type = c.Type,
                    status = c.Status,
                    duration = c.Duration,
                    isOutgoing = c.CallerId == currentUserId,
                    createdAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, callLogs = logs });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCallLog([FromBody] CallLog log, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(log.ReceiverId)) return BadRequest();

            log.Id = "call_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            log.CallerId = currentUserId ?? log.CallerId;
            log.CreatedAt = DateTime.UtcNow;

            _context.CallLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, callLog = log });
        }
    }
}
