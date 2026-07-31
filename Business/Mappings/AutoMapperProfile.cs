using AutoMapper;
using Core.Entities.Concrete.Users;
using Entities.Concrete;
using Entities.Dtos.Auth;
using Entities.Dtos.Product;

namespace Business.Mappings
{
    /// <summary>
    /// AutoMapper profili. DTO ve Entity modelleri arasındaki eşleşmeleri tanımlar.
    /// Program.cs'te services.AddAutoMapper(typeof(AutoMapperProfile)) ile otomatik yüklenir.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ─── Product Eşleşmeleri ──────────────────────────────────────────
            CreateMap<ProductAddDto, Product>();
            CreateMap<ProductUpdateDto, Product>();

            // ─── Auth/User Eşleşmeleri ────────────────────────────────────────
            CreateMap<UserForRegisterDto, User>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false));
        }
    }
}
