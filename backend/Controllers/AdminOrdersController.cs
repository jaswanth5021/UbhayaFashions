using backend.Data;
using backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedDate)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                Customer = order.Customer == null ? null : new
                {
                    order.Customer.Id,
                    order.Customer.Name,
                    order.Customer.Email,
                    order.Customer.Mobile,
                    order.Customer.DateOfBirth
                },
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                Items = order.Items.Select(item => new
                {
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                Customer = order.Customer == null ? null : new
                {
                    order.Customer.Id,
                    order.Customer.Name,
                    order.Customer.Email,
                    order.Customer.Mobile,
                    order.Customer.DateOfBirth
                },
                order.TotalAmount,
                order.Status,
                order.PaymentStatus,
                order.ShippingAddress,
                order.CreatedDate,
                Items = order.Items.Select(item => new
                {
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.Size,
                    item.Color
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> Status(int id, AdminOrderStatusRequest request)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return NotFound();

        order.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            order.PaymentStatus = request.PaymentStatus;

        await db.SaveChangesAsync();

        return Ok(new { order.Id, order.Status, order.PaymentStatus });
    }
}
