using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for managing products.</para>
    /// <para>
    /// Follows the same conventions as <see cref="CustomersController"/>: actions with a body
    /// return <c>ActionResult&lt;T&gt;</c>, body-less mutations return <c>IActionResult</c>, and
    /// every reachable status — including the success one — is declared with
    /// <c>[ProducesResponseType]</c>, because adding any of those attributes replaces the
    /// framework's inferred 200 rather than adding to it.
    /// </para>
    /// <para>
    /// Unlike <c>Customer</c>, <c>SalesLT.Product</c> has unique constraints beyond its key
    /// (<c>Name</c> and <c>ProductNumber</c>) and nullable foreign keys
    /// (<c>ProductCategoryID</c>/<c>ProductModelID</c>), so <c>POST</c> and <c>PUT</c> declare
    /// <c>409</c> as well — produced by <c>DatabaseConflictExceptionHandler</c>, never by these
    /// actions.
    /// </para>
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyCollection<ProductDto>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> GetProducts(CancellationToken cancellationToken)
        {
            var products = await _productService.GetProductsAsync(cancellationToken);

            return Ok(products);
        }

        [HttpGet("{productId:int}")]
        [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProductById(int productId, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductByIdAsync(productId, cancellationToken);

            return product is null ? NotFound() : Ok(product);
        }

        /// <summary>
        /// Creates a product.
        /// </summary>
        /// <remarks>
        /// Returns <c>409</c> when the request collides with a database constraint: a duplicate
        /// <c>Name</c> or <c>ProductNumber</c> (both unique), a <c>ProductCategoryId</c>/
        /// <c>ProductModelId</c> that doesn't exist, or a <c>SellEndDate</c> earlier than
        /// <c>SellStartDate</c> (a CHECK constraint — cross-field, so it isn't caught by the DTO's
        /// validation attributes).
        /// </remarks>
        [HttpPost]
        [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto product, CancellationToken cancellationToken)
        {
            var createdProduct = await _productService.CreateProductAsync(product, cancellationToken);

            return CreatedAtAction(
                nameof(GetProductById),
                new { productId = createdProduct.ProductId },
                createdProduct);
        }

        /// <summary>
        /// Replaces a product. Subject to the same <c>409</c> sources as create.
        /// </summary>
        [HttpPut("{productId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProduct(
            int productId,
            [FromBody] UpdateProductDto product,
            CancellationToken cancellationToken)
        {
            var updated = await _productService.UpdateProductAsync(productId, product, cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes a product.
        /// </summary>
        /// <remarks>
        /// Returns <c>409</c> when anything still references the product — an order line
        /// (<c>SalesLT.SalesOrderDetail</c>) or the <c>SalesIntelligence</c> bundle/recommendation
        /// tables. Most seeded products are referenced, so <c>204</c> is realistic mainly for
        /// products created through this API.
        /// </remarks>
        [HttpDelete("{productId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteProduct(int productId, CancellationToken cancellationToken)
        {
            var deleted = await _productService.DeleteProductAsync(productId, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
    }
}
