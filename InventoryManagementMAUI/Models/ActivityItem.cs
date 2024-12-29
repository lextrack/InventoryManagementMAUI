using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagementApp.Models
{
    public class ActivityItem
    {
        public string Icon { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
        public Color TextColor { get; set; }
    }
}
