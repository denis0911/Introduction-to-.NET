using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Order_Management_API.Common.Logging;
using Order_Management_API.Data;
using Order_Management_API.Features.Orders.DTOs;
using System.Diagnostics;

namespace Order_Management_API.Features.Orders.Handlers;

public class CreateOrderHandler
{
    private readonly ApplicationContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateOrderProfileRequest> _validator;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        ApplicationContext context,
        IMapper mapper,
        IValidator<CreateOrderProfileRequest> validator,
        IMemoryCache cache,
        ILogger<CreateOrderHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _validator = validator;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<OrderProfileDto> Handle(
        CreateOrderProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var operationStartTime = Stopwatch.GetTimestamp();
        var operationId = Guid.NewGuid().ToString("N")[..8];
        
        TimeSpan validationDuration = TimeSpan.Zero;
        TimeSpan databaseSaveDuration = TimeSpan.Zero;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["OrderTitle"] = request.Title,
            ["ISBN"] = request.ISBN,
            ["Category"] = request.Category
        }))
        {
            try
            {
                // Log operation start
                _logger.LogInformation(
                    new EventId(LogEvents.OrderCreationStarted, nameof(LogEvents.OrderCreationStarted)),
                    "Starting order creation - Title: '{Title}', Author: '{Author}', ISBN: {ISBN}, Category: {Category}",
                    request.Title,
                    request.Author,
                    request.ISBN,
                    request.Category
                );

                // Validation phase with timing
                var validationStartTime = Stopwatch.GetTimestamp();
                
                // Log ISBN validation
                _logger.LogInformation(
                    new EventId(LogEvents.ISBNValidationPerformed, nameof(LogEvents.ISBNValidationPerformed)),
                    "Performing ISBN uniqueness check for ISBN: {ISBN}",
                    request.ISBN
                );

                // Check ISBN uniqueness
                var existingOrder = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Isbn == request.ISBN, cancellationToken);

                if (existingOrder != null)
                {
                    _logger.LogWarning(
                        new EventId(LogEvents.OrderValidationFailed, nameof(LogEvents.OrderValidationFailed)),
                        "Order validation failed - ISBN {ISBN} already exists for order '{Title}'",
                        request.ISBN,
                        existingOrder.Title
                    );

                    throw new ValidationException($"An order with ISBN {request.ISBN} already exists");
                }

                // Log stock validation
                _logger.LogInformation(
                    new EventId(LogEvents.StockValidationPerformed, nameof(LogEvents.StockValidationPerformed)),
                    "Validating stock quantity: {StockQuantity}",
                    request.StockQuantity
                );

                // Perform full validation
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    
                    _logger.LogWarning(
                        new EventId(LogEvents.OrderValidationFailed, nameof(LogEvents.OrderValidationFailed)),
                        "Order validation failed - {ErrorCount} error(s): {Errors}",
                        validationResult.Errors.Count,
                        errors
                    );

                    throw new ValidationException(errors);
                }

                validationDuration = Stopwatch.GetElapsedTime(validationStartTime);

                // Database operation phase with timing
                var dbStartTime = Stopwatch.GetTimestamp();
                
                _logger.LogInformation(
                    new EventId(LogEvents.DatabaseOperationStarted, nameof(LogEvents.DatabaseOperationStarted)),
                    "Starting database save operation for order '{Title}'",
                    request.Title
                );

                // Map request to entity
                var order = _mapper.Map<Order>(request);

                // Save to database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    new EventId(LogEvents.DatabaseOperationCompleted, nameof(LogEvents.DatabaseOperationCompleted)),
                    "Database save completed - OrderId: {OrderId}",
                    order.Id
                );

                databaseSaveDuration = Stopwatch.GetElapsedTime(dbStartTime);

                // Invalidate cache
                _cache.Remove("all_orders");
                
                _logger.LogInformation(
                    new EventId(LogEvents.CacheOperationPerformed, nameof(LogEvents.CacheOperationPerformed)),
                    "Cache invalidated - Key: 'all_orders'"
                );

                // Map to DTO
                var result = _mapper.Map<OrderProfileDto>(order);

                // Calculate total duration and log metrics
                var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);

                var metrics = new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = request.Category,
                    ValidationDuration = validationDuration,
                    DatabaseSaveDuration = databaseSaveDuration,
                    TotalDuration = totalDuration,
                    Success = true
                };

                _logger.LogOrderCreationMetrics(metrics);

                return result;
            }
            catch (Exception ex)
            {
                // Log error metrics
                var totalDuration = Stopwatch.GetElapsedTime(operationStartTime);

                var errorMetrics = new OrderCreationMetrics
                {
                    OperationId = operationId,
                    OrderTitle = request.Title,
                    ISBN = request.ISBN,
                    Category = request.Category,
                    ValidationDuration = validationDuration,
                    DatabaseSaveDuration = databaseSaveDuration,
                    TotalDuration = totalDuration,
                    Success = false,
                    ErrorReason = ex.Message
                };

                _logger.LogOrderCreationMetrics(errorMetrics);

                // Re-throw for global handler
                throw;
            }
        }
    }
}