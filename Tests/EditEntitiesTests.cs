
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mercadito.Tests
{
    public class EditEntitiesTests
    {
        [Fact]
        public async Task UpdateCategoryAndProduct_Flows_CallRepositoryMethodsAndValidate()
        {
            // --- Arrange: Category ---
            var categoryId = Guid.NewGuid();
            var mockCategoryRepo = new Mock<ICategoryRepository>();
            // existing category returned when asked
            mockCategoryRepo.Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(new CategoryModel { Id = categoryId, Code = "OLD", Name = "OldName", Description = "Old" });

            Category? capturedUpdatedCategory = null;
            mockCategoryRepo.Setup(r => r.UpdateCategoryAsync(It.IsAny<Category>()))
                .Returns(Task.CompletedTask)
                .Callback<Category>(c => capturedUpdatedCategory = c);

            var categoriesPage = new CategoriesModel(NullLogger<CategoriesModel>.Instance, mockCategoryRepo.Object)
            {
                EditCategory = new UpdateCategoryDto
                {
                    Id = categoryId,
                    Code = "NEW-CODE",
                    Name = "New Name",
                    Description = "New Desc"
                }
            };

            // --- Act: Category edit ---
            var catResult = await categoriesPage.OnPostEditAsync();

            // --- Assert: Category edit ---
            mockCategoryRepo.Verify(r => r.GetCategoryByIdAsync(categoryId), Times.Once);
            mockCategoryRepo.Verify(r => r.UpdateCategoryAsync(It.IsAny<Category>()), Times.Once);
            Assert.NotNull(capturedUpdatedCategory);
            Assert.Equal(categoryId, capturedUpdatedCategory!.Id);
            Assert.Equal("NEW-CODE", capturedUpdatedCategory.Code);
            Assert.Equal("New Name", capturedUpdatedCategory.Name);

            // --- Arrange: Product ---
            var productId = Guid.NewGuid();
            var oldCategoryId = Guid.NewGuid();
            var newCategoryId = Guid.NewGuid();

            var mockProductRepo = new Mock<IProductRepository>();
            var mockProductCategoryRepo = new Mock<IProductCategoryRepository>();
            var mockCategoryRepo2 = new Mock<ICategoryRepository>();

            var existingProduct = new Product
            {
                Id = productId,
                Name = "Old Product",
                Description = "Old Desc",
                Stock = 5,
                Lote = DateTime.Today.AddDays(-2),
                FechaDeCaducidad = DateTime.Today.AddDays(10),
                Price = 9.99m
            };

            mockProductRepo.Setup(r => r.GetProductByIdAsync(productId)).ReturnsAsync(existingProduct);

            Product? capturedUpdatedProduct = null;
            mockProductRepo.Setup(r => r.UpdateProductAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask)
                .Callback<Product>(p => capturedUpdatedProduct = p);

            // existing relation present
            var existingRelation = new ProductCategory(productId, oldCategoryId);
            mockProductCategoryRepo.Setup(r => r.GetProductsCategoriesByProductIdAsync(productId))
                .ReturnsAsync(existingRelation);

            mockProductCategoryRepo.Setup(r => r.DeleteProductCategoryAsync(It.IsAny<ProductCategory>()))
                .Returns(Task.CompletedTask);

            ProductCategory? capturedAddedRelation = null;
            mockProductCategoryRepo.Setup(r => r.AddProductCategoryAsync(It.IsAny<ProductCategory>()))
                .Returns(Task.CompletedTask)
                .Callback<ProductCategory>(pc => capturedAddedRelation = pc);

            // page model for products
            var mockRegisterUseCase = new Mock<RegisterNewProductUseCase>(
                Mock.Of<IProductRepository>(), Mock.Of<ICategoryRepository>(), NullLogger<RegisterNewProductUseCase>.Instance);

            var mockRegisterWithCat = new Mock<RegisterNewProductWithCategoryUseCase>(
                Mock.Of<RegisterNewProductUseCase>(), Mock.Of<AsignCategoryToProductUseCase>());

            var mockAsignUseCase = new Mock<AsignCategoryToProductUseCase>(
                Mock.Of<IProductCategoryRepository>(), Mock.Of<IProductRepository>(), Mock.Of<ICategoryRepository>());

            var productsPage = new Pages.Products.ProductsModel(
                NullLogger<Pages.Products.ProductsModel>.Instance,
                mockProductRepo.Object,
                mockCategoryRepo2.Object,
                mockProductCategoryRepo.Object,
                mockRegisterUseCase.Object,
                mockRegisterWithCat.Object,
                mockAsignUseCase.Object
            );

            // fill EditProduct DTO (simulate user edit)
            productsPage.EditProduct = new UpdateProductDto
            {
                Id = productId,
                Name = "Updated Product",
                Description = "Updated Desc",
                Stock = 20,
                Lote = DateTime.Today,
                FechaDeCaducidad = DateTime.Today.AddMonths(6),
                Price = 19.95m,
                CategoryId = newCategoryId
            };

            // --- Act: Product edit ---
            var prodResult = await productsPage.OnPostEditAsync();

            // --- Assert: Product edit ---
            mockProductRepo.Verify(r => r.GetProductByIdAsync(productId), Times.Once);
            mockProductRepo.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Once);
            Assert.NotNull(capturedUpdatedProduct);
            Assert.Equal(productId, capturedUpdatedProduct!.Id);
            Assert.Equal("Updated Product", capturedUpdatedProduct.Name);
            Assert.Equal(20, capturedUpdatedProduct.Stock);
            Assert.Equal(19.95m, capturedUpdatedProduct.Price);

            // relation deletion + addition should be called
            mockProductCategoryRepo.Verify(r => r.DeleteProductCategoryAsync(It.Is<ProductCategory>(pc => pc.ProductId == productId && pc.CategoryId == oldCategoryId)), Times.Once);
            mockProductCategoryRepo.Verify(r => r.AddProductCategoryAsync(It.IsAny<ProductCategory>()), Times.Once);
            Assert.NotNull(capturedAddedRelation);
            Assert.Equal(productId, capturedAddedRelation!.ProductId);
            Assert.Equal(newCategoryId, capturedAddedRelation.CategoryId);
        }
    }
}