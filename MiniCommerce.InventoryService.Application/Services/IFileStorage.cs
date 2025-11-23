using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCommerce.InventoryService.Application.Services
{
    public interface IFileStorage
    {
        Task<string> UploadAsync(Stream fileStream, string contentType, string fileName);
    }
}
