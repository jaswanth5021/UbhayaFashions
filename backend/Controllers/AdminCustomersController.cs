using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "Admin")]
public class AdminCustomersController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
    {
        var customers = await db.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.Email,
                customer.Mobile,
                customer.DateOfBirth,
                OrderCount = customer.Orders.Count(),
                LastOrderDate = customer.Orders
                    .Select(order => (DateTime?)order.CreatedDate)
                    .Max()
            })
            .ToListAsync();

        return Ok(customers);
    }
}
