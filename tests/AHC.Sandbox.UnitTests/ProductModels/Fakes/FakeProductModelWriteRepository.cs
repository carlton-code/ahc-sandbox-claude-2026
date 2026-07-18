using AHC.Sandbox.Application.ProductModels.Interfaces;

namespace AHC.Sandbox.UnitTests.ProductModels.Fakes
{
    // Minimal in-memory fake for IProductModelWriteRepository. Result defaults to an empty set of
    // affected products (model exists, nothing to evict); set it to null to simulate an unknown
    // model, or to a list to drive cache-eviction assertions.
    public class FakeProductModelWriteRepository : IProductModelWriteRepository
    {
        public IReadOnlyCollection<int>? Result { get; set; } = Array.Empty<int>();

        public int? LastModelId { get; private set; }
        public string? LastDescription { get; private set; }
        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<int>?> SetEnglishDescriptionAsync(
            int modelId,
            string description,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastModelId = modelId;
            LastDescription = description;
            return Task.FromResult(Result);
        }
    }
}
