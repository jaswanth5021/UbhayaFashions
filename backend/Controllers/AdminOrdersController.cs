using backend.Data;using backend.DTOs;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace backend.Controllers;
[ApiController][Route("api/admin/orders")][Authorize(Roles="Admin")]
public class AdminOrdersController(ApplicationDbContext db):ControllerBase{
 [HttpGet]public async Task<IActionResult>All()=>Ok(await db.Orders.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Items).OrderByDescending(x=>x.CreatedDate).ToListAsync());
 [HttpGet("{id:int}")]public async Task<IActionResult>Get(int id){var o=await db.Orders.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);return o is null?NotFound():Ok(o);}
 [HttpPut("{id:int}/status")]public async Task<IActionResult>Status(int id,AdminOrderStatusRequest r){var o=await db.Orders.FindAsync(id);if(o is null)return NotFound();o.Status=r.Status;if(!string.IsNullOrWhiteSpace(r.PaymentStatus))o.PaymentStatus=r.PaymentStatus;await db.SaveChangesAsync();return Ok(o);}
}
