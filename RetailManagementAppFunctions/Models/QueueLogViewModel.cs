using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailManagementAppFunctions.Models
{
    public class QueueLogViewModel
    {
        public string MessageId { get; set; } = string.Empty;

        public DateTimeOffset? InsertionTime { get; set; }

        public string MessageText { get; set; } = string.Empty;
    }
}
