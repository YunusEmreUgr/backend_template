using Business.Abstract;
using Entities.Dtos.Product;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Ürün (Product) işlemlerini yöneten RESTful API Controller sınıfı.
    /// Tüm metodlar Business (Manager) servislerini çağırır ve standard API yanıtı döner.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>Sistemdeki tüm aktif ürünleri listeler.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _productService.GetAllAsync();
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Ürünleri sayfalanmış (paged) olarak listeler.</summary>
        [HttpGet("getpaged")]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _productService.GetPagedAsync(pageNumber, pageSize);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>ID değerine göre tek bir ürün detayı döner.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productService.GetByIdAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Yeni bir ürün ekler. (Aspect: admin veya moderator rolü gerekir)</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ProductAddDto productAddDto)
        {
            var result = await _productService.AddAsync(productAddDto);
            if (result.Success)
            {
                return StatusCode(result.StatusCode, result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Mevcut bir ürünü günceller. (Aspect: admin rolü gerekir)</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductUpdateDto productUpdateDto)
        {
            var result = await _productService.UpdateAsync(productUpdateDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Ürünü sistemden (soft-delete ile) siler. (Aspect: admin rolü gerekir)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
