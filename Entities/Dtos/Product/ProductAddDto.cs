using Core.Entities.Abstract;

namespace Entities.Dtos.Product
{
    /// <summary>
    /// Yeni ürün eklemek için kullanılan DTO.
    /// FluentValidation → ProductAddValidator ile doğrulanır.
    /// 
    /// DTO'lar entity'lerden bağımsız olarak değiştirilebilir.
    /// AutoMapper ile Product entity'sine dönüştürülür.
    /// </summary>
    public class ProductAddDto : IDto
    {
        /// <summary>Ürün adı - boş olamaz, max 200 karakter</summary>
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>Fiyat - pozitif olmalıdır</summary>
        public decimal Price { get; set; }

        /// <summary>Stok - negatif olamaz</summary>
        public int Stock { get; set; }

        public int CategoryId { get; set; }
    }
}
