using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Order_Management_API.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidIsbnAttribute : ValidationAttribute, IClientModelValidator
{
    public ValidIsbnAttribute()
    {
        ErrorMessage = "ISBN must be a valid 10 or 13 digit number (hyphens and spaces are allowed)";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var isbn = value.ToString()!;
        
        // Remove hyphens and spaces
        var cleanedIsbn = isbn.Replace("-", "").Replace(" ", "");

        // Check if it's all digits and either 10 or 13 characters
        if (cleanedIsbn.All(char.IsDigit) && (cleanedIsbn.Length == 10 || cleanedIsbn.Length == 13))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-isbn"] = ErrorMessage ?? "Invalid ISBN format";
    }
}