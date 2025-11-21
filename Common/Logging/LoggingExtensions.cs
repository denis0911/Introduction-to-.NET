

namespace Order_Management_API.Common.Logging;


public static class LoggingExtensions
{
    public static void LogOrderCreationMetrics(this ILogger logger, OrderCreationMetrics metrics)
    {
        var eventId = new EventId(LogEvents.OrderCreationCompleted, nameof(LogEvents.OrderCreationCompleted));
        
        logger.Log(
            metrics.Success ? LogLevel.Information : LogLevel.Error,
            eventId,
            "Order creation {Status} - OperationId: {OperationId}, Title: '{Title}', ISBN: {ISBN}, " +
            "Category: {Category}, ValidationDuration: {ValidationMs}ms, " +
            "DatabaseSaveDuration: {DatabaseMs}ms, TotalDuration: {TotalMs}ms" +
            (metrics.ErrorReason != null ? ", ErrorReason: {ErrorReason}" : ""),
            metrics.Success ? "completed" : "failed",
            metrics.OperationId,
            metrics.OrderTitle,
            metrics.ISBN,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.ErrorReason
        );
    }
}