using Order_Management_API.Features.Orders;

namespace Order_Management_API.Common.Logging;


public static class LogEvents
{
    public const int OrderCreationStarted = 2001;
    public const int OrderValidationFailed = 2002;
    public const int OrderCreationCompleted = 2003;
    public const int DatabaseOperationStarted = 2004;
    public const int DatabaseOperationCompleted = 2005;
    public const int CacheOperationPerformed = 2006;
    public const int ISBNValidationPerformed = 2007;
    public const int StockValidationPerformed = 2008;
}

public record OrderCreationMetrics
{
  
    public required string OperationId { get; init; }
    
    public required string OrderTitle { get; init; }
    
    public required string ISBN { get; init; }
    
    public required OrderCategory Category { get; init; }
    
    public required TimeSpan ValidationDuration { get; init; }
    
    public required TimeSpan DatabaseSaveDuration { get; init; }
    
    public required TimeSpan TotalDuration { get; init; }
    
    public required bool Success { get; init; }
    
    public string? ErrorReason { get; init; }
}