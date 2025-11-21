namespace Lab3.CQRS.Commands;

public record UpdateBookCommand(int Id, string Title, string Author, int Year);