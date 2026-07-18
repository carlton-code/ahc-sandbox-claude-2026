using AHC.Sandbox.Application.ProductModels.Dtos;
using AHC.Sandbox.Application.ProductModels.Services;
using AHC.Sandbox.Domain.Entities;
using AHC.Sandbox.UnitTests.ProductModels.Fakes;
using AHC.Sandbox.UnitTests.Products.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AHC.Sandbox.UnitTests.ProductModels
{
    public class ProductModelServiceTests
    {
        private FakeProductModelReadRepository _readRepository = null!;
        private FakeProductModelWriteRepository _writeRepository = null!;
        private FakeProductCacheRepository _productCacheRepository = null!;
        private ProductModelService _service = null!;

        [SetUp]
        public void Setup()
        {
            _readRepository = new FakeProductModelReadRepository();
            _writeRepository = new FakeProductModelWriteRepository();
            _productCacheRepository = new FakeProductCacheRepository();
            _service = new ProductModelService(
                _readRepository,
                _writeRepository,
                _productCacheRepository,
                NullLogger<ProductModelService>.Instance);
        }

        // --- GetByIdAsync ---------------------------------------------------------------------

        [Test]
        public async Task GetByIdAsync_KnownModel_MapsToDto()
        {
            _readRepository.ModelById = new ProductModel
            {
                ProductModelId = 6,
                Name = "HL Road Frame",
                Description = "Our lightest and best quality aluminum frame."
            };

            var result = await _service.GetByIdAsync(6);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ModelId, Is.EqualTo(6));
            Assert.That(result.Name, Is.EqualTo("HL Road Frame"));
            Assert.That(result.Description, Is.EqualTo("Our lightest and best quality aluminum frame."));
        }

        [Test]
        public async Task GetByIdAsync_ModelWithNoDescription_MapsNullDescription()
        {
            _readRepository.ModelById = new ProductModel { ProductModelId = 128, Name = "Rear Brake", Description = null };

            var result = await _service.GetByIdAsync(128);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Description, Is.Null);
        }

        [Test]
        public async Task GetByIdAsync_UnknownModel_ReturnsNull()
        {
            var result = await _service.GetByIdAsync(999999);

            Assert.That(result, Is.Null);
        }

        // --- GetByProductIdAsync --------------------------------------------------------------

        [Test]
        public async Task GetByProductIdAsync_KnownProduct_MapsItsModel()
        {
            _readRepository.ModelByProductId = new ProductModel { ProductModelId = 6, Name = "HL Road Frame" };

            var result = await _service.GetByProductIdAsync(680);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ModelId, Is.EqualTo(6));
            Assert.That(_readRepository.LastRequestedProductId, Is.EqualTo(680));
        }

        [Test]
        public async Task GetByProductIdAsync_UnknownProductOrNoModel_ReturnsNull()
        {
            var result = await _service.GetByProductIdAsync(999999);

            Assert.That(result, Is.Null);
        }

        // --- SetEnglishDescriptionAsync -------------------------------------------------------

        [Test]
        public async Task SetEnglishDescriptionAsync_PassesModelIdAndDescriptionThrough()
        {
            _writeRepository.Result = new[] { 680 };

            await _service.SetEnglishDescriptionAsync(
                6,
                new UpdateProductModelDescriptionDto { Description = "New copy" });

            Assert.That(_writeRepository.LastModelId, Is.EqualTo(6));
            Assert.That(_writeRepository.LastDescription, Is.EqualTo("New copy"));
        }

        [Test]
        public async Task SetEnglishDescriptionAsync_UnknownModel_ReturnsFalseAndEvictsNothing()
        {
            _writeRepository.Result = null;

            var result = await _service.SetEnglishDescriptionAsync(
                999999,
                new UpdateProductModelDescriptionDto { Description = "New copy" });

            Assert.That(result, Is.False);
            Assert.That(_productCacheRepository.RemoveAsyncCalled, Is.False);
        }

        // The whole point of the write path: a description edit is shared, so every product on the
        // model must be evicted from the product cache.
        [Test]
        public async Task SetEnglishDescriptionAsync_Success_EvictsEveryAffectedProductFromCache()
        {
            _writeRepository.Result = new[] { 680, 706, 717 };

            var result = await _service.SetEnglishDescriptionAsync(
                6,
                new UpdateProductModelDescriptionDto { Description = "New copy" });

            Assert.That(result, Is.True);
            Assert.That(_productCacheRepository.RemovedProductIds, Is.EquivalentTo(new[] { 680, 706, 717 }));
        }

        [Test]
        public async Task SetEnglishDescriptionAsync_ModelWithNoProducts_ReturnsTrueAndEvictsNothing()
        {
            _writeRepository.Result = Array.Empty<int>();

            var result = await _service.SetEnglishDescriptionAsync(
                6,
                new UpdateProductModelDescriptionDto { Description = "New copy" });

            Assert.That(result, Is.True);
            Assert.That(_productCacheRepository.RemoveAsyncCalled, Is.False);
        }

        // A cache outage during eviction must not fail the edit — the write already committed.
        [Test]
        public async Task SetEnglishDescriptionAsync_CacheUnavailableDuringEviction_StillReturnsTrue()
        {
            _productCacheRepository.ThrowOnRemove = true;
            _writeRepository.Result = new[] { 680 };

            var result = await _service.SetEnglishDescriptionAsync(
                6,
                new UpdateProductModelDescriptionDto { Description = "New copy" });

            Assert.That(result, Is.True);
        }
    }
}
