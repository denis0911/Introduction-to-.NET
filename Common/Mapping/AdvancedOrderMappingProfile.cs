using AutoMapper;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;

namespace Order_Management_API.Common.Mapping;


public class AdvancedOrderMappingProfile : Profile
{
    public AdvancedOrderMappingProfile()
    {

        CreateMap<CreateOrderProfileRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Price, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                // Apply 10% discount for Children category
                return src.Category == OrderCategory.Children ? src.Price * 0.9m : src.Price;
            }))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                // Return null for Children category (content filtering)
                return src.Category == OrderCategory.Children ? null : src.CoverImageUrl;
            }));

        // Map Order to OrderProfileDto with custom resolvers
        CreateMap<Order, OrderProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
            .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
    }
}


public class CategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            OrderCategory.Fiction => "Fiction & Literature",
            OrderCategory.NonFiction => "Non-Fiction",
            OrderCategory.Technical => "Technical & Professional",
            OrderCategory.Children => "Children's Orders",
            _ => "Uncategorized"
        };
    }
}


public class PriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Price.ToString("C2");
    }
}


public class PublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        var daysSincePublished = (DateTime.UtcNow - source.PublishedDate).Days;

        if (daysSincePublished < 30)
        {
            return "New Release";
        }
        else if (daysSincePublished < 365)
        {
            var months = daysSincePublished / 30;
            return $"{months} month{(months != 1 ? "s" : "")} old";
        }
        else if (daysSincePublished < 1825) // 5 years
        {
            var years = daysSincePublished / 365;
            return $"{years} year{(years != 1 ? "s" : "")} old";
        }
        else
        {
            return "Classic";
        }
    }
}


public class AuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Author))
        {
            return "?";
        }

        var names = source.Author.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (names.Length >= 2)
        {
            // First letter of first and last names
            return $"{char.ToUpper(names[0][0])}{char.ToUpper(names[^1][0])}";
        }
        else if (names.Length == 1)
        {
            // First letter of single name
            return char.ToUpper(names[0][0]).ToString();
        }

        return "?";
    }
}

public class AvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
{
    public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock";
        }

        if (source.StockQuantity == 0)
        {
            return "Unavailable";
        }
        else if (source.StockQuantity == 1)
        {
            return "Last Copy";
        }
        else if (source.StockQuantity <= 5)
        {
            return "Limited Stock";
        }
        else
        {
            return "In Stock";
        }
    }
}