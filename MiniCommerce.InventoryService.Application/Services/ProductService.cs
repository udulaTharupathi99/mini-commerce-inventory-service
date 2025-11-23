using AutoMapper;
using MiniCommerce.InventoryService.Application.DTOs;
using MiniCommerce.InventoryService.Application.Interfaces;
using MiniCommerce.InventoryService.Domain.Entities;
using MiniCommerce.InventoryService.Domain.Interfaces;

namespace MiniCommerce.InventoryService.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IFileStorage _storage;
        private readonly IMapper _mapper;
        public ProductService(IProductRepository repo, IFileStorage storage, IMapper mapper)
        {
            _repo = repo;
            _storage = storage;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var product =  await _repo.GetByIdAsync(id);
            return _mapper.Map<ProductDto>(product);
        }
           

        public async Task<ProductDto> CreateAsync(string name, string description, decimal price, int quantity, 
            Stream? imageStream, string? contentType, string? fileName)
        {
            string? imageUrl = null;

            if (imageStream != null)
                imageUrl = await _storage.UploadAsync(imageStream, contentType!, fileName!);

            var product = new Product
            {
                Name = name,
                Description = description,
                Price = price,
                Quantity = quantity,
                ImageUrl = imageUrl
            };

            await _repo.AddAsync(product);
            await _repo.SaveChangesAsync();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> UpdateAsync(Guid id, string name, string description, decimal price, int quantity, 
            Stream? imageStream, string? contentType, string? fileName)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            string? imageUrl = null;

            if (imageStream != null)
                imageUrl = await _storage.UploadAsync(imageStream, contentType!, fileName!);

            existing.Name = name;
            existing.Description = description;
            existing.Price = price;
            existing.Quantity = quantity;
            existing.ImageUrl = imageUrl ?? existing.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(existing);
            await _repo.SaveChangesAsync();

            return _mapper.Map<ProductDto>(existing); ;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var deleted = await _repo.DeleteAsync(id);
            if (!deleted) return false;
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
