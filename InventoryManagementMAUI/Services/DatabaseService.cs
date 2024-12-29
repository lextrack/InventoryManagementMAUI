using InventoryManagementMAUI.Models;
using SQLite;
using System.Diagnostics;

namespace InventoryManagementMAUI.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public DatabaseService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "inventory.db");
            _database = new SQLiteAsyncConnection(_databasePath);
            _database.CreateTableAsync<Product>().Wait();
            _database.CreateTableAsync<ProductMovement>().Wait();
        }

        public async Task CloseConnection()
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
            }
        }

        public async Task ReopenConnection()
        {
            if (_database == null)
            {
                _database = new SQLiteAsyncConnection(_databasePath);
            }
        }

        public async Task<List<string>> GetAllLocationsAsync()
        {
            var products = await _database.Table<Product>().ToListAsync();
            return products
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToList();
        }

        public async Task<bool> IsSkuUniqueAsync(string sku)
        {
            var existingProduct = await _database.Table<Product>()
                .FirstOrDefaultAsync(p => p.SKU == sku);
            return existingProduct == null;
        }

        public async Task<string> GenerateSKUAsync(string category)
        {
            var prefix = GetCategoryPrefix(category);
            var year = DateTime.Now.ToString("yy");

            var products = await _database.Table<Product>().ToListAsync();
            var lastSKU = products
                .Where(p => p.SKU != null && p.SKU.StartsWith($"{prefix}{year}"))
                .OrderByDescending(p => p.SKU)
                .FirstOrDefault();

            int sequence = 1;
            if (lastSKU != null && lastSKU.SKU.Length >= 11)
            {
                if (int.TryParse(lastSKU.SKU.Substring(5), out int lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            string newSKU;
            bool isUnique = false;
            int maxAttempts = 100;
            int attempts = 0;

            do
            {
                newSKU = $"{prefix}{year}{sequence:D6}";
                isUnique = await IsSkuUniqueAsync(newSKU);

                if (!isUnique)
                {
                    sequence++;
                    attempts++;
                }
            } while (!isUnique && attempts < maxAttempts);

            if (!isUnique)
            {
                throw new Exception("SKU could not be generated after multiple attempts.");
            }

            return newSKU;
        }

        private string GetCategoryPrefix(string category)
        {
            if (string.IsNullOrEmpty(category)) return "XX";

            var prefix = new string(category.ToUpper()
                .Where(char.IsLetter)
                .Take(3)
                .ToArray());

            return prefix.PadRight(3, 'X');
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await _database.Table<Product>()
                                 .OrderByDescending(p => p.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<Product> GetProductAsync(int id)
        {
            return await _database.Table<Product>()
                                .Where(p => p.Id == id)
                                .FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            var products = await _database.Table<Product>().ToListAsync();
            return products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public async Task<int> SaveProductAsync(Product product)
        {
            try
            {
                if (product.Id == 0)
                {
                    product.SKU = await GenerateSKUAsync(product.Category);
                    product.CreatedAt = DateTime.Now;
                    product.Location = product.Location ?? string.Empty;

                    var result = await _database.InsertAsync(product);

                    if (result > 0)
                    {
                        var savedProduct = await _database.Table<Product>()
                            .OrderByDescending(p => p.Id)
                            .FirstOrDefaultAsync();

                        product.Id = savedProduct.Id;

                        await RegisterProductMovement(new ProductMovement
                        {
                            ProductId = product.Id,
                            Quantity = product.Quantity,
                            Date = DateTime.Now,
                            Type = "INCOMING",
                            Notes = $"Initial stock entry - Location: {product.Location}"
                        });
                    }
                    return result;
                }
                else
                {
                    var existingProduct = await GetProductAsync(product.Id);
                    if (existingProduct != null)
                    {
                        string locationChange = "";
                        if (existingProduct.Location != product.Location)
                        {
                            locationChange = $" - Moved from {existingProduct.Location} to {product.Location}";
                        }

                        if (existingProduct.Quantity != product.Quantity)
                        {
                            int difference = product.Quantity - existingProduct.Quantity;
                            await RegisterProductMovement(new ProductMovement
                            {
                                ProductId = product.Id,
                                Quantity = Math.Abs(difference),
                                Date = DateTime.Now,
                                Type = difference > 0 ? "INCOMING" : "OUTGOING",
                                Notes = $"Stock adjusted by {Math.Abs(difference)} units{locationChange}"
                            });
                        }
                        else if (!string.IsNullOrEmpty(locationChange))
                        {
                            await RegisterProductMovement(new ProductMovement
                            {
                                ProductId = product.Id,
                                Quantity = 0,
                                Date = DateTime.Now,
                                Type = "LOCATION_CHANGE",
                                Notes = $"Location changed{locationChange}"
                            });
                        }
                    }
                    return await _database.UpdateAsync(product);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveProductAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteProductAsync(Product product)
        {
            await _database.RunInTransactionAsync(async (tran) =>
            {
                await _database.Table<ProductMovement>()
                    .Where(m => m.ProductId == product.Id)
                    .DeleteAsync();

                await _database.DeleteAsync(product);
            });

            return 1;
        }

        public async Task RegisterProductOutput(int productId, int quantity, string notes)
        {
            await _database.RunInTransactionAsync(async (tran) =>
            {
                var product = await GetProductAsync(productId);
                if (product == null)
                    throw new Exception("Product not found");

                if (product.Quantity < quantity)
                    throw new Exception("Insufficient stock");

                product.Quantity -= quantity;
                await _database.UpdateAsync(product);

                var movement = new ProductMovement
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Date = DateTime.Now,
                    Type = "OUTGOING",
                    Notes = notes
                };

                await RegisterProductMovement(movement);
            });
        }

        private async Task RegisterProductMovement(ProductMovement movement)
        {
            await _database.InsertAsync(movement);
        }

        public async Task<List<ProductMovement>> GetProductMovements(int productId)
        {
            return await _database.Table<ProductMovement>()
                                .Where(m => m.ProductId == productId)
                                .OrderByDescending(m => m.Date)
                                .ToListAsync();
        }

        public async Task<List<ProductMovement>> GetAllMovements(string type = null)
        {
            if (string.IsNullOrEmpty(type))
            {
                return await _database.Table<ProductMovement>()
                                    .OrderByDescending(m => m.Date)
                                    .ToListAsync();
            }

            return await _database.Table<ProductMovement>()
                                .Where(m => m.Type == type)
                                .OrderByDescending(m => m.Date)
                                .ToListAsync();
        }
    }
}
