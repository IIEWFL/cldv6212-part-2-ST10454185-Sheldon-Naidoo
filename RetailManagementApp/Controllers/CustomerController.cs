using Microsoft.AspNetCore.Mvc;
using RetailManagementApp.Models;
using RetailManagementApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementApp.Controllers
{
    public class CustomerController : Controller
    {
        // Code Attribution
        // This method was adapted from W3Schools
        // https://www.w3schools.com/cs/cs_constructors.php
        // W3Schools

        private readonly CustomerTableService _customerTableService;

        public CustomerController(CustomerTableService customerTableService)
        {
            _customerTableService = customerTableService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerTableService.GetCustomerAsync();
            return View(customers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName, Email")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerTableService.AddCustomerAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _customerTableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _customerTableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, [Bind("PartitionKey,RowKey,ETag,CustomerName,Email")] Customer customer)
        {
            if (partitionKey != customer.PartitionKey || rowKey != customer.RowKey)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _customerTableService.UpdateCustomerAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var customer = await _customerTableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var customer = await _customerTableService.GetCustomerAsync(partitionKey, rowKey);
            if (customer != null)
            {
                await _customerTableService.DeleteCustomerAsync(partitionKey, rowKey);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
