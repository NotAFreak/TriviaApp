using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TriviaApp.Models;

namespace TriviaApp.Controllers;

[Authorize(Roles = nameof(Role.Admin))]
public class AdminController : Controller {
    private TriviaContext db;

    public AdminController(TriviaContext db) {
        this.db = db;
    }
    [HttpGet]
    public async Task<IActionResult> Index() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
