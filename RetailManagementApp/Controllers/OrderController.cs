using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RetailManagementApp.Models;
using RetailManagementApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RetailManagementApp.Controllers
{
    public class OrderController : Controller
    {
        // Code Attribution
        // This method was adapted from Microsoft Learn
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line
        // Microsoft Learn

        // Dependency Injection for the API client and logging
        private readonly IFunctionService _functionService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionService functionService, ILogger<OrderController> logger)
        {
            _functionService = functionService ?? throw new ArgumentNullException(nameof(functionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Order/Index (List all orders - using Index as the main list view)
        public async Task<IActionResult> Index()
        {
            // Code Attribution
            // This method was adapted from C-Sharpcorner
            // https://www.c-sharpcorner.com/article/how-to-use-tempdata-in-asp-net/
            // Usama Shahid
            // https://www.c-sharpcorner.com/members/muhammad-usama13

            try
            {
                // Calls Azure Function: GetAllOrders
                var orders = await _functionService.GetAllOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve orders from Azure Function API.");
                TempData["ErrorMessage"] = "Could not load orders. Check API connection.";
                return View(new List<Order>());
            }
        }

        // GET: /Order/Create (Display create form)
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Order/Create (Submit new order with photo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, IFormFile? photo)
        {
            // Code Attribution
            // This method was adapted from Microsoft Learn
            // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-9.0
            // Microsoft Learn

            if (!ModelState.IsValid)
            {
                return View(order);
            }

            try
            {
                // Calls Azure Function: CreateOrder
                bool success = await _functionService.AddOrderAsync(order, photo);

                if (success)
                {
                    TempData["SuccessMessage"] = "Order created and photo uploaded successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "The Azure Function API failed to process the new order.");
                    return View(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order via Azure Function.");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                return View(order);
            }
        }

        // GET: /Order/Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            try
            {
                var order = await _functionService.GetOrderAsync(partitionKey, rowKey);
                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order details.");
                TempData["ErrorMessage"] = "Failed to load order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Order/Edit (Load data for editing)
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            try
            {
                var order = await _functionService.GetOrderAsync(partitionKey, rowKey);
                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order for edit.");
                TempData["ErrorMessage"] = "Failed to load order for editing.";
                return RedirectToAction(nameof(Index));
            }
        }


        // POST: /Order/Edit (Submit updated data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            try
            {
                // Calls Azure Function: UpdateOrder (Passing null for IFormFile)
                bool success = await _functionService.UpdateOrderAsync(order, null);

                if (success)
                {
                    TempData["SuccessMessage"] = "Order updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Update failed. The order may not exist or the API returned an error.");
                    return View(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order.");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                return View(order);
            }
        }

        // GET: /Order/Delete (Confirmation view)
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            try
            {
                // Calls Azure Function: GetOrder
                var order = await _functionService.GetOrderAsync(partitionKey, rowKey);
                if (order == null)
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order for delete confirmation.");
                TempData["ErrorMessage"] = "Failed to load order for deletion confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Order/Delete (Execute deletion)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey, string productPhotoBlobName)
        {

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return BadRequest("Missing required keys for deletion.");
            }

            try
            {
                // Calls Azure Function: DeleteOrder.
                bool success = await _functionService.DeleteOrderAsync(partitionKey, rowKey, productPhotoBlobName);

                if (success)
                {
                    TempData["SuccessMessage"] = "Order and photo deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete order via the Azure Function API.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing order deletion.");
                TempData["ErrorMessage"] = $"An error occurred during deletion: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- Queue and File Share Actions ---

        // GET: /Order/Log (View queue messages)
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            try
            {
                // Calls Azure Function: GetMessages
                var logMessages = await _functionService.GetMessagesAsync();
                return View(logMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve queue messages.");
                TempData["ErrorMessage"] = "Could not retrieve transaction logs. Check API connection.";
                return View(new List<QueueLogViewModel>());
            }
        }

        // POST: /Order/ExportLog (Trigger file share upload)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportLog()
        {
            try
            {
                var filename = $"Log_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                // Calls Azure Function: UploadLogFile
                var fileUrl = await _functionService.ExportLog(filename);
                TempData["SuccessMessage"] = $"Log file generated and uploaded successfully. File URL: {fileUrl}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting log file.");
                TempData["ErrorMessage"] = $"Failed to export log file: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}