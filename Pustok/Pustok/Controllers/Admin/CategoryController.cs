using Microsoft.AspNetCore.Mvc;
using Pustok.Database;
using System.Linq;

namespace Pustok.Controllers.Admin;

[Route("admin/categories")]
public class CategoryController : Controller
{
    private readonly PustokDbContext _pustokDbContext;

    public CategoryController(PustokDbContext pustokDbContext)
    {
        _pustokDbContext = pustokDbContext;
    }

    [HttpGet]
    public IActionResult Colors()
    {
        var categories = _pustokDbContext.Categories.ToList();

        return View("Views/Admin/Category/Categories.cshtml", categories);
    }
}