using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.InventoryService.Infrastructure.Data;

namespace MiniCommerce.InventoryService.Api.Grpc
{
    public class InventoryGrpcService : InventoryGrpc.InventoryGrpcBase
    {
        private readonly InventoryDbContext _db;

        public InventoryGrpcService(InventoryDbContext db)
        {
            _db = db;
        }

        public override async Task<StockResponse> CheckStock(StockRequest request, ServerCallContext context)
        {
            // validate input
            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                return new StockResponse
                {
                    Available = false,
                    Message = "Invalid Product ID"
                };
            }

            // find product
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, context.CancellationToken);
            if (product is null)
            {
                return new StockResponse
                {
                    Available = false,
                    Message = "Product not found"
                };
            }

            // check quantity
            if (product.Quantity < request.Quantity)
            {
                return new StockResponse
                {
                    Available = false,
                    Message = $"Insufficient stock: {product.Quantity} left"
                };
            }

            // success
            return new StockResponse
            {
                Available = true,
                Message = "Stock available"
            };
        }
    }
}
