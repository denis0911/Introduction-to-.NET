using System.ComponentModel.DataAnnotations;
using Order_Management_API.Features.Orders;

namespace Order_Management_API.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class OrderCategoryAttribute : ValidationAttribute
{
    private readonly OrderCategory[] _allowedCategories;

    public OrderCategoryAttribute(params OrderCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories;
        
        var categoryNames = string.Join(", ", allowedCategories.Select(c => c.ToString()));
        ErrorMessage = $"Category must be one of: {categoryNames}";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value is OrderCategory category)
        {
            if (_allowedCategories.Contains(category))
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(ErrorMessage);
    }
}