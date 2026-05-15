using Library.Application.Abstractions;
using Library.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Library.Presentation.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;

    public BooksController(IBookService books)
    {
        _books = books;
    }

    [HttpPost]
    public async Task<ActionResult<BookResponse>> Create([FromBody] CreateBookRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var created = await _books.CreateAsync(body.WriterId, body.Title, body.Circulation, cancellationToken);
        var response = Mapping.ToResponse(created);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }
        return Mapping.ToResponse(book);
    }
}
