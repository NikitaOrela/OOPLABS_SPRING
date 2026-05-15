using CarRental.Application.Abstractions;
using CarRental.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Presentation.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var created = await _users.CreateAsync(
            body.UserName,
            body.FullName,
            body.Age,
            body.DrivingExperienceYears,
            body.Roles,
            cancellationToken);
        var response = Mapping.ToResponse(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }
        return Mapping.ToResponse(user);
    }

    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<UserResponse>> UpdateRoles(
        [FromRoute] int id,
        [FromBody] UpdateUserRolesRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var updated = await _users.UpdateRolesAsync(id, body.AdministratorId, body.Roles, cancellationToken);
        return Mapping.ToResponse(updated);
    }
}
