using SQLite;

namespace InventoryManagementMAUI.Models
{
    public class ProductMovement
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }  // "IN" o "OUT"
        public string Notes { get; set; }
    }
}
