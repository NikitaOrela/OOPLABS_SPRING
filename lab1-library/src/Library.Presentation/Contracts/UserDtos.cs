using Library.Domain.Enums;

namespace Library.Presentation.Contracts;

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<UserRole> Roles { get; set; } = new();
}

public class UserResponse
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<UserRole> Roles { get; set; } = new();
}
