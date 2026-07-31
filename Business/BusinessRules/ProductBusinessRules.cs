using Core.Utilities.Results;
using DataAccess.Abstract;
using Business.Constants;
using Core.Utilities.Exceptions;

namespace Business.BusinessRules
{
    /// <summary>
    /// Product (Ürün) ile ilgili iş kurallarını barındıran sınıf.
    /// Kurallar BusinessRules.Run() yardımcısına parametre olarak geçilerek çalıştırılır.
    /// </summary>
    public class ProductBusinessRules
    {
        private readonly IProductDal _productDal;

        public ProductBusinessRules(IProductDal productDal)
        {
            _productDal = productDal;
        }

        /// <summary>
        /// Aynı isimde başka bir ürünün eklenmesini veya güncellenmesini engeller.
        /// </summary>
        public async Task<IResult> CheckIfProductNameExists(string productName)
        {
            var exists = await _productDal.AnyAsync(p => p.Name.ToLower() == productName.ToLower());
            if (exists)
            {
                return new ErrorResult(Messages.ProductNameAlreadyExists, 409, ErrorCodes.Conflict);
            }
            return new SuccessResult();
        }
    }
}
