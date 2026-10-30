
using System.Security.Claims;
using FinTrack.Application.Categories.DTOs;
using FinTrack.Application.Categories.Interfaces;
using FinTrack.Web.ViewModels.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCategoryViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new CreateCategoryRequest(
                model.Name,
                model.Type);

            await _categoryService.CreateAsync(
                CurrentUserId,
                request,
                cancellationToken);

            TempData["SuccessMessage"] =
                "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View(model);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (category is null)
            return NotFound();

        var model = new EditCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditCategoryViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var updated = await _categoryService.RenameAsync(
                CurrentUserId,
                id,
                model.Name,
                cancellationToken);

            if (!updated)
                return NotFound();

            TempData["SuccessMessage"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View(model);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(model.Name), ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        var archived = await _categoryService.ArchiveAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (!archived)
            return NotFound();

        TempData["SuccessMessage"] =
            "Category archived successfully.";

        return RedirectToAction(nameof(Index));
    }
}
