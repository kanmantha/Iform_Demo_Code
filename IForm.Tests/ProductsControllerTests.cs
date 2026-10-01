using System.IO;
using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class ProductsControllerTests
{
    private static IFormFile ImageFile(string fileName, byte[]? content = null)
        => new FormFile(new MemoryStream(content ?? new byte[] { 1, 2, 3, 4 }), 0, (content ?? new byte[] { 1, 2, 3, 4 }).Length, "ImageFile", fileName);

    [Fact]
    public async Task Create_UploadsImage_AndPersistsImagePath()
    {
        var (db, connection) = TestData.CreateDbContext();
        var env = new TestWebHostEnvironment();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new ProductsController(db, TestData.UserManager(user), env)
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new ProductFormViewModel
            {
                ProductCode = "DAAA",
                Name = "SNAP TIE",
                Family = "Tie",
                Material = "Steel",
                IsActive = true,
                ImageFile = ImageFile("snap-tie.png")
            });

            Assert.IsType<RedirectToActionResult>(result);
            var saved = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductCode == "DAAA");
            Assert.NotNull(saved);
            Assert.NotNull(saved.ImagePath);
            Assert.StartsWith("/uploads/products/", saved.ImagePath);
            Assert.True(File.Exists(Path.Combine(env.WebRootPath, saved.ImagePath!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
            DeleteUploadsFolder(env);
        }
    }

    [Fact]
    public async Task Create_InvalidImageExtension_ReturnsView_AndDoesNotPersist()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new ProductsController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new ProductFormViewModel
            {
                ProductCode = "DAAA",
                Name = "SNAP TIE",
                IsActive = true,
                ImageFile = ImageFile("snap-tie.exe")
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.Products);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_ReplacesImage_AndDeletesOldFile()
    {
        var (db, connection) = TestData.CreateDbContext();
        var env = new TestWebHostEnvironment();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var product = new Product
            {
                ProductCode = "DAAA",
                Name = "SNAP TIE",
                ImagePath = "/uploads/products/old-image.png",
                IsActive = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var oldFullPath = Path.Combine(env.WebRootPath, "uploads", "products", "old-image.png");
            Directory.CreateDirectory(Path.GetDirectoryName(oldFullPath)!);
            File.WriteAllBytes(oldFullPath, new byte[] { 1, 2, 3 });
            Assert.True(File.Exists(oldFullPath));

            var controller = new ProductsController(db, TestData.UserManager(user), env)
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new ProductFormViewModel
            {
                Id = product.Id,
                ProductCode = "DAAA",
                Name = "SNAP TIE",
                IsActive = true,
                ImageFile = ImageFile("new-image.png")
            });

            Assert.IsType<RedirectToActionResult>(result);
            var updated = await db.Products.FindAsync(product.Id);
            Assert.NotNull(updated);
            Assert.NotEqual("/uploads/products/old-image.png", updated.ImagePath);
            Assert.StartsWith("/uploads/products/", updated.ImagePath);
            Assert.False(File.Exists(oldFullPath));
            Assert.True(File.Exists(Path.Combine(env.WebRootPath, updated.ImagePath!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
            DeleteUploadsFolder(env);
        }
    }

    [Fact]
    public async Task Delete_RemovesImageFile()
    {
        var (db, connection) = TestData.CreateDbContext();
        var env = new TestWebHostEnvironment();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var product = new Product
            {
                ProductCode = "DAAA",
                Name = "SNAP TIE",
                ImagePath = "/uploads/products/to-delete.png",
                IsActive = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var fullPath = Path.Combine(env.WebRootPath, "uploads", "products", "to-delete.png");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, new byte[] { 1, 2, 3 });

            var controller = new ProductsController(db, TestData.UserManager(user), env)
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Delete(product.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(db.Products);
            Assert.False(File.Exists(fullPath));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
            DeleteUploadsFolder(env);
        }
    }

    private static void DeleteUploadsFolder(TestWebHostEnvironment env)
    {
        var folder = Path.Combine(env.WebRootPath, "uploads");
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
