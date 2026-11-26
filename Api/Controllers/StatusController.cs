using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly WhatsappDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StatusController(WhatsappDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatuses()
        {
            var now = DateTime.UtcNow;

            var statuses = await _context.Statuses
                .Where(s => s.ExpiresAt > now)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var grouped = statuses
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    userId = g.Key,
                    user = new
                    {
                        id = g.First().User.Id,
                        name = g.First().User.Name,
                        avatar = g.First().User.Avatar,
                        phone = g.First().User.Phone
                    },
                    stories = g.Select(s => new
                    {
                        id = s.Id,
                        type = s.Type,
                        content = s.Content,
                        caption = s.Caption,
                        backgroundColor = s.BackgroundColor,
                        font = s.Font,
                        createdAt = s.CreatedAt,
                        expiresAt = s.ExpiresAt
                    }).ToList(),
                    latestUpdate = g.Max(s => s.CreatedAt)
                })
                .OrderByDescending(x => x.latestUpdate)
                .ToList();

            return Ok(new { success = true, statuses = grouped });
        }

        [HttpPost]
        public async Task<IActionResult> CreateStatus(
            [FromForm] string type,
            [FromForm] string? content,
            [FromForm] string? caption,
            [FromForm] string? backgroundColor,
            [FromForm] string? font,
            [FromForm] IFormFile? file,
            [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return BadRequest(new { success = false, message = "UserId is required." });
            }

            string finalContent = content ?? string.Empty;

            if (file != null && file.Length > 0)
            {
                var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var ext = Path.GetExtension(file.FileName);
                var fileName = $"status_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                finalContent = $"/uploads/{fileName}";
            }

            var status = new Status
            {
                Id = "st_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                UserId = currentUserId,
                Type = type?.ToUpper() ?? "TEXT",
                Content = finalContent,
                Caption = caption,
                BackgroundColor = backgroundColor ?? "#128c7e",
                Font = font ?? "sans-serif",
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _context.Statuses.Add(status);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, status });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatus(string id, [FromQuery] string? userId)
        {
            var currentUserId = userId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var status = await _context.Statuses.FindAsync(id);
            if (status == null) return NotFound();

            if (status.UserId != currentUserId) return Forbid();

            _context.Statuses.Remove(status);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
