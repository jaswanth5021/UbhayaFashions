using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

[Authorize]
public class AddressesController(ApiService api, IHttpClientFactory httpClientFactory) : Controller
{

    [HttpGet]
    public async Task<IActionResult> ReverseGeocode(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            return BadRequest(new { success = false, message = "Invalid location coordinates." });

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("UbhayaFashions/1.0");
            var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&zoom=18&addressdetails=1";
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(new { success = false, message = "Could not find an address for your location." });

            using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var address = root.TryGetProperty("address", out var a) ? a : default;
            string Get(string name) => address.ValueKind == System.Text.Json.JsonValueKind.Object && address.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

            return Json(new
            {
                success = true,
                address = new
                {
                    houseNumber = Get("house_number"),
                    road = Get("road"),
                    suburb = Get("suburb"),
                    neighbourhood = Get("neighbourhood"),
                    quarter = Get("quarter"),
                    city = Get("city").Length > 0 ? Get("city") : Get("town").Length > 0 ? Get("town") : Get("village"),
                    state = Get("state"),
                    postcode = Get("postcode"),
                    display_name = root.TryGetProperty("display_name", out var display) ? display.GetString() ?? "" : ""
                }
            });
        }
        catch
        {
            return BadRequest(new { success = false, message = "Could not load your address right now." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveAddressViewModel model)
    {
        var result = await api.SaveAddressAsync(model);
        if (!result.Success || result.Data is null)
            return BadRequest(new { success = false, message = result.Message });

        return Json(new { success = true, address = result.Data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await api.DeleteAddressAsync(id);
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });
        return Json(new { success = true });
    }
}
