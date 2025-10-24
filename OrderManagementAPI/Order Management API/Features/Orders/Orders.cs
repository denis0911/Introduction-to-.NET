namespace Order_Management_API.Features.Orders;

public class Orders
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public OrderCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime Published { get; set; }
    public string? CoverImnageURL { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StockQuantity { get; set; }
}