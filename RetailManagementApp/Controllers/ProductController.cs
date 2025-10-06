using Microsoft.AspNetCore.Mvc;
using RetailManagementApp.Models;
using RetailManagementApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementApp.Controllers
{
    public class ProductController : Controller
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly ProductTableService _productTableService;
        private readonly ProductImageBlobStorageService _productImageBlobService;
        private readonly TransactionQueueService _transactionQueueService;

        public ProductController(ProductTableService productTableStorageService, ProductImageBlobStorageService productImageBlobStorageService, TransactionQueueService transactionQueueService)
        {
            _productTableService = productTableStorageService;
            _productImageBlobService = productImageBlobStorageService;
            _transactionQueueService = transactionQueueService;

        }

        public async Task<IActionResult> Index()
        {
            var products = await _productTableService.GetProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _productTableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName, Price")] Product product, IFormFile image)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-9.0
            // Microsoft Learn

            if (ModelState.IsValid)
            {
                if (image != null && image.Length > 0)
                {
                    using (var stream = image.OpenReadStream())
                    {
                        var blobName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var imageUrl = await _productImageBlobService.UploadImageAsync(blobName, stream);
                    }
                }

                await _productTableService.AddProductAsync(product);
                await _transactionQueueService.SendTransactionMessageAsync(new { Type = "NewProduct", ProductId = product.RowKey });

                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _productTableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("ProductName, Price, Quantity")] Product updatedProduct)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            // Get the existing product to retain its ETag for concurrency control
            var product = await _productTableService.GetProductAsync(partitionKey, rowKey);

            if (product == null)
            {
                return NotFound();
            }

            // The ETag is required for the update to work correctly
            updatedProduct.PartitionKey = partitionKey;
            updatedProduct.RowKey = rowKey;
            updatedProduct.ETag = product.ETag;

            if (ModelState.IsValid)
            {
                await _productTableService.UpdateProductAsync(updatedProduct);
                return RedirectToAction(nameof(Index));
            }
            return View(updatedProduct);
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _productTableService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            await _productTableService.DeleteProductAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
