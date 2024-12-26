using SQLite;

namespace InventoryManagementMAUI.Models
{
    public class Product
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
