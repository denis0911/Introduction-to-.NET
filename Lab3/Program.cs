using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Lab3.Data;
using Lab3.CQRS.Commands;
using Lab3.CQRS.Queries;
using Lab3.CQRS.Handlers;
using Lab3.DTOs;
using Lab3.Validators;

using System.Data.Common;
using Microsoft.Data.Sqlite; // add if missing

var builder = WebApplication.CreateBuilder(args);

// SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=books.db"));

// Register CQRS handlers
builder.Services.AddScoped<CreateBookHandler>();
builder.Services.AddScoped<UpdateBookHandler>();
builder.Services.AddScoped<DeleteBookHandler>();
builder.Services.AddScoped<GetBookByIdHandler>();
builder.Services.AddScoped<GetAllBooksHandler>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookValidator>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();



// CREATE
app.MapPost("/books", async (CreateBookCommand cmd, CreateBookHandler handler, IValidator<CreateBookCommand> validator) =>
{
    var validationResult = await validator.ValidateAsync(cmd);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var book = await handler.Handle(cmd);
    var dto = new BookDto(book.Id, book.Title, book.Author, book.Year);
    return Results.Created($"/books/{book.Id}", dto);
});


// UPDATE
app.MapPut("/books/{id:int}", async (int id, UpdateBookCommand cmd, UpdateBookHandler handler, IValidator<UpdateBookCommand> validator) =>
{
    cmd = cmd with { Id = id }; // ensure route id overrides body id
    var validationResult = await validator.ValidateAsync(cmd);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var updatedBook = await handler.Handle(cmd);
    if (updatedBook == null)
        return Results.NotFound();

    var dto = new BookDto(updatedBook.Id, updatedBook.Title, updatedBook.Author, updatedBook.Year);
    return Results.Ok(dto);
});


// GET by ID
app.MapGet("/books/{id:int}", async (int id, GetBookByIdHandler handler) =>
{
    var dto = await handler.Handle(new GetBookByIdQuery(id));
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});


// GET all with pagination
app.MapGet("/books", async (int page, int pageSize, GetAllBooksHandler handler) =>
{
    if (page <= 0) page = 1;
    if (pageSize <= 0) pageSize = 10;

    var query = new GetAllBooksQuery(page, pageSize);
    var (items, totalCount) = await handler.Handle(query);

   
    return Results.Ok(new { totalCount, items });
});


// DELETE
app.MapDelete("/books/{id:int}", async (int id, DeleteBookHandler handler) =>
{
    var success = await handler.Handle(new DeleteBookCommand(id));
    return success ? Results.NoContent() : Results.NotFound();
});
app.Run();

