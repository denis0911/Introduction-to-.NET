using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Order_Management_API.Common.Mapping;
using Order_Management_API.Common.Middleware;
using Order_Management_API.Data;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Validators;
using Order_Management_API.Features.Orders.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Order Management API",
        Version = "v1",
        Description = "Advanced .NET API for managing book orders with comprehensive validation and logging"
    });
});

// Add DbContext with in-memory database
builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseInMemoryDatabase("OrderManagementDb"));

// Add AutoMapper with both profiles
builder.Services.AddAutoMapper(typeof(AdvancedOrderMappingProfile));

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add FluentValidation
builder.Services.AddScoped<IValidator<CreateOrderProfileRequest>, CreateOrderProfileValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderProfileValidator>();

// Add handlers
builder.Services.AddScoped<CreateOrderHandler>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add Correlation Middleware
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();

// Map endpoints
app.MapPost("/orders", async (
    CreateOrderProfileRequest request,
    CreateOrderHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.Handle(request, cancellationToken);
        return Results.Created($"/orders/{result.Id}", result);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new
        {
            error = "Validation failed",
            message = ex.Message,
            details = ex.Errors?.Select(e => e.ErrorMessage).ToList()
        });
    }
    catch (Exception ex)
    {
        // Fixed: Remove statusCode parameter to avoid conflict
        return Results.Problem(
            detail: ex.Message,
            title: "An error occurred while creating the order"
        );
    }
})
.WithName("CreateOrder")
.WithDescription("Create a new book order with advanced validation and logging")
.WithTags("Orders")
.Produces<OrderProfileDto>(StatusCodes.Status201Created)
.Produces<object>(StatusCodes.Status400BadRequest)
.Produces<object>(StatusCodes.Status500InternalServerError);

app.MapGet("/orders", async (ApplicationContext context) =>
{
    var orders = await context.Orders.ToListAsync();
    return Results.Ok(orders);
})
.WithName("GetAllOrders")
.WithDescription("Retrieve all orders from the system")
.WithTags("Orders")
.Produces<List<Order>>(StatusCodes.Status200OK);

app.MapGet("/orders/{id:guid}", async (Guid id, ApplicationContext context) =>
{
    var order = await context.Orders.FindAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("GetOrderById")
.WithDescription("Retrieve a specific order by ID")
.WithTags("Orders")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();