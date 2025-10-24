using AutoMapper;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
namespace Order_Management_API.Common.Mapping;

public class AdvancedOrderMappingProfile : Profile
{
    public AdvancedOrderMappingProfile()
    {
        CreateMap<CreateOrderProfileRequest, Orders>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0));
        //.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    CreateMap<Orders, OrderProfileDto>()
        .ForMember(dest => dest.CategoryDisplayName, 
            opt => opt.MapFrom<CategoryDisplayResolver>())
        .ForMember(dest => dest.FormattedPrice, 
            opt => opt.MapFrom<PriceFormatterResolver>())
        .ForMember(dest => dest.PublishedAge, 
            opt => opt.MapFrom<PublishedAgeResolver>())
        .ForMember(dest => dest.AuthorInitials, 
            opt => opt.MapFrom<AuthorInitialsResolver>())
        .ForMember(dest => dest.AvailabilityStatus, 
            opt => opt.MapFrom<AvailabilityStatusResolver>())
        .ForMember(dest => dest.CoverImageUrl, 
            opt => opt.MapFrom<ConditionalCoverImageResolver>())
        .ForMember(dest => dest.Price, 
            opt => opt.MapFrom<ConditionalPriceResolver>());
    }
}
public class CategoryDisplayResolver : IValueResolver<Orders, OrderProfileDto, string>
    { 
        public string Resolve(Orders source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Category switch
            {
                OrderCategory.Fiction=> "Fiction & Literature",
                OrderCategory.NonFiction => "Non-Fiction",
                OrderCategory.Technical => "Technical & Professional",
                OrderCategory.Children => "Children's Books",
                _ => "Uncategorized"
            };
        }
        
    }

    
    public class PriceFormatterResolver : IValueResolver<Orders, OrderProfileDto, string>
    {
        public string Resolve(Orders source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Price.ToString("C2");
        }
    }


    public class PublishedAgeResolver : IValueResolver<Orders, OrderProfileDto, string>
    {
        public string Resolve(Orders source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            var daysOld = (DateTime.UtcNow - source.Published).TotalDays;

            if (daysOld < 30)
            {
                return "New Release";
            }

            if (daysOld < 365)
            {
                var monthsOld = (int)(daysOld / 30.44);
                return $"{monthsOld} months old";
            }

            if (daysOld < 1825)
            {
                var yearsOld = (int)(daysOld / 365.25);
                return $"{yearsOld} years old";
            }
            return "Classic";
        } 
    }


    public class AuthorInitialsResolver : IValueResolver<Orders, OrderProfileDto, string>
        {
            public string Resolve(Orders source, OrderProfileDto destination, string destMember, ResolutionContext context)
            {
                if (string.IsNullOrWhiteSpace(source.Author))
                    return "?";

                var nameParts = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (nameParts.Length >= 2)
                    return $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpper();

                return nameParts.Length == 1 ? nameParts[0][0].ToString().ToUpper() : "?";
            }
        }


        public class AvailabilityStatusResolver : IValueResolver<Orders, OrderProfileDto, string>
        {
            public string Resolve(Orders source, OrderProfileDto destination, string destMember, ResolutionContext context)
            {
                if (!source.IsAvailable)
                    return "Out of Stock";

                return source.StockQuantity switch
                {
                    0 => "Unavailable",
                    1 => "Last Copy",
                    >= 2 and <= 5 => "Limited Stock",
                    _ => "In Stock"
                };
            }
        }

        public class ConditionalCoverImageResolver : IValueResolver<Orders, OrderProfileDto, string?>
        {
            public string? Resolve(Orders source, OrderProfileDto destination, string? destMember,
                ResolutionContext context)
            {
                if (source.Category == OrderCategory.Children)
                    return null;

                return source.CoverImnageURL;
            }
        }

        public class ConditionalPriceResolver : IValueResolver<Orders, OrderProfileDto, decimal>
        {
            public decimal Resolve(Orders source, OrderProfileDto destination, decimal destMember,
                ResolutionContext context)
            {
                if (source.Category == OrderCategory.Children)
                    return source.Price * 0.9m;

                return source.Price;
            }
        }
    