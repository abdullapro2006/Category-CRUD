﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pustok.Database;
using Pustok.Database.DomainModels;
using Pustok.ViewModels.Product;
using System.Linq;

namespace Pustok.Controllers.Admin;

[Route("admin/products")]
public class ProductController : Controller
{
    private readonly PustokDbContext _pustokDbContext;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        PustokDbContext pustokDbContext,
        ILogger<ProductController> logger)
    {
        _pustokDbContext = pustokDbContext;
        _logger = logger;
    }

    #region Products

    [HttpGet] //admin/products
    public IActionResult Products()
    {
        var products = _pustokDbContext.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
            .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.Color)
            .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .ToList();


        return View("Views/Admin/Product/Products.cshtml", products);
    }

    #endregion

    #region Add

    [HttpGet("add")]
    public IActionResult Add()
    {
        var model = new ProductAddResponseViewModel
        {
            Categories = _pustokDbContext.Categories.ToList(),
            Colors = _pustokDbContext.Colors.ToList(),
            Sizes = _pustokDbContext.Sizes.ToList(),
        };

        return View("Views/Admin/Product/ProductAdd.cshtml", model);
    }

    [HttpPost("add")]
    public IActionResult Add(ProductAddRequestViewModel model)
    {
        if (!ModelState.IsValid)
            return PrepareValidationView("Views/Admin/Product/ProductAdd.cshtml");

        if (model.CategoryId != null)
        {
            var category = _pustokDbContext.Categories.FirstOrDefault(c => c.Id == model.CategoryId.Value);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Category doesn't exist");

                return PrepareValidationView("Views/Admin/Product/ProductAdd.cshtml");
            }
        }

        try
        {
            var product = new Product
            {
                Name = model.Name,
                Price = model.Price,
                Rating = model.Rating,
                CategoryId = model.CategoryId,
            };

            _pustokDbContext.Products.Add(product);

            foreach (var colorId in model.SelectedColorIds)
            {
                var color = _pustokDbContext.Colors.FirstOrDefault(c => c.Id == colorId);
                if (color == null)
                {
                    ModelState.AddModelError("SelectedColorIds", "Color doesn't exist");

                    return PrepareValidationView("Views/Admin/Product/ProductAdd.cshtml");
                }


                var productColor = new ProductColor
                {
                    ColorId = color.Id,
                    Product = product
                };

                _pustokDbContext.ProductColors.Add(productColor);
            }


            

            foreach (var sizeId in model.SelectedSizeIds)
            {
                var size = _pustokDbContext.Sizes.FirstOrDefault(c => c.Id == sizeId);
                if (size == null)
                {
                    ModelState.AddModelError("SelectedSizeIds", "Size doesn't exist");

                    return PrepareValidationView("Views/Admin/Product/ProductAdd.cshtml");
                }


                var productSize = new ProductSize
                {
                    Size = size,
                    Product = product
                };

                _pustokDbContext.ProductSizes.Add(productSize);
            }

            _pustokDbContext.SaveChanges();

        }
        catch (PostgresException e)
        {
            _logger.LogError(e, "Postgresql Exception");

            throw e;
        }

        return RedirectToAction("Products");
    }

    #endregion

    #region Edit

    [HttpGet("edit")]
    public IActionResult Edit(int id)
    {
        Product product = _pustokDbContext.Products
            .Include(p => p.ProductColors)
            .Include(p => p.ProductSizes)
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        var model = new ProductUpdateResponseViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Rating = product.Rating,
            Categories = _pustokDbContext.Categories.ToList(),
            CategoryId = product.CategoryId,
            SelectedColorIds = product.ProductColors.Select(pc => pc.ColorId).ToArray(),
            Colors = _pustokDbContext.Colors.ToList(),
            SelectedSizeIds = product.ProductSizes.Select(ps => ps.SizeId).ToArray(),
            Sizes = _pustokDbContext.Sizes.ToList()
        };

        return View("Views/Admin/Product/ProductEdit.cshtml", model);
    }

    [HttpPost("edit")]
    public IActionResult Edit(ProductUpdateRequestViewModel model)
    {
        if (!ModelState.IsValid)
            return PrepareValidationView("Views/Admin/Product/ProductEdit.cshtml");

        if (model.CategoryId != null)
        {
            var category = _pustokDbContext.Categories.FirstOrDefault(c => c.Id == model.CategoryId.Value);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Category doesn't exist");

                return PrepareValidationView("Views/Admin/Product/ProductAdd.cshtml");
            }
        }

        Product product = _pustokDbContext.Products
            .Include(p => p.ProductColors)
            .Include(p => p.ProductSizes)
            .FirstOrDefault(p => p.Id == model.Id);

        if (product == null)
            return NotFound();

        #region Product color

        var productColorIdsInDb = product.ProductColors.Select(pc => pc.ColorId);

        //Remove proces ========================================

        var removableColorIds = productColorIdsInDb
            .Where(id => !model.SelectedColorIds.Contains(id))
            .ToList();

        product.ProductColors.RemoveAll(pc => removableColorIds.Contains(pc.ColorId));


        //Add proces ========================================

        var addableColorIds = model.SelectedColorIds
            .Where(id => !productColorIdsInDb.Contains(id))
            .ToList();

        var newProductColors = addableColorIds.Select(colorId => new ProductColor
        {
            ColorId = colorId,
            Product = product
        });

        product.ProductColors.AddRange(newProductColors);

        #endregion

        #region Product size

        var productSizeIdsInDb = product.ProductSizes.Select(pc => pc.SizeId);

        //Remove proces ========================================

        var removableSizeIds = productSizeIdsInDb
            .Where(id => !model.SelectedSizeIds.Contains(id))
            .ToList();

        product.ProductSizes.RemoveAll(pc => removableSizeIds.Contains(pc.SizeId));


        //Add proces ========================================

        var addableSizeIds = model.SelectedSizeIds
            .Where(id => !productSizeIdsInDb.Contains(id))
            .ToList();

        var newProductSize = addableSizeIds.Select(sizeId => new ProductSize
        {
            SizeId = sizeId,
            Product = product
        });

        product.ProductSizes.AddRange(newProductSize);

        #endregion

        try
        {
            product.Name = model.Name;
            product.Price = model.Price;
            product.Rating = model.Rating;
            product.CategoryId = model.CategoryId;

            _pustokDbContext.SaveChanges();
        }
        catch (PostgresException e)
        {
            _logger.LogError(e, "Postgresql Exception");

            throw e;
        }


        return RedirectToAction("Products");
    }

    #endregion

    #region Delete

    [HttpGet("delete")]
    public IActionResult Delete(int id)
    {
        Product product = _pustokDbContext.Products
            .FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        _pustokDbContext.Remove(product);
        _pustokDbContext.SaveChanges();


        return RedirectToAction("Products");
    }

    #endregion

    private IActionResult PrepareValidationView(string viewName)
    {
        var responseViewModel = new ProductAddResponseViewModel
        {
            Categories = _pustokDbContext.Categories.ToList(),
            Colors = _pustokDbContext.Colors.ToList(),
            Sizes = _pustokDbContext.Sizes.ToList(),
        };

        return View(viewName, responseViewModel);
    }
}
