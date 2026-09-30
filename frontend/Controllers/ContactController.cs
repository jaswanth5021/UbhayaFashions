using Microsoft.AspNetCore.Mvc;
using LadiesDressStore.Web.Models;

namespace LadiesDressStore.Web.Controllers;

public class ContactController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // For now we will just show a success message.
        // Later we can save this to the database
        // or send it to your business email.

        TempData["ContactSuccess"] =
            "Thank you for contacting Ubhaya Fashions. We will get back to you soon.";

        return RedirectToAction(nameof(Index));
    }
}