namespace Lab3.CQRS.Commands;

public record CreateBookCommand(string Title, string Author, int Year);