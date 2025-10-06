using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Models
{
    public class Order : ITableEntity
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public string PartitionKey { get; set; } = string.Empty; 
        public string RowKey { get; set; } = string.Empty; 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }


        // URL pointing to the product image stored in Blob Storage
        public string ProductPhotoUrl { get; set; } = string.Empty;

        // The unique name of the blob file
        public string ProductPhotoBlobName { get; set; } = string.Empty;
    }
}
