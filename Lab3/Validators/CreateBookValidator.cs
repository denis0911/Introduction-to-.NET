namespace Lab3.Validators;
using FluentValidation;
using Lab3.CQRS.Commands;
public class CreateBookValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(0, DateTime.UtcNow.Year);
    }
}