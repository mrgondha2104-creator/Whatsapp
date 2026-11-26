using System;
using System.Linq;
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
    public class EmployeesController : ControllerBase
    {
        private readonly WhatsappDbContext _context;

        public EmployeesController(WhatsappDbContext context)
        {
            _context = context;
        }

        // Get all in-house employees
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _context.Users
                .OrderBy(u => u.EmpCode)
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

            return Ok(new { success = true, employees });
        }

        // Add a new in-house Employee with permissions
        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromBody] CreateEmployeeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, message = "Employee Name is required." });
            }

            var cleanCode = !string.IsNullOrWhiteSpace(request.EmpCode)
                ? request.EmpCode.Trim()
                : "EMP-" + (await _context.Users.CountAsync() + 1);

            // Check if emp code exists
            if (await _context.Users.AnyAsync(u => u.EmpCode == cleanCode))
            {
                return BadRequest(new { success = false, message = $"Employee code '{cleanCode}' already exists." });
            }

            var phone = !string.IsNullOrWhiteSpace(request.Phone)
                ? request.Phone.Trim()
                : $"+91 {new Random().Next(90000, 99999)}{new Random().Next(10000, 99999)}";

            var newEmp = new User
            {
                Id = "u_" + cleanCode.ToLower().Replace("-", "_") + "_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                EmpCode = cleanCode,
                Name = request.Name.Trim(),
                Phone = phone,
                Department = !string.IsNullOrWhiteSpace(request.Department) ? request.Department.Trim() : "General",
                Designation = !string.IsNullOrWhiteSpace(request.Designation) ? request.Designation.Trim() : "Staff",
                Email = request.Email?.Trim(),
                Role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim() : "Employee",
                Avatar = !string.IsNullOrWhiteSpace(request.Avatar) 
                    ? request.Avatar 
                    : $"https://api.dicebear.com/7.x/bottts/svg?seed={Uri.EscapeDataString(cleanCode)}",
                About = $"In-house team member | {request.Department}",
                Password = !string.IsNullOrWhiteSpace(request.Password) ? request.Password.Trim() : "Password@123",
                CanSendFiles = request.CanSendFiles,
                CanMakeCalls = request.CanMakeCalls,
                CanCreateGroups = request.CanCreateGroups,
                CanPostStatus = request.CanPostStatus,
                CanDeleteMessages = request.CanDeleteMessages,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newEmp);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Employee added successfully", employee = newEmp });
        }

        // Update employee permissions & details
        [HttpPut("{id}/permissions")]
        public async Task<IActionResult> UpdatePermissions(string id, [FromBody] UpdateEmployeePermissionsRequest request)
        {
            var emp = await _context.Users.FindAsync(id);
            if (emp == null) return NotFound(new { success = false, message = "Employee not found." });

            if (request.CanSendFiles.HasValue) emp.CanSendFiles = request.CanSendFiles.Value;
            if (request.CanMakeCalls.HasValue) emp.CanMakeCalls = request.CanMakeCalls.Value;
            if (request.CanCreateGroups.HasValue) emp.CanCreateGroups = request.CanCreateGroups.Value;
            if (request.CanPostStatus.HasValue) emp.CanPostStatus = request.CanPostStatus.Value;
            if (request.CanDeleteMessages.HasValue) emp.CanDeleteMessages = request.CanDeleteMessages.Value;
            if (request.IsActive.HasValue) emp.IsActive = request.IsActive.Value;
            if (!string.IsNullOrWhiteSpace(request.Role)) emp.Role = request.Role.Trim();
            if (!string.IsNullOrWhiteSpace(request.Department)) emp.Department = request.Department.Trim();
            if (!string.IsNullOrWhiteSpace(request.Designation)) emp.Designation = request.Designation.Trim();
            emp.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Employee permissions updated successfully.",
                employee = new UserDto
                {
                    Id = emp.Id,
                    EmpCode = emp.EmpCode,
                    Phone = emp.Phone,
                    Name = emp.Name,
                    Department = emp.Department,
                    Designation = emp.Designation,
                    Email = emp.Email,
                    Role = emp.Role,
                    CanSendFiles = emp.CanSendFiles,
                    CanMakeCalls = emp.CanMakeCalls,
                    CanCreateGroups = emp.CanCreateGroups,
                    CanPostStatus = emp.CanPostStatus,
                    CanDeleteMessages = emp.CanDeleteMessages,
                    IsActive = emp.IsActive,
                    Avatar = emp.Avatar,
                    About = emp.About,
                    IsOnline = emp.IsOnline,
                    LastSeen = emp.LastSeen
                }
            });
        }

        // Delete / Deactivate employee
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var emp = await _context.Users.FindAsync(id);
            if (emp == null) return NotFound();

            emp.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Employee deactivated." });
        }
    }
}
