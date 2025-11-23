using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniCommerce.InventoryService.Domain.Entities;
using MiniCommerce.InventoryService.Infrastructure.Data;
using MiniCommerce.OrderService.Application.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniCommerce.InventoryService.Infrastructure.Messaging
{
    public class OrderEventsConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly string _host = "localhost";

        public OrderEventsConsumer(IServiceProvider services)
        {
            _services = services;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run consumer on background thread
            Task.Run(() => StartConsuming("order.created", HandleOrderCreated, stoppingToken), stoppingToken);
            Task.Run(() => StartConsuming("order.cancelled", HandleOrderCancelled, stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private void StartConsuming(string queueName, Func<string, Task> handler, CancellationToken ct)
        {
            var factory = new ConnectionFactory() { HostName = _host };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                await handler(json);
            };

            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        //Reserve items when order created
        private async Task HandleOrderCreated(string json)
        {
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);
            if (evt == null) return;

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            foreach (var item in evt.Items)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null) continue;

                if (product.Quantity >= item.Quantity)
                {
                    product.Quantity -= item.Quantity;

                    db.ReservedItems.Add(new ReservedItem
                    {
                        OrderId = evt.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
                }
                else
                {
                    Console.WriteLine($"Not enough stock for {product.Name}");
                }
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"Order {evt.OrderId}: Reserved stock successfully");
        }

        //Release items when order cancelled
        private async Task HandleOrderCancelled(string json)
        {
            var evt = JsonSerializer.Deserialize<OrderCancelledEvent>(json);
            if (evt == null) return;

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var reservedItems = await db.ReservedItems
                .Where(r => r.OrderId == evt.OrderId)
                .ToListAsync();

            if (reservedItems.Any())
            {
                foreach (var reserved in reservedItems)
                {
                    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == reserved.ProductId);
                    if (product != null)
                    {
                        product.Quantity += reserved.Quantity; // release stock
                    }
                }

                db.ReservedItems.RemoveRange(reservedItems);
                await db.SaveChangesAsync();
                Console.WriteLine($"Order {evt.OrderId}: Released reserved stock");
            }
            else
            {
                Console.WriteLine($"No reserved stock found for cancelled order {evt.OrderId}");
            }
        }
    }
}
