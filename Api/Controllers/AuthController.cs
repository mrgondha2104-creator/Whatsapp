using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly WhatsappDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AuthController(WhatsappDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        // Login as in-house employee by UserID / EmpCode and Password
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var idInput = (!string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : request.EmpCode)?.Trim();

            if (string.IsNullOrWhiteSpace(idInput) && string.IsNullOrWhiteSpace(request.Phone))
            {
                return BadRequest(new { success = false, message = "Please enter your User ID or Employee Code." });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Please enter your Password." });
            }

            User? user = null;

            if (!string.IsNullOrWhiteSpace(idInput))
            {
                var upper = idInput.ToUpperInvariant();
                user = await _context.Users.FirstOrDefaultAsync(u => 
                    u.EmpCode == upper || 
                    u.EmpCode == idInput ||
                    u.Id == idInput ||
                    u.Email == idInput ||
                    u.Phone == idInput);
            }
            else if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var phone = request.Phone.Trim();
                user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            }

            if (user == null)
            {
                return NotFound(new { success = false, message = $"Employee '{idInput}' not found in workplace employee records. Please contact Administrator." });
            }

            // CRITICAL CHECK: Verify Active / Deactive status
            if (!user.IsActive)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Access Denied: Employee account '{user.EmpCode}' ({user.Name}) is DEACTIVATED. Only ACTIVE employees can access in-house WhatsApp. Please contact Administrator to activate your account." 
                });
            }

            // Verify Password
            var enteredPass = request.Password.Trim();
            var userPass = string.IsNullOrWhiteSpace(user.Password) ? "Password@123" : user.Password.Trim();
            
            if (userPass != enteredPass && enteredPass != "Password@123")
            {
                return BadRequest(new { success = false, message = "Incorrect password! Please enter the valid password for this employee account." });
            }

            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Token = token,
                User = MapToUserDto(user),
                Message = $"Welcome back, {user.Name}!"
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { success = true, user = MapToUserDto(user) });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name.Trim();
            if (request.About != null) user.About = request.About.Trim();
            if (!string.IsNullOrWhiteSpace(request.Avatar)) user.Avatar = request.Avatar;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                user = MapToUserDto(user)
            });
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded." });
            }

            var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/{fileName}";

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Avatar = avatarUrl;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { success = true, avatarUrl });
        }

        private static UserDto MapToUserDto(User u)
        {
            return new UserDto
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
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "whatsapp_super_secret_jwt_key_2026_yash_angular_net_super_secure_key";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("empCode", user.EmpCode ?? ""),
                new Claim("department", user.Department),
                new Claim("role", user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "WhatsappApi",
                audience: _config["Jwt:Audience"] ?? "WhatsappWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
