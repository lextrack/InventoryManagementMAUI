using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagementApp.Models
{
    public class CategoryItem
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
    }
}
