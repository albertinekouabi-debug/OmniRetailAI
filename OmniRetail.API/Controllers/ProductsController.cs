using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;

namespace OmniRetail.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔐 sécurité globale SaaS
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    //
    // GET: api/products
    //
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId)
    {
        var products = await _productService
            .GetAllProducts(search, categoryId);

        return Ok(products);
    }

    //
    // GET: api/products/{id}
    //
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService
            .GetProductById(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    //
    // POST: api/products
    // ADMIN ONLY
    //
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await _productService
            .CreateProduct(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    //
    // PUT: api/products/{id}
    // ADMIN ONLY
    //
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _productService
            .UpdateProduct(id, request);

        if (updated == null)
            return NotFound();

        return Ok(updated);
    }

    //
    // DELETE: api/products/{id}
    // ADMIN ONLY
    //
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteProduct(id);

        return NoContent();
    }
}