using Lab3.CQRS.Commands;
using Lab3.Data;
using Lab3.Entities;
namespace Lab3.CQRS.Handlers;

public class CreateBookHandler(AppDbContext db)
{
    public async Task<Book> Handle(CreateBookCommand cmd, CancellationToken ct = default)
    {
        var book = new Book { Title = cmd.Title, Author = cmd.Author, Year = cmd.Year };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book;
    }
    
}