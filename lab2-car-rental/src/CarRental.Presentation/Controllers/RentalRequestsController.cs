using CarRental.Application.Abstractions;
using CarRental.Domain.Interfaces;
using CarRental.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Presentation.Controllers;

[ApiController]
[Route("api/rentals")]
public class RentalRequestsController : ControllerBase
{
    private readonly IRentalRequestService _service;
    private readonly IRentalRequestRepository _repository;

    public RentalRequestsController(IRentalRequestService service, IRentalRequestRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<RentalRequestResponse>> Create(
        [FromBody] CreateRentalRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var created = await _service.CreateAsync(
            body.ClientId,
            body.CarId,
            body.StartDate,
            body.EndDate,
            cancellationToken);
        var response = Mapping.ToResponse(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RentalRequestResponse>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }
        return Mapping.ToResponse(request);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<RentalRequestResponse>> Approve(
        [FromRoute] int id,
        [FromBody] ApproveRentalRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _service.ApproveAsync(id, body.ManagerId, cancellationToken);
        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return updated is null ? NotFound() : Mapping.ToResponse(updated);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<RentalRequestResponse>> Reject(
        [FromRoute] int id,
        [FromBody] RejectRentalRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _service.RejectAsync(id, body.ManagerId, body.Reason, cancellationToken);
        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return updated is null ? NotFound() : Mapping.ToResponse(updated);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<RentalRequestResponse>> Complete(
        [FromRoute] int id,
        [FromBody] CompleteRentalRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _service.CompleteAsync(id, body.ManagerId, body.ActualReturnDate, body.Damaged, cancellationToken);
        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return updated is null ? NotFound() : Mapping.ToResponse(updated);
    }
}
