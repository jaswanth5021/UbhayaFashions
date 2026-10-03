using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "Admin")]
public class AdminAccountsController(ApplicationDbContext db) : ControllerBase
{
    private bool IsOwner => string.Equals(
        User.FindFirstValue(ClaimTypes.Email), "admin@ubhaya.com", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsOwner) return Forbid();
        var accounts = await db.AdminUsers.AsNoTracking()
            .OrderBy(account => account.Name)
            .Select(account => new { account.Id, account.Name, account.Email, account.IsActive })
            .ToListAsync();
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminAccountRequest request)
    {
        if (!IsOwner) return Forbid();
        var validation = Validate(request, requirePassword: true);
        if (validation is not null) return BadRequest(validation);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.AdminUsers.AnyAsync(account => account.Email == email))
            return Conflict("An admin account with this email already exists.");

        var account = new AdminUser
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = request.IsActive
        };
        db.AdminUsers.Add(account);
        await db.SaveChangesAsync();
        return Created("api/admin/accounts", new { account.Id, account.Name, account.Email, account.IsActive });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdminAccountRequest request)
    {
        if (!IsOwner) return Forbid();
        if (request.Id != 0 && request.Id != id) return BadRequest("Account ID does not match the request.");
        var validation = Validate(request, requirePassword: false);
        if (validation is not null) return BadRequest(validation);
        var account = await db.AdminUsers.FirstOrDefaultAsync(item => item.Id == id);
        if (account is null) return NotFound();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.AdminUsers.AnyAsync(item => item.Id != id && item.Email == email))
            return Conflict("An admin account with this email already exists.");
        if (string.Equals(account.Email, "admin@ubhaya.com", StringComparison.OrdinalIgnoreCase)
            && (!request.IsActive || !string.Equals(email, account.Email, StringComparison.OrdinalIgnoreCase)))
            return BadRequest("The owner account must remain active and keep its email address.");

        account.Name = request.Name.Trim();
        account.Email = email;
        account.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsOwner) return Forbid();
        var account = await db.AdminUsers.FirstOrDefaultAsync(item => item.Id == id);
        if (account is null) return NotFound();
        if (string.Equals(account.Email, "admin@ubhaya.com", StringComparison.OrdinalIgnoreCase))
            return BadRequest("The owner account cannot be deleted.");
        db.AdminUsers.Remove(account);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Validate(AdminAccountRequest request, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return "Name is required and must be 100 characters or fewer.";
        if (!System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out var address)
            || !string.Equals(address.Address, request.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            return "Enter a valid email address.";
        if ((requirePassword || !string.IsNullOrWhiteSpace(request.Password))
            && string.IsNullOrWhiteSpace(request.Password))
            return "Password is required when creating an admin account.";
        return null;
    }
}
