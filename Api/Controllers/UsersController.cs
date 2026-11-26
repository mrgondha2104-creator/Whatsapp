using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly WhatsappDbContext _context;

        public UsersController(WhatsappDbContext context)
        {
            _context = context;
        }

        // Get all in-house employees except current user
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var users = await _context.Users
                .Where(u => u.Id != userId && u.IsActive)
                .OrderBy(u => u.Department)
                .ThenBy(u => u.Name)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    EmpCode = u.EmpCode,
                    Phone = u.Phone,
                    Name = u.Name,
                    Department = u.Department,
                    Designation = u.Designation,
                    Email = u.Email,
                    Role = u.Role,
                    CanSendFiles = u.CanSendFiles,
                    CanMakeCalls = u.CanMakeCalls,
                    CanCreateGroups = u.CanCreateGroups,
                    CanPostStatus = u.CanPostStatus,
                    CanDeleteMessages = u.CanDeleteMessages,
                    IsActive = u.IsActive,
                    Avatar = u.Avatar,
                    About = u.About,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .ToListAsync();

            return Ok(new { success = true, users });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new { success = true, users = Enumerable.Empty<UserDto>() });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var cleanQuery = query.Trim().ToLower();

            var users = await _context.Users
                .Where(u => u.Id != userId && u.IsActive && 
                            (u.Name.ToLower().Contains(cleanQuery) || 
                             (u.EmpCode != null && u.EmpCode.ToLower().Contains(cleanQuery)) ||
                             u.Department.ToLower().Contains(cleanQuery)))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    EmpCode = u.EmpCode,
                    Phone = u.Phone,
                    Name = u.Name,
                    Department = u.Department,
                    Designation = u.Designation,
                    Email = u.Email,
                    Role = u.Role,
                    CanSendFiles = u.CanSendFiles,
                    CanMakeCalls = u.CanMakeCalls,
                    CanCreateGroups = u.CanCreateGroups,
                    CanPostStatus = u.CanPostStatus,
                    CanDeleteMessages = u.CanDeleteMessages,
                    IsActive = u.IsActive,
                    Avatar = u.Avatar,
                    About = u.About,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .ToListAsync();

            return Ok(new { success = true, users });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive) return NotFound(new { success = false, message = "Employee not found." });

            return Ok(new
            {
                success = true,
                user = new UserDto
                {
                    Id = user.Id,
                    EmpCode = user.EmpCode,
                    Phone = user.Phone,
                    Name = user.Name,
                    Department = user.Department,
                    Designation = user.Designation,
                    Email = user.Email,
                    Role = user.Role,
                    CanSendFiles = user.CanSendFiles,
                    CanMakeCalls = user.CanMakeCalls,
                    CanCreateGroups = user.CanCreateGroups,
                    CanPostStatus = user.CanPostStatus,
                    CanDeleteMessages = user.CanDeleteMessages,
                    IsActive = user.IsActive,
                    Avatar = user.Avatar,
                    About = user.About,
                    IsOnline = user.IsOnline,
                    LastSeen = user.LastSeen
                }
            });
        }
    }
}
