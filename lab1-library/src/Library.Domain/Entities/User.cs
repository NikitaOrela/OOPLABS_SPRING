using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    public bool HasRole(UserRole role) => Roles.Contains(role);
}
