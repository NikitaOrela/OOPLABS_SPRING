using CarRental.Domain.Enums;

namespace CarRental.Presentation.Contracts;

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public int DrivingExperienceYears { get; set; }
    public List<UserRole> Roles { get; set; } = new();
}

public class UpdateUserRolesRequest
{
    public int AdministratorId { get; set; }
    public List<UserRole> Roles { get; set; } = new();
}

public class UserResponse
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public int DrivingExperienceYears { get; set; }
    public List<UserRole> Roles { get; set; } = new();
}
