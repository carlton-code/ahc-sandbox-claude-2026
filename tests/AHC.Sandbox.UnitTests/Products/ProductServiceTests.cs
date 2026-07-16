using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Services;
using AHC.Sandbox.Domain.Entities;
using AHC.Sandbox.UnitTests.Products.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AHC.Sandbox.UnitTests.Products
{
    public class ProductServiceTests
    {
        private FakeProductReadRepository _readRepository = null!;
        private FakeProductWriteRepository _writeRepository = null!;
        private FakeProductCacheRepository _cacheRepository = null!;
        private ProductService _service = null!;

        [SetUp]
        public void Setup()
        {
            _readRepository = new FakeProductReadRepository();
            _writeRepository = new FakeProductWriteRepository();
            _cacheRepository = new FakeProductCacheRepository();
            _service = new ProductService(
                _readRepository,
                _writeRepository,
                _cacheRepository,
                NullLogger<ProductService>.Instance);
        }

        private static Product CreateProduct(
            int productId,
            string name = "HL Road Frame - Black, 58",
            string productNumber = "FR-R92B-58",
            string? color = "Black",
            decimal standardCost = 1059.31m,
            decimal listPrice = 1431.50m,
            string? size = "58",
            decimal? weight = 1016.04m,
            int? productCategoryId = 18,
            int? productModelId = 6,
            DateTime? sellEndDate = null,
            DateTime? discontinuedDate = null)
        {
            return new Product
            {
                ProductId = productId,
                Name = name,
                ProductNumber = productNumber,
                Color = color,
                StandardCost = standardCost,
                ListPrice = listPrice,
                Size = size,
                Weight = weight,
                ProductCategoryId = productCategoryId,
                ProductModelId = productModelId,
                SellStartDate = new DateTime(2002, 6, 1),
                SellEndDate = sellEndDate,
                DiscontinuedDate = discontinuedDate
            };
        }

        // --- GetProductsAsync -----------------------------------------------------------------

        [Test]
        public async Task GetProductsAsync_MapsAllRepositoryProductsToDtos()
        {
            _readRepository.Products.Add(CreateProduct(680));
            _readRepository.Products.Add(CreateProduct(
                879,
                name: "All-Purpose Bike Stand",
                productNumber: "ST-1401",
                color: null,
                size: null,
                weight: null));

            var result = await _service.GetProductsAsync();

            Assert.That(result, Has.Count.EqualTo(2));

            var first = result.Single(p => p.ProductId == 680);
            Assert.That(first.Name, Is.EqualTo("HL Road Frame - Black, 58"));
            Assert.That(first.ProductNumber, Is.EqualTo("FR-R92B-58"));

            var second = result.Single(p => p.ProductId == 879);
            Assert.That(second.Color, Is.Null);
            Assert.That(second.Size, Is.Null);
            Assert.That(second.Weight, Is.Null);
        }

        [Test]
        public async Task GetProductsAsync_EmptyRepository_ReturnsEmptyNotNull()
        {
            var result = await _service.GetProductsAsync();

            Assert.That(result, Is.Empty);
        }

        // --- GetProductByIdAsync --------------------------------------------------------------

        // The mapping is the only real logic in the read path, so this pins every field once.
        [Test]
        public async Task GetProductByIdAsync_KnownProduct_MapsEveryFieldToTheDto()
        {
            _readRepository.Products.Add(CreateProduct(680, sellEndDate: new DateTime(2007, 6, 30)));

            var result = await _service.GetProductByIdAsync(680);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ProductId, Is.EqualTo(680));
            Assert.That(result.Name, Is.EqualTo("HL Road Frame - Black, 58"));
            Assert.That(result.ProductNumber, Is.EqualTo("FR-R92B-58"));
            Assert.That(result.Color, Is.EqualTo("Black"));
            Assert.That(result.StandardCost, Is.EqualTo(1059.31m));
            Assert.That(result.ListPrice, Is.EqualTo(1431.50m));
            Assert.That(result.Size, Is.EqualTo("58"));
            Assert.That(result.Weight, Is.EqualTo(1016.04m));
            Assert.That(result.ProductCategoryId, Is.EqualTo(18));
            Assert.That(result.ProductModelId, Is.EqualTo(6));
            Assert.That(result.SellStartDate, Is.EqualTo(new DateTime(2002, 6, 1)));
            Assert.That(result.SellEndDate, Is.EqualTo(new DateTime(2007, 6, 30)));
            Assert.That(result.DiscontinuedDate, Is.Null);
            Assert.That(result.IsDiscontinued, Is.False);
        }

        [Test]
        public async Task GetProductByIdAsync_DiscontinuedProduct_SetsIsDiscontinuedOnTheDto()
        {
            _readRepository.Products.Add(CreateProduct(680, discontinuedDate: new DateTime(2006, 1, 1)));

            var result = await _service.GetProductByIdAsync(680);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.DiscontinuedDate, Is.EqualTo(new DateTime(2006, 1, 1)));
            Assert.That(result.IsDiscontinued, Is.True);
        }

        [Test]
        public async Task GetProductByIdAsync_UnknownProduct_ReturnsNull()
        {
            var result = await _service.GetProductByIdAsync(999999);

            Assert.That(result, Is.Null);
            Assert.That(_readRepository.GetByIdAsyncCallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetProductByIdAsync_CacheHit_ReturnsCachedDtoWithoutHittingReadRepository()
        {
            var cached = new ProductDto
            {
                ProductId = 680,
                Name = "HL Road Frame - Black, 58",
                ProductNumber = "FR-R92B-58"
            };
            _cacheRepository.Seed(680, cached);

            var result = await _service.GetProductByIdAsync(680);

            Assert.That(result, Is.SameAs(cached));
            Assert.That(_readRepository.GetByIdAsyncCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetProductByIdAsync_CacheMiss_FallsThroughToReadRepositoryAndPopulatesCache()
        {
            _readRepository.Products.Add(CreateProduct(680));

            var result = await _service.GetProductByIdAsync(680);

            Assert.That(result, Is.Not.Null);
            Assert.That(_cacheRepository.SetAsyncCalled, Is.True);
            Assert.That(_cacheRepository.Contains(680), Is.True);
        }

        // The failed cache read already proved Redis unreachable, so the service must not spend
        // a second Redis round-trip attempting to repopulate within the same request.
        [Test]
        public async Task GetProductByIdAsync_CacheUnavailableOnRead_FallsBackToReadRepositoryWithoutCaching()
        {
            _cacheRepository.ThrowOnGet = true;
            _readRepository.Products.Add(CreateProduct(680));

            var result = await _service.GetProductByIdAsync(680);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ProductId, Is.EqualTo(680));
            Assert.That(_cacheRepository.SetAsyncCalled, Is.False);
        }

        // --- CreateProductAsync ---------------------------------------------------------------

        [Test]
        public async Task CreateProductAsync_PassesDtoToWriteRepositoryUnmodified()
        {
            var createDto = new CreateProductDto
            {
                Name = "New Product",
                ProductNumber = "NP-0001",
                StandardCost = 10.50m,
                ListPrice = 19.99m,
                SellStartDate = new DateTime(2026, 1, 1)
            };

            await _service.CreateProductAsync(createDto);

            Assert.That(_writeRepository.LastCreateRequest, Is.SameAs(createDto));
        }

        [Test]
        public async Task CreateProductAsync_MapsCreatedProductToDto()
        {
            _writeRepository.ProductToReturnOnCreate = CreateProduct(1000, name: "New Product", productNumber: "NP-0001");

            var result = await _service.CreateProductAsync(new CreateProductDto());

            Assert.That(result.ProductId, Is.EqualTo(1000));
            Assert.That(result.Name, Is.EqualTo("New Product"));
            Assert.That(result.ProductNumber, Is.EqualTo("NP-0001"));
        }

        // --- UpdateProductAsync ---------------------------------------------------------------

        [Test]
        public async Task UpdateProductAsync_PassesIdAndDtoThrough_AndReturnsTrue()
        {
            var updateDto = new UpdateProductDto
            {
                Name = "Updated Product",
                ProductNumber = "UP-0001",
                StandardCost = 11m,
                ListPrice = 24.99m,
                SellStartDate = new DateTime(2026, 1, 1)
            };

            var result = await _service.UpdateProductAsync(680, updateDto);

            Assert.That(result, Is.True);
            Assert.That(_writeRepository.LastUpdateRequest, Is.Not.Null);
            Assert.That(_writeRepository.LastUpdateRequest!.Value.ProductId, Is.EqualTo(680));
            Assert.That(_writeRepository.LastUpdateRequest.Value.Dto, Is.SameAs(updateDto));
        }

        [Test]
        public async Task UpdateProductAsync_UnknownProduct_ReturnsFalse()
        {
            _writeRepository.UpdateResult = false;

            var result = await _service.UpdateProductAsync(999999, new UpdateProductDto());

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateProductAsync_WriteSucceeds_InvalidatesCache()
        {
            var result = await _service.UpdateProductAsync(680, new UpdateProductDto());

            Assert.That(result, Is.True);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.True);
            Assert.That(_cacheRepository.LastRemovedProductId, Is.EqualTo(680));
        }

        [Test]
        public async Task UpdateProductAsync_WriteFails_DoesNotTouchCache()
        {
            _writeRepository.UpdateResult = false;

            await _service.UpdateProductAsync(680, new UpdateProductDto());

            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.False);
        }

        // --- DeleteProductAsync ---------------------------------------------------------------

        [Test]
        public async Task DeleteProductAsync_PassesIdThrough_AndReturnsTrue()
        {
            var result = await _service.DeleteProductAsync(680);

            Assert.That(result, Is.True);
            Assert.That(_writeRepository.LastDeleteRequest, Is.EqualTo(680));
        }

        [Test]
        public async Task DeleteProductAsync_UnknownProduct_ReturnsFalse()
        {
            _writeRepository.DeleteResult = false;

            var result = await _service.DeleteProductAsync(999999);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteProductAsync_DeleteSucceeds_InvalidatesCache()
        {
            var result = await _service.DeleteProductAsync(680);

            Assert.That(result, Is.True);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.True);
            Assert.That(_cacheRepository.LastRemovedProductId, Is.EqualTo(680));
        }

        [Test]
        public async Task DeleteProductAsync_DeleteFails_DoesNotTouchCache()
        {
            _writeRepository.DeleteResult = false;

            await _service.DeleteProductAsync(680);

            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.False);
        }
    }
}
