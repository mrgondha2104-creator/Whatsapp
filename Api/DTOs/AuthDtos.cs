using System;

namespace Api.DTOs
{
    public class LoginRequest
    {
        public string? UserId { get; set; } // Can be EmpCode, Email, or Phone
        public string? EmpCode { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Name { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? Name { get; set; }
        public string? About { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string? EmpCode { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = "General";
        public string Designation { get; set; } = "Staff";
        public string? Email { get; set; }
        public string Role { get; set; } = "Employee";
        public bool CanSendFiles { get; set; } = true;
        public bool CanMakeCalls { get; set; } = true;
        public bool CanCreateGroups { get; set; } = true;
        public bool CanPostStatus { get; set; } = true;
        public bool CanDeleteMessages { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public string? Avatar { get; set; }
        public string About { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class CreateEmployeeRequest
    {
        public string EmpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public string Department { get; set; } = "General";
        public string Designation { get; set; } = "Staff";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = "Employee";
        public string? Avatar { get; set; }
        public bool CanSendFiles { get; set; } = true;
        public bool CanMakeCalls { get; set; } = true;
        public bool CanCreateGroups { get; set; } = true;
        public bool CanPostStatus { get; set; } = true;
        public bool CanDeleteMessages { get; set; } = true;
    }

    public class UpdateEmployeePermissionsRequest
    {
        public bool? CanSendFiles { get; set; }
        public bool? CanMakeCalls { get; set; }
        public bool? CanCreateGroups { get; set; }
        public bool? CanPostStatus { get; set; }
        public bool? CanDeleteMessages { get; set; }
        public bool? IsActive { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
    }
}
