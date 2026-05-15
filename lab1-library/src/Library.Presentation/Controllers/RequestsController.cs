using Library.Application.Abstractions;
using Library.Domain.Interfaces;
using Library.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Presentation.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly IBookRequestService _requests;
    private readonly IBookRequestRepository _requestRepository;

    public RequestsController(IBookRequestService requests, IBookRequestRepository requestRepository)
    {
        _requests = requests;
        _requestRepository = requestRepository;
    }

    [HttpPost]
    public async Task<ActionResult<BookRequestResponse>> Create([FromBody] CreateBookRequestRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var created = await _requests.CreateAsync(body.ApplicantId, body.BookId, body.Type, body.Quantity, cancellationToken);
        var response = Mapping.ToResponse(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookRequestResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }
        return Mapping.ToResponse(request);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<BookRequestResponse>> Approve(
        [FromRoute] int id,
        [FromBody] ResolveBookRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _requests.ApproveAsync(id, body.LibrarianId, cancellationToken);
        var updated = await _requestRepository.GetByIdAsync(id, cancellationToken);
        return updated is null ? NotFound() : Mapping.ToResponse(updated);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<BookRequestResponse>> Reject(
        [FromRoute] int id,
        [FromBody] ResolveBookRequestRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _requests.RejectAsync(id, body.LibrarianId, cancellationToken);
        var updated = await _requestRepository.GetByIdAsync(id, cancellationToken);
        return updated is null ? NotFound() : Mapping.ToResponse(updated);
    }
}
