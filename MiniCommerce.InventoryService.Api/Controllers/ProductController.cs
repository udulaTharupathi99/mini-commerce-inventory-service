using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCommerce.InventoryService.Application.DTOs;
using MiniCommerce.InventoryService.Application.Interfaces;
using MiniCommerce.InventoryService.Domain.Entities;

namespace MiniCommerce.InventoryService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService service)
        {
            _productService = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }
            
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            return product is null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateUpdateRequest request)
        {
            Stream? imageStream = null;
            string? contentType = null;
            string? fileName = null;

            if (request.Image != null)
            {
                imageStream = request.Image.OpenReadStream();
                contentType = request.Image.ContentType;
                fileName = request.Image.FileName;
            }

            var created = await _productService.CreateAsync(request.Name,request.Description,request.Price,request.Quantity, 
                imageStream,contentType,fileName
            );

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct([FromForm] ProductCreateUpdateRequest request, Guid id)
        {

            Stream? imageStream = null;
            string? contentType = null;
            string? fileName = null;

            if (request.Image != null)
            {
                imageStream = request.Image.OpenReadStream();
                contentType = request.Image.ContentType;
                fileName = request.Image.FileName;
            }

            var updated = await _productService.UpdateAsync(id, request.Name, request.Description, request.Price, request.Quantity,
                imageStream, contentType, fileName);

            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
