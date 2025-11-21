
namespace Lab3.CQRS.Queries;

public record GetAllBooksQuery(int Page = 1, int PageSize = 10);