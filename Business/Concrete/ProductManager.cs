using AutoMapper;
using Business.Abstract;
using Business.BusinessAspects.Autofac;
using Business.BusinessRules;
using Business.Constants;
using Business.ValidationRules.FluentValidation;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Validation;
using Core.Utilities.Business;
using Core.Utilities.Exceptions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Product;
using Microsoft.AspNetCore.Http;
using IResult = Core.Utilities.Results.IResult;
using BusinessRules = Core.Utilities.Business.BusinessRules;

namespace Business.Concrete
{
    /// <summary>
    /// Product (Ürün) iş mantığı yöneticisi.
    /// Clean Architecture prensiplerine göre tüm CRUD operasyonlarını gerçekleştirir.
    /// 
    /// AOP Aspect'leri:
    ///   - [SecuredOperation]: Yetki kontrolü (Admin/Moderator veya her ikisi)
    ///   - [ValidationAspect]: FluentValidation doğrulama kontrolü
    ///   - [CacheAspect]: Metod çıktısını ICacheManager'a ekler (sayfa/parametre duyarlı)
    ///   - [CacheRemoveAspect]: Metod bittiğinde eşleşen cache'leri temizler (veri tutarlılığı)
    /// </summary>
    public class ProductManager : IProductService
    {
        private readonly IProductDal _productDal;
        private readonly ProductBusinessRules _productBusinessRules;
        private readonly IMapper _mapper;

        public ProductManager(IProductDal productDal, ProductBusinessRules productBusinessRules, IMapper mapper)
        {
            _productDal = productDal;
            _productBusinessRules = productBusinessRules;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        [CacheAspect(duration: 30)] // 30 dakika cache'ler
        public async Task<IDataResult<List<Product>>> GetAllAsync()
        {
            var products = await _productDal.GetAllAsync();
            return new SuccessDataResult<List<Product>>(products, Messages.ProductsListed);
        }

        /// <inheritdoc/>
        [CacheAspect(duration: 10)] // Sayfalanmış veri 10 dakika cache'ler
        public async Task<IDataResult<List<Product>>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pagedProducts = await _productDal.GetPagedAsync(null, pageNumber, pageSize);
            return new SuccessDataResult<List<Product>>(pagedProducts, Messages.ProductsListed);
        }

        /// <inheritdoc/>
        [CacheAspect(duration: 60)] // Detay verisi 60 dakika cache'ler
        public async Task<IDataResult<Product>> GetByIdAsync(int productId)
        {
            var product = await _productDal.GetByIdAsync(productId);
            if (product == null)
            {
                return new ErrorDataResult<Product>(Messages.ProductNotFound, StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound);
            }
            return new SuccessDataResult<Product>(product, Messages.ProductListed);
        }

        /// <inheritdoc/>
        [SecuredOperation("admin,moderator")] // admin VEYA moderator yetkisi gerekir
        [ValidationAspect(typeof(ProductValidator))] // ProductValidator ile doğrulanır
        [CacheRemoveAspect("IProductService.Get")] // IProductService.Get ile başlayan cache'leri siler
        public async Task<IResult> AddAsync(ProductAddDto productAddDto)
        {
            // İş Kuralları Çalıştırılır (BusinessRules helper yardımıyla)
            var result = Core.Utilities.Business.BusinessRules.Run(
                await _productBusinessRules.CheckIfProductNameExists(productAddDto.Name)
            );

            if (result != null)
            {
                return result; // Hata durumunda ilk başarısız olan kural sonucu döner
            }

            // AutoMapper ile DTO -> Entity dönüşümü
            var product = _mapper.Map<Product>(productAddDto);

            // TODO: UserContextService aracılığıyla giriş yapan kullanıcının ID'si set edilebilir.
            // product.UserId = _userContextService.GetUserId();

            await _productDal.AddAsync(product);
            return new SuccessResult(Messages.ProductAdded, StatusCodes.Status201Created);
        }

        /// <inheritdoc/>
        [SecuredOperation("admin")] // Sadece admin yetkisi gerekir
        [ValidationAspect(typeof(ProductValidator))]
        [CacheRemoveAspect("IProductService.Get")]
        public async Task<IResult> UpdateAsync(ProductUpdateDto productUpdateDto)
        {
            var existingProduct = await _productDal.GetByIdAsync(productUpdateDto.ProductId);
            if (existingProduct == null)
            {
                return new ErrorResult(Messages.ProductNotFound, StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound);
            }

            // Eşleşen entity üzerine mapper ile DTO alanları kopyalanır
            _mapper.Map(productUpdateDto, existingProduct);

            await _productDal.UpdateAsync(existingProduct);
            return new SuccessResult(Messages.ProductUpdated);
        }

        /// <inheritdoc/>
        [SecuredOperation("admin")]
        [CacheRemoveAspect("IProductService.Get")]
        public async Task<IResult> DeleteAsync(int productId)
        {
            var product = await _productDal.GetByIdAsync(productId);
            if (product == null)
            {
                return new ErrorResult(Messages.ProductNotFound, StatusCodes.Status404NotFound, ErrorCodes.ResourceNotFound);
            }

            // Generic repository IsDeleted alanı varsa soft delete, yoksa hard delete yapar.
            await _productDal.DeleteAsync(product);
            return new SuccessResult(Messages.ProductDeleted);
        }
    }
}
