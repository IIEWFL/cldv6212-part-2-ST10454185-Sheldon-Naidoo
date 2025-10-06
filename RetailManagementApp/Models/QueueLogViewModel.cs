using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RetailManagementApp.Models
{
    public class QueueLogViewModel
    {
        // Corresponds to the Azure Queue Message ID
        public string Id { get; set; } = string.Empty;

        // Corresponds to the Azure Queue InsertionTime
        [Display(Name = "Time Queued")]
        public DateTimeOffset? QueueTime { get; set; }

        [Display(Name = "Operation")]
        public string OperationType { get; set; } = string.Empty;

        // Order Key components
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;

        // Order details
        [Display(Name = "Customer")]
        public string CustomerName { get; set; } = string.Empty;

        public string MessageText { get; set; } = string.Empty;
    }
}
