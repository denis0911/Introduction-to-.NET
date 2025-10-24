namespace Order_Management_API.Features.Orders.DTOs;

public class OrderProfileDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public string CategoryDisplayName { get; set; }
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string PublishedAge { get; set; }
    public string AuthorInitials { get; set; }
    public string AvailabilityStatus { get; set; }
}

public class CreateOrderProfileRequest
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public OrderCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public int StockQuantity { get; set; } = 1;
}