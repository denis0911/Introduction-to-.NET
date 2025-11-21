namespace Lab3.CQRS.Handlers;
using Lab3.Data;
using Lab3.CQRS.Queries;
using Lab3.DTOs;
using Microsoft.EntityFrameworkCore;
public class GetAllBooksHandler(AppDbContext db)
{
    public async Task<(IEnumerable<BookDto> Items, int TotalCount)> Handle(GetAllBooksQuery q, CancellationToken ct = default)
    {
        if (q.Page <= 0) q = q with { Page = 1 };
        if (q.PageSize <= 0) q = q with { PageSize = 10 };

        var query = db.Books.AsNoTracking().OrderBy(b => b.Id);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(b => new BookDto(b.Id, b.Title, b.Author, b.Year))
            .ToListAsync(ct);

        return (items, totalCount);
    }
}

//  Pagination must be applied before materializing results with ToListAsync().
//   Applying it after ToList() loads all data into memory, then paginates in-memory.
//   This is inefficient and can cause huge performance issues on large datasets,
//   as the entire table is first loaded instead of letting the database handle LIMIT/OFFSET.