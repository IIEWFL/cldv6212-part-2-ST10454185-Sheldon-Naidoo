using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Models
{
    public class OrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public string PartitionKey { get; set; } = string.Empty; 
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public string? ETag { get; set; }
    }
}
