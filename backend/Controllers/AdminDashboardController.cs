using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var totalProducts = await db.Products.CountAsync();
        var totalOrders = await db.Orders.CountAsync();
        var pendingOrders = await db.Orders.CountAsync(order => order.Status == "Pending");
        var lowStock = await db.ProductVariants.CountAsync(variant => variant.Stock <= 5);
        var customers = await db.Customers.CountAsync();
        var revenue = await db.Orders.Where(order => order.PaymentStatus == "Paid" || order.Status == "Delivered")
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        return Ok(new { totalProducts, totalOrders, pendingOrders, lowStock, customers, revenue });
    }
}
