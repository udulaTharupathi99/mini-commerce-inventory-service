using Microsoft.EntityFrameworkCore;
using MiniCommerce.InventoryService.Domain.Entities;
using MiniCommerce.InventoryService.Domain.Interfaces;
using MiniCommerce.InventoryService.Infrastructure.Data;

namespace MiniCommerce.InventoryService.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly InventoryDbContext _db;
        public ProductRepository(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _db.Products.AsNoTracking().ToListAsync();

        public async Task<Product?> GetByIdAsync(Guid id)
            => await _db.Products.FindAsync(new object[] { id });

        public async Task AddAsync(Product product)
            => await _db.Products.AddAsync(product);

        public async Task UpdateAsync(Product product)
            => _db.Products.Update(product);

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _db.Products.FindAsync(new object[] { id });
            if (existing != null)
            {
                _db.Products.Remove(existing);
                return true;
            }else
                return false;       
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}
