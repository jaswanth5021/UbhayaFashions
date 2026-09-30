using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/categories")]
[AllowAnonymous]
public class CategoriesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> All()
        => Ok(await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync());

    [HttpGet("home")]
    public async Task<IActionResult> Home()
        => Ok(await db.Categories.AsNoTracking().Where(category => category.ShowOnHomePage).OrderBy(category => category.Name).Take(4).ToListAsync());
}
