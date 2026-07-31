using Core.Entities.Abstract;

namespace Entities.Dtos.Product
{
    /// <summary>
    /// Ürün güncelleme DTO'su.
    /// ProductId zorunludur (hangi ürün güncelleneceğini belirtir).
    /// Diğer alanlar seçimli güncellenebilir.
    /// </summary>
    public class ProductUpdateDto : IDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
    }
}
