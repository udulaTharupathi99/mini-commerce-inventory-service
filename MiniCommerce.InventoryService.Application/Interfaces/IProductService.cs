using MiniCommerce.InventoryService.Application.DTOs;
using MiniCommerce.InventoryService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCommerce.InventoryService.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<ProductDto> CreateAsync(string name, string description, decimal price, int quantity, Stream? image, string? contentType, string? fileName);
        Task<ProductDto?> UpdateAsync(Guid id, string name, string description, decimal price, int quantity, Stream? image, string? contentType, string? fileName);
        Task<bool> DeleteAsync(Guid id);
    }
}
