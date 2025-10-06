using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace RetailManagementApp.Models
{
    public class Order
    {
        [Required]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero.")]
        [Display(Name = "Total Amount")]
        public double TotalAmount { get; set; }


        public string PartitionKey { get; set; } = string.Empty; 
        public string RowKey { get; set; } = string.Empty; 

        // The public URL to the product photo in Azure Blob Storage.
        public string ProductPhotoUrl { get; set; } = string.Empty;
        // The unique file name (blob name) used in Blob Storage.
        public string ProductPhotoBlobName { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
    }
}
