using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Models
{
    public class QueueMessageDto
    {
        public string OperationType { get; set; } = string.Empty; 
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public DateTime CreationTimeUtc { get; set; }
    }
}