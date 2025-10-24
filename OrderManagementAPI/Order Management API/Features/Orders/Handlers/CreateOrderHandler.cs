using AutoMapper;
using Order_Management_API.Features.Orders.DTOs;
namespace Order_Management_API.Features.Orders.Handlers;

public class CreateOrderHandler(
    IRepository<Orders> orderRepository,
    IMapper mapper,
    ILogger<CreateOrderHandler> logger,
    ICacheService cacheService)
{
    private readonly IRepository<Orders> _orderRepository = orderRepository;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<OrderProfileDto> Handle(CreateOrderProfileRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title is required");

            if (string.IsNullOrWhiteSpace(request.ISBN))
                throw new ArgumentException("ISBN is required");
            
            var existingOrder = await _orderRepository.GetAsync(o => o.ISBN == request.ISBN);
            if (existingOrder != null)
            {
                logger.LogWarning(
                    "Attempted to create order with duplicate ISBN. ISBN: {ISBN}, Title: {Title}, Author: {Author}",
                    request.ISBN, request.Title, request.Author);
                throw new InvalidOperationException($"An order with ISBN '{request.ISBN}' already exists.");
            }
            var order = mapper.Map<Orders>(request);
            
            logger.LogInformation(
                "Creating new order. Title: {Title}, Author: {Author}, Category: {Category}, ISBN: {ISBN}, Price: {Price}, Stock: {StockQuantity}",
                order.Title, order.Author, order.Category, order.ISBN, order.Price, order.StockQuantity);
            
            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();
            
            await _cacheService.RemoveAsync("all_orders");
            
            var result = mapper.Map<OrderProfileDto>(order);

            logger.LogInformation(
                "Order created successfully. Id: {Id}, Title: {Title}, Availability: {AvailabilityStatus}",
                result.Id, result.Title, result.AvailabilityStatus);

            return result;
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Validation error while creating order. Title: {Title}, ISBN: {ISBN}",
                request.Title, request.ISBN);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Business logic error while creating order. ISBN: {ISBN}", request.ISBN);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while creating order. Title: {Title}", request.Title);
            throw new ApplicationException("An error occurred while creating the order.", ex);
        }
    }
}