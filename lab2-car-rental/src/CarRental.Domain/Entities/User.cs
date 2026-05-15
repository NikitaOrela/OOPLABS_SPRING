using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public int DrivingExperienceYears { get; set; }
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    public bool HasRole(UserRole role) => Roles.Contains(role);
}
