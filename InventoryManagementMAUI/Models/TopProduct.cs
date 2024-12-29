using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagementApp.Models
{
    public class TopProduct
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int MovementCount { get; set; }
        public decimal Value { get; set; }
    }
}
