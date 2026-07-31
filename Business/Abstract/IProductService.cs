using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Product;

namespace Business.Abstract
{
    /// <summary>
    /// Product (Ürün) iş mantığı katmanı arayüzü.
    /// Clean Architecture prensiplerine göre servisler IResult/IDataResult tipleri döner.
    /// WebApi katmanı bu arayüz üzerinden haberleşir.
    /// </summary>
    public interface IProductService
    {
        Task<IDataResult<List<Product>>> GetAllAsync();
        Task<IDataResult<List<Product>>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IDataResult<Product>> GetByIdAsync(int productId);
        Task<IResult> AddAsync(ProductAddDto productAddDto);
        Task<IResult> UpdateAsync(ProductUpdateDto productUpdateDto);
        Task<IResult> DeleteAsync(int productId);
    }
}
