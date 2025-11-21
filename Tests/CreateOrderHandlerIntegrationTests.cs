using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Order_Management_API.Common.Mapping;
using Order_Management_API.Data;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Validators;
using Order_Management_API.Features.Orders.Handlers;
using Xunit;

namespace Order_Management_API.Tests;

public class CreateOrderHandlerIntegrationTests : IDisposable
{
    private readonly ApplicationContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CreateOrderHandler>> _loggerMock;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: $"OrderTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationContext(options);
        
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AdvancedOrderMappingProfile>();
        }).CreateMapper();
        
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        _loggerMock = new Mock<ILogger<CreateOrderHandler>>();
        var validatorLoggerMock = new Mock<ILogger<CreateOrderProfileValidator>>();
        
        var validator = new CreateOrderProfileValidator(_context, validatorLoggerMock.Object);
        
        _handler = new CreateOrderHandler(
            _context,
            mapper,
            validator,
            _cache,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMappings()
    {
        var request = new CreateOrderProfileRequest
        {
            Title = "Advanced Programming Algorithms",
            Author = "John Smith",
            ISBN = "978-0-123456-78-9",
            Category = OrderCategory.Technical,
            Price = 49.99m,
            PublishedDate = DateTime.UtcNow.AddMonths(-6),
            CoverImageUrl = "https://example.com/cover.jpg",
            StockQuantity = 15
        };
        
        var result = await _handler.Handle(request);
        
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Advanced Programming Algorithms", result.Title);
        Assert.Equal("John Smith", result.Author);
        Assert.Equal("978-0-123456-78-9", result.ISBN);
        
        Assert.Equal("Technical & Professional", result.CategoryDisplayName);
        
        Assert.Equal("JS", result.AuthorInitials);
        
        Assert.Equal("6 months old", result.PublishedAge);
        
        Assert.StartsWith("$", result.FormattedPrice);
        Assert.Contains("49.99", result.FormattedPrice);
        
        Assert.Equal("In Stock", result.AvailabilityStatus);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == 2001),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
    {
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(),
            Title = "Existing Book",
            Author = "Jane Doe",
            Isbn = "978-1-234567-89-0",
            Category = OrderCategory.Fiction,
            Price = 29.99m,
            PublishedDate = DateTime.UtcNow.AddYears(-1),
            StockQuantity = 10,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();
        
        var request = new CreateOrderProfileRequest
        {
            Title = "New Book",
            Author = "John Smith",
            ISBN = "978-1-234567-89-0", // Same ISBN
            Category = OrderCategory.Fiction,
            Price = 19.99m,
            PublishedDate = DateTime.UtcNow.AddMonths(-3),
            StockQuantity = 5
        };
        
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(request));
        
        Assert.Contains("already exists", exception.Message);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.Is<EventId>(e => e.Id == 2002),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
    {
        var request = new CreateOrderProfileRequest
        {
            Title = "Fun Stories for Kids",
            Author = "Mary Johnson",
            ISBN = "978-2-345678-90-1",
            Category = OrderCategory.Children,
            Price = 20.00m, // Original price
            PublishedDate = DateTime.UtcNow.AddMonths(-2),
            CoverImageUrl = "https://example.com/kids-cover.jpg", // Should be filtered
            StockQuantity = 3
        };

      
        var result = await _handler.Handle(request);
        
        Assert.NotNull(result);
        
        Assert.Equal("Children's Orders", result.CategoryDisplayName);
        
        Assert.Equal(18.00m, result.Price);
        
        Assert.Null(result.CoverImageUrl);
        
        var savedOrder = await _context.Orders
            .FirstOrDefaultAsync(o => o.Isbn == request.ISBN);
        
        Assert.NotNull(savedOrder);
        Assert.Equal(18.00m, savedOrder.Price);
        Assert.Null(savedOrder.CoverImageUrl);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _cache.Dispose();
    }
}