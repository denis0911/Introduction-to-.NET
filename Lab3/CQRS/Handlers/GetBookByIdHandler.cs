namespace Lab3.CQRS.Handlers;
using Lab3.Data;
using Lab3.CQRS.Queries;
using Lab3.DTOs;
using Microsoft.EntityFrameworkCore;
public class GetBookByIdHandler(AppDbContext db)
{
    public async Task<BookDto?> Handle(GetBookByIdQuery q, CancellationToken ct = default)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == q.Id, ct);
        return book == null ? null : new BookDto(book.Id, book.Title, book.Author, book.Year);
    }
}