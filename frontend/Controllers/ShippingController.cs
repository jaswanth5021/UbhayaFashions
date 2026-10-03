using Microsoft.AspNetCore.Mvc;

namespace LadiesDressStore.Web.Controllers;

public class ShippingController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}