using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressesController(ApplicationDbContext db) : ControllerBase
{
    private bool TryGetCustomerId(out int customerId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();

        var savedAddresses = await db.CustomerAddresses.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedDate)
            .ToListAsync();

        // Format address text after materialization: the string array join is
        // not consistently translated by EF Core SQL Server providers.
        var addresses = savedAddresses.Select(x => new
        {
            x.Id,
            x.Name,
            x.Mobile,
            x.AddressLine1,
            x.AddressLine2,
            x.City,
            x.State,
            x.PostalCode,
            x.Country,
            x.IsDefault,
            FullAddress = string.Join(", ", new[]
            {
                x.AddressLine1,
                x.AddressLine2,
                x.City,
                x.State,
                x.PostalCode,
                x.Country
            }.Where(value => !string.IsNullOrWhiteSpace(value)))
        });

        return Ok(addresses);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAddressRequest request)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        var validation = Validate(request.Name, request.Mobile, request.AddressLine1, request.City, request.State, request.PostalCode);
        if (validation is not null) return BadRequest(validation);

        var hasAddresses = await db.CustomerAddresses.AnyAsync(x => x.CustomerId == customerId);
        var address = new CustomerAddress
        {
            CustomerId = customerId,
            Name = request.Name.Trim(),
            Mobile = request.Mobile.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim(),
            IsDefault = request.IsDefault || !hasAddresses
        };

        if (address.IsDefault)
            await ClearDefault(customerId);

        db.CustomerAddresses.Add(address);
        await db.SaveChangesAsync();
        return Ok(await ToResponse(address));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAddressRequest request)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        var validation = Validate(request.Name, request.Mobile, request.AddressLine1, request.City, request.State, request.PostalCode);
        if (validation is not null) return BadRequest(validation);

        var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
        if (address is null) return NotFound("Address not found.");

        address.Name = request.Name.Trim();
        address.Mobile = request.Mobile.Trim();
        address.AddressLine1 = request.AddressLine1.Trim();
        address.AddressLine2 = request.AddressLine2?.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country.Trim();
        address.IsDefault = request.IsDefault;

        if (address.IsDefault)
            await ClearDefault(customerId, address.Id);

        await db.SaveChangesAsync();
        return Ok(await ToResponse(address));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        var address = await db.CustomerAddresses.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);
        if (address is null) return NotFound("Address not found.");

        db.CustomerAddresses.Remove(address);
        await db.SaveChangesAsync();

        if (address.IsDefault)
        {
            var replacement = await db.CustomerAddresses
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();
            if (replacement is not null)
            {
                replacement.IsDefault = true;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { success = true });
    }

    private async Task ClearDefault(int customerId, int? exceptId = null)
    {
        var current = await db.CustomerAddresses
            .Where(x => x.CustomerId == customerId && (!exceptId.HasValue || x.Id != exceptId.Value) && x.IsDefault)
            .ToListAsync();
        foreach (var item in current) item.IsDefault = false;
    }

    private static string? Validate(string name, string mobile, string address, string city, string state, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(mobile)) return "Mobile number is required.";
        if (string.IsNullOrWhiteSpace(address)) return "Address is required.";
        if (string.IsNullOrWhiteSpace(city)) return "City is required.";
        if (string.IsNullOrWhiteSpace(state)) return "State is required.";
        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length < 5) return "A valid PIN/postal code is required.";
        return null;
    }

    private static Task<object> ToResponse(CustomerAddress x) => Task.FromResult<object>(new
    {
        x.Id,
        x.Name,
        x.Mobile,
        x.AddressLine1,
        x.AddressLine2,
        x.City,
        x.State,
        x.PostalCode,
        x.Country,
        x.IsDefault,
        FullAddress = string.Join(", ", new[] { x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value)))
    });
}
