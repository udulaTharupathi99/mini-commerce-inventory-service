using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniCommerce.OrderService.Application.Events
{
    public class OrderCancelledEvent
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<OrderItemEvent> Items { get; set; } = new();
    }
}
