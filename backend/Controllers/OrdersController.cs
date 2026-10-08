using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(ApplicationDbContext db, IOrderConfirmationEmailService orderEmail) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return Unauthorized();
        if (request.Items is null || request.Items.Count == 0) return BadRequest("Cart is empty.");
        if (request.Items.Any(item => item.Quantity <= 0 || string.IsNullOrWhiteSpace(item.Size))) return BadRequest("Choose a size and a valid quantity for each item.");

        var ids = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Products.Include(product => product.Variants).Where(product => ids.Contains(product.Id)).ToListAsync();
        if (products.Count != ids.Count) return BadRequest("One or more products no longer exist.");

        decimal total = 0;
        var inventoryTransactions = new List<InventoryTransaction>();
        var order = new Order { CustomerId = customerId, ShippingAddress = request.ShippingAddress };

        foreach (var item in request.Items)
        {
            var product = products.Single(product => product.Id == item.ProductId);
            var variant = product.Variants.FirstOrDefault(variant => variant.Size == item.Size);
            if (variant is null) return BadRequest($"{product.Name} does not have size {item.Size}.");
            if (variant.Stock < item.Quantity) return BadRequest($"{product.Name} ({variant.Size}) has insufficient stock.");

            total += variant.Price * item.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = variant.Price,
                Quantity = item.Quantity,
                Size = variant.Size,
                Color = item.Color
            });
            variant.Stock -= item.Quantity;
            inventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                Size = variant.Size,
                Type = "Sale",
                Quantity = -item.Quantity,
                StockAfter = variant.Stock,
                Note = "Customer order"
            });
        }

        order.TotalAmount = total;
        db.Orders.Add(order);
        db.InventoryTransactions.AddRange(inventoryTransactions);
        await db.SaveChangesAsync();
        await orderEmail.SendOrderConfirmationAsync(order, customer);
        return Ok(order);
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyOrders()
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await db.Orders.AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedDate)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                order.RazorpayOrderId,
                Items = order.Items.Select(item => new
                {
                    item.Id,
                    item.OrderId,
                    item.ProductId,
                    item.ProductName,
                    ImageUrl = db.Products
                        .Where(product => product.Id == item.ProductId)
                        .Select(product => product.ImageUrl)
                        .FirstOrDefault(),
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                })
            })
            .ToListAsync();
        return Ok(orders);
    }
}
