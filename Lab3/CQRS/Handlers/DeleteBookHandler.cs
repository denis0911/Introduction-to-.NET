namespace Lab3.CQRS.Handlers;
using Lab3.CQRS.Commands;
using Lab3.Data;
using Microsoft.EntityFrameworkCore;
public class DeleteBookHandler(AppDbContext db)
{
    public async Task<bool> Handle(DeleteBookCommand cmd, CancellationToken ct = default)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == cmd.Id, ct);
        if (book == null) return false;
        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
        return true;
    }
}