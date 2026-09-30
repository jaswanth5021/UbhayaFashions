using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var variants = await db.ProductVariants.AsNoTracking()
            .OrderBy(variant => variant.Stock)
            .Select(variant => new
            {
                Id = variant.ProductId,
                variant.Product.Name,
                Category = variant.Product.CategoryNavigation.Name,
                variant.Size,
                variant.Stock,
                variant.Price,
                variant.Product.ImageUrl
            }).ToListAsync();
        return Ok(variants);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions()
        => Ok(await db.InventoryTransactions.AsNoTracking().Include(x => x.Product).OrderByDescending(x => x.CreatedDate).Take(200).ToListAsync());

    [HttpPost("{productId:int}/adjust")]
    public async Task<IActionResult> Adjust(int productId, InventoryAdjustmentRequest request)
    {
        var variant = await db.ProductVariants.FirstOrDefaultAsync(item =>
            item.ProductId == productId && item.Size == request.Size);
        if (variant is null) return NotFound("Product size variant not found.");

        var newStock = variant.Stock + request.Quantity;
        if (newStock < 0) return BadRequest("Stock cannot be negative.");
        variant.Stock = newStock;
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = variant.ProductId,
            Size = variant.Size,
            Type = request.Type,
            Quantity = request.Quantity,
            StockAfter = newStock,
            Note = request.Note
        });
        await db.SaveChangesAsync();
        return Ok(new { variant.ProductId, variant.Size, variant.Stock });
    }
}
