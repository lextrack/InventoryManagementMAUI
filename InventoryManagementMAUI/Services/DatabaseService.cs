﻿using InventoryManagementMAUI.Models;
using SQLite;

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

        public async Task<int> SaveProductAsync(Product product)
        {
            if (product.Id == 0)
            {
                product.CreatedAt = DateTime.Now;
                var result = await _database.InsertAsync(product);
                if (result > 0)
                {
                    await RegisterProductMovement(new ProductMovement
                    {
                        ProductId = product.Id,
                        Quantity = product.Quantity,
                        Date = DateTime.Now,
                        Type = "INCOMING",
                        Notes = "Initial stock entry"
                    });
                }
                return result;
            }
            else
            {
                var existingProduct = await GetProductAsync(product.Id);
                if (existingProduct != null && existingProduct.Quantity != product.Quantity)
                {
                    int difference = product.Quantity - existingProduct.Quantity;
                    await RegisterProductMovement(new ProductMovement
                    {
                        ProductId = product.Id,
                        Quantity = Math.Abs(difference),
                        Date = DateTime.Now,
                        Type = difference > 0 ? "INCOMING" : "OUTGOING",
                        Notes = $"Stock adjusted by {Math.Abs(difference)} units"
                    });
                }
                return await _database.UpdateAsync(product);
            }
        }

        public async Task<int> DeleteProductAsync(Product product)
        {
            return await _database.DeleteAsync(product);
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
