using CarRental.Application.Abstractions;
using CarRental.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Presentation.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController : ControllerBase
{
    private readonly ICarService _cars;

    public CarsController(ICarService cars)
    {
        _cars = cars;
    }

    [HttpPost]
    public async Task<ActionResult<CarResponse>> Create(
        [FromBody] CreateCarRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var created = await _cars.CreateAsync(
            body.ManagerId,
            body.Vin,
            body.Make,
            body.Model,
            body.PowerHp,
            body.DailyTariff,
            cancellationToken);
        var response = Mapping.ToResponse(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CarResponse>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var car = await _cars.GetByIdAsync(id, cancellationToken);
        if (car is null)
        {
            return NotFound();
        }
        return Mapping.ToResponse(car);
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<CarResponse>> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateCarStatusRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var updated = await _cars.UpdateStatusAsync(id, body.ManagerId, body.Status, cancellationToken);
        return Mapping.ToResponse(updated);
    }
}
