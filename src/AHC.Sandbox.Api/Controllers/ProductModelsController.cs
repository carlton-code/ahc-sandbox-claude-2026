using AHC.Sandbox.Application.ProductModels.Dtos;
using AHC.Sandbox.Application.ProductModels.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for reading product models and editing their description.</para>
    /// <para>
    /// A product's marketing description lives on its <c>ProductModel</c>, not on the product row,
    /// and is shared by every product variant on that model. Editing it here — rather than under
    /// <c>/products/{id}</c> — keeps the scope honest: a <c>PUT</c> to a model changes the
    /// description for all of that model's products, and the route says so. The read-only
    /// <c>ProductDto.description</c> on <see cref="ProductsController"/> stays a derived
    /// convenience field.
    /// </para>
    /// <para>
    /// Uses an explicit route rather than the <c>[controller]</c> token: this is the first
    /// multi-word resource, and <c>[controller]</c> would render <c>ProductModels</c> rather than
    /// the hyphenated <c>product-models</c>.
    /// </para>
    /// </summary>
    [ApiController]
    [Route("api/v1/product-models")]
    [Produces("application/json")]
    public class ProductModelsController : ControllerBase
    {
        private readonly IProductModelService _productModelService;

        public ProductModelsController(IProductModelService productModelService)
        {
            _productModelService = productModelService;
        }

        [HttpGet("{modelId:int}")]
        [ProducesResponseType<ProductModelDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductModelDto>> GetProductModelById(int modelId, CancellationToken cancellationToken)
        {
            var model = await _productModelService.GetByIdAsync(modelId, cancellationToken);

            return model is null ? NotFound() : Ok(model);
        }

        /// <summary>
        /// Sets the model's English description (create-or-replace).
        /// </summary>
        /// <remarks>
        /// Affects every product on this model, since they share one description. Creates the
        /// description if the model has none yet. Returns <c>404</c> for an unknown model.
        /// </remarks>
        [HttpPut("{modelId:int}/description")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEnglishDescription(
            int modelId,
            [FromBody] UpdateProductModelDescriptionDto description,
            CancellationToken cancellationToken)
        {
            var updated = await _productModelService.SetEnglishDescriptionAsync(modelId, description, cancellationToken);

            return updated ? NoContent() : NotFound();
        }
    }
}
