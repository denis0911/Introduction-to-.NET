using Lab3.CQRS.Commands;
using Lab3.Data;
using Lab3.Entities;
using Microsoft.EntityFrameworkCore;
namespace Lab3.CQRS.Handlers;

public class UpdateBookHandler(AppDbContext db)
{
    public async Task<Book?> Handle(UpdateBookCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.Id, ct);
        if (book == null) return null;
        book.Title = cmd.Title;
        book.Author = cmd.Author;
        book.Year = cmd.Year;
        await db.SaveChangesAsync(ct);
        return book;
    }
}