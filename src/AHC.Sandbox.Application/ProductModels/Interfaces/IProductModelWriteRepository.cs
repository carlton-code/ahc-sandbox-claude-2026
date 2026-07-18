namespace AHC.Sandbox.Application.ProductModels.Interfaces
{
    public interface IProductModelWriteRepository
    {
        /// <summary>
        /// Sets the model's English description, creating the description row if the model has none
        /// yet (upsert). Returns the ids of the products on this model — whose cached reads are now
        /// stale and must be evicted — or <c>null</c> if no model with that id exists.
        /// </summary>
        Task<IReadOnlyCollection<int>?> SetEnglishDescriptionAsync(
            int modelId,
            string description,
            CancellationToken cancellationToken = default);
    }
}
