using AutoMapper;
using Business.BusinessRules;
using Business.Concrete;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Unit.Business
{
    /// <summary>
    /// ProductManager sınıfı için kurumsal standartlarda birim (unit) testleri.
    /// Paging ve sayfalama altyapısı bu sınıf üzerinden doğrulanır.
    /// </summary>
    public class ProductManagerTests
    {
        private readonly Mock<IProductDal> _productDalMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ProductBusinessRules _productBusinessRules;
        private readonly ProductManager _productManager;

        public ProductManagerTests()
        {
            _productDalMock = new Mock<IProductDal>();
            _mapperMock = new Mock<IMapper>();
            _productBusinessRules = new ProductBusinessRules(_productDalMock.Object);
            
            _productManager = new ProductManager(
                _productDalMock.Object, 
                _productBusinessRules, 
                _mapperMock.Object);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldReturnPaginatedList_WithCorrectPagingInfo()
        {
            // Arrange (Hazırlık)
            int pageNumber = 1;
            int pageSize = 2;
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Product 1", Price = 10, Stock = 100 },
                new Product { ProductId = 2, Name = "Product 2", Price = 20, Stock = 50 }
            };
            
            // Toplamda 5 kayıt olduğunu ve ilk 2 kaydı çektiğimizi simüle eden PaginatedList
            var paginatedList = new PaginatedList<Product>(products, 5, pageNumber, pageSize);

            _productDalMock.Setup(x => x.GetPaginatedAsync(null, pageNumber, pageSize))
                .ReturnsAsync(paginatedList);

            // Act (Eylem)
            var result = await _productManager.GetPagedAsync(pageNumber, pageSize);

            // Assert (Doğrulama)
            result.Success.Should().BeTrue();
            result.Message.Should().Be(Messages.ProductsListed);
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.PageIndex.Should().Be(pageNumber);
            result.Data.PageSize.Should().Be(pageSize);
            result.Data.TotalCount.Should().Be(5);
            result.Data.TotalPages.Should().Be(3); // 5 kayıt / 2'şer = 3 sayfa eder (tavan değer)
            result.Data.HasNextPage.Should().BeTrue();
            result.Data.HasPreviousPage.Should().BeFalse();
        }
    }
}
