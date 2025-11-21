using FluentValidation;
using Microsoft.EntityFrameworkCore;

using Order_Management_API.Data;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using System.Text.RegularExpressions;

namespace Order_Management_API.Validators;

public class CreateOrderProfileValidator : AbstractValidator<CreateOrderProfileRequest>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CreateOrderProfileValidator> _logger;

    // Inappropriate words list for content filtering
    private static readonly HashSet<string> InappropriateWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "offensive1", "offensive2", "inappropriate", "banned"
    };

    // Technical keywords for Technical category validation
    private static readonly HashSet<string> TechnicalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "programming", "coding", "software", "algorithm", "database", "api", "framework",
        "development", "engineering", "technical", "computer", "system", "architecture"
    };

    // Restricted words for children's orders
    private static readonly HashSet<string> RestrictedWordsForChildren = new(StringComparer.OrdinalIgnoreCase)
    {
        "violence", "horror", "scary", "adult", "mature"
    };

    public CreateOrderProfileValidator(ApplicationContext context, ILogger<CreateOrderProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        ConfigureTitleValidation();
        ConfigureAuthorValidation();
        ConfigureIsbnValidation();
        ConfigureCategoryValidation();
        ConfigurePriceValidation();
        ConfigurePublishedDateValidation();
        ConfigureStockQuantityValidation();
        ConfigureCoverImageUrlValidation();
        ConfigureBusinessRulesValidation();
        ConfigureConditionalValidation();
    }

    private void ConfigureTitleValidation()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters")
            .Must(BeValidTitle)
            .WithMessage("Title contains inappropriate content")
            .MustAsync(BeUniqueTitle)
            .WithMessage("A book with this title by this author already exists");
    }

    private void ConfigureAuthorValidation()
    {
        RuleFor(x => x.Author)
            .NotEmpty()
            .WithMessage("Author is required")
            .Length(2, 100)
            .WithMessage("Author must be between 2 and 100 characters")
            .Must(BeValidAuthorName)
            .WithMessage("Author name contains invalid characters. Only letters, spaces, hyphens, apostrophes, and dots are allowed");
    }

    private void ConfigureIsbnValidation()
    {
        RuleFor(x => x.ISBN)
            .NotEmpty()
            .WithMessage("ISBN is required")
            .Must(BeValidIsbn)
            .WithMessage("ISBN must be a valid 10 or 13 digit format")
            .MustAsync(BeUniqueIsbn)
            .WithMessage("An order with this ISBN already exists");
    }

    private void ConfigureCategoryValidation()
    {
        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid category value");
    }

    private void ConfigurePriceValidation()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0")
            .LessThan(10000)
            .WithMessage("Price must be less than $10,000");
    }

    private void ConfigurePublishedDateValidation()
    {
        RuleFor(x => x.PublishedDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Published date cannot be in the future")
            .GreaterThan(new DateTime(1400, 1, 1))
            .WithMessage("Published date cannot be before year 1400");
    }

    private void ConfigureStockQuantityValidation()
    {
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative")
            .LessThanOrEqualTo(100000)
            .WithMessage("Stock quantity cannot exceed 100,000");
    }

    private void ConfigureCoverImageUrlValidation()
    {
        RuleFor(x => x.CoverImageUrl)
            .Must(BeValidImageUrl!)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("Cover image URL must be a valid HTTP/HTTPS URL ending with .jpg, .jpeg, .png, .gif, or .webp");
    }

    private void ConfigureBusinessRulesValidation()
    {
        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Order does not meet business rule requirements");
    }

    private void ConfigureConditionalValidation()
    {
        // Technical order conditions
        When(x => x.Category == OrderCategory.Technical, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(20.00m)
                .WithMessage("Technical orders must have a minimum price of $20.00");

            RuleFor(x => x.Title)
                .Must(ContainTechnicalKeywords)
                .WithMessage("Technical orders must contain technical keywords in the title");

            RuleFor(x => x)
                .Must(x => (DateTime.UtcNow - x.PublishedDate).TotalDays <= 1825) // 5 years
                .WithMessage("Technical orders must be published within the last 5 years");
        });

        // Children's order conditions
        When(x => x.Category == OrderCategory.Children, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(50.00m)
                .WithMessage("Children's orders must have a maximum price of $50.00");

            RuleFor(x => x.Title)
                .Must(BeAppropriateForChildren)
                .WithMessage("Children's order title contains inappropriate content");
        });

        // Fiction order conditions
        When(x => x.Category == OrderCategory.Fiction, () =>
        {
            RuleFor(x => x.Author)
                .MinimumLength(5)
                .WithMessage("Fiction orders require full author names (minimum 5 characters)");
        });

        // Cross-field validation for expensive orders
        RuleFor(x => x)
            .Must(x => x.Price <= 100 || x.StockQuantity <= 20)
            .WithMessage("Expensive orders (>$100) must have limited stock (≤20 units)");
    }

    // Validation helper methods
    private bool BeValidTitle(string title)
    {
        return !InappropriateWords.Any(word => title.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> BeUniqueTitle(CreateOrderProfileRequest request, string title, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating title uniqueness for '{Title}' by '{Author}'", title, request.Author);
        
        var exists = await _context.Orders
            .AnyAsync(o => o.Title == title && o.Author == request.Author, cancellationToken);

        return !exists;
    }

    private bool BeValidAuthorName(string author)
    {
        // Allow letters, spaces, hyphens, apostrophes, and dots
        var regex = new Regex(@"^[a-zA-Z\s\-'.]+$");
        return regex.IsMatch(author);
    }

    private bool BeValidIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        // Remove hyphens and spaces
        var cleanedIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Must be 10 or 13 digits
        return cleanedIsbn.All(char.IsDigit) && (cleanedIsbn.Length == 10 || cleanedIsbn.Length == 13);
    }

    private async Task<bool> BeUniqueIsbn(string isbn, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating ISBN uniqueness for '{ISBN}'", isbn);
        
        var exists = await _context.Orders
            .AnyAsync(o => o.Isbn == isbn, cancellationToken);

        return !exists;
    }

    private bool BeValidImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return validExtensions.Any(ext => uri.AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> PassBusinessRules(CreateOrderProfileRequest request, CancellationToken cancellationToken)
    {
        // Rule 1: Daily order addition limit check (max 500 per day)
        var today = DateTime.UtcNow.Date;
        var todayOrderCount = await _context.Orders
            .CountAsync(o => o.CreatedAt.Date == today, cancellationToken);

        if (todayOrderCount >= 500)
        {
            _logger.LogWarning("Daily order limit reached: {Count}/500", todayOrderCount);
            return false;
        }

        // Rule 2: Technical orders minimum price check
        if (request is { Category: OrderCategory.Technical, Price: < 20.00m })
        {
            _logger.LogWarning("Technical order price too low: {Price}", request.Price);
            return false;
        }

        // Rule 3: Children's order content restrictions
        if (request.Category == OrderCategory.Children)
        {
            if (RestrictedWordsForChildren.Any(word => 
                request.Title.Contains(word, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Children's order contains restricted content in title");
                return false;
            }
        }

        // Rule 4: High-value order stock limit
        if (request is { Price: > 500, StockQuantity: > 10 })
        {
            _logger.LogWarning("High-value order has too much stock: Price={Price}, Stock={Stock}", 
                request.Price, request.StockQuantity);
            return false;
        }

        return true;
    }

    private bool ContainTechnicalKeywords(string title)
    {
        return TechnicalKeywords.Any(keyword => 
            title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAppropriateForChildren(string title)
    {
        return !RestrictedWordsForChildren.Any(word => 
            title.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}