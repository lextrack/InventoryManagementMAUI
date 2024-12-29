using InventoryManagementApp.Models;
using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Services;
using System.Collections.ObjectModel;

namespace InventoryManagementMAUI.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DatabaseService _database;
        private readonly ObservableCollection<ActivityItem> _recentActivity;
        private readonly ObservableCollection<CategoryItem> _categories;
        private readonly ObservableCollection<AlertItem> _alerts;
        private readonly ObservableCollection<TopProduct> _topProducts;
        private DateTime _startDate = DateTime.MinValue;
        private bool _isLoading;

        public DashboardPage()
        {
            InitializeComponent();
            _database = new DatabaseService();
            _recentActivity = new ObservableCollection<ActivityItem>();
            _categories = new ObservableCollection<CategoryItem>();
            _alerts = new ObservableCollection<AlertItem>();
            _topProducts = new ObservableCollection<TopProduct>();

            recentActivityList.ItemsSource = _recentActivity;
            categoryList.ItemsSource = _categories;
            alertsList.ItemsSource = _alerts;
            topProductsList.ItemsSource = _topProducts;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (!_isLoading)
            {
                MainThread.BeginInvokeOnMainThread(async () => await LoadDashboardData());
            }
        }

        private async Task LoadDashboardData()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                IsBusy = true;

                var productsTask = _database.GetProductsAsync();
                var movementsTask = _database.GetAllMovements();

                await Task.WhenAll(productsTask, movementsTask);

                var products = await productsTask;
                var movements = await movementsTask;

                if (_startDate != DateTime.MinValue)
                {
                    movements = movements.Where(m => m.Date >= _startDate).ToList();
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateSummaryCards(products);
                    UpdateRecentActivity(movements, products);
                    UpdateCategoryDistribution(products);
                    UpdateMovementSummary(movements, products);
                    UpdateAlerts(products, movements);
                    UpdateTopProducts(products, movements);
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Error", "Error loading dashboard data: " + ex.Message, "OK"));
            }
            finally
            {
                _isLoading = false;
                IsBusy = false;
            }
        }

        private void UpdateSummaryCards(List<Product> products)
        {
            UpdateTotalProducts(products);
            UpdateTotalValue(products);
            UpdateLowStock(products);
            UpdateOutOfStock(products);
        }

        private void UpdateTotalProducts(List<Product> products)
        {
            totalProductsLabel.Text = products.Count.ToString();
        }

        private void UpdateTotalValue(List<Product> products)
        {
            totalValueLabel.Text = $"${products.Sum(p => p.Price * p.Quantity):N2}";
        }

        private void UpdateLowStock(List<Product> products)
        {
            const int LOW_STOCK_THRESHOLD = 10;
            lowStockLabel.Text = products.Count(p => p.Quantity > 0 && p.Quantity <= LOW_STOCK_THRESHOLD).ToString();
        }

        private void UpdateOutOfStock(List<Product> products)
        {
            outOfStockLabel.Text = products.Count(p => p.Quantity == 0).ToString();
        }

        private void UpdateRecentActivity(List<ProductMovement> movements, List<Product> products)
        {
            _recentActivity.Clear();
            var productDict = products.ToDictionary(p => p.Id);

            var recentMovements = movements
                .OrderByDescending(m => m.Date)
                .Take(10);

            foreach (var movement in recentMovements)
            {
                if (productDict.TryGetValue(movement.ProductId, out var product))
                {
                    _recentActivity.Add(new ActivityItem
                    {
                        Icon = movement.Type == "INCOMING" ? "📥" : "📤",
                        Description = $"{product.Name} - {movement.Notes}",
                        Date = movement.Date,
                        Quantity = movement.Type == "INCOMING" ? movement.Quantity : -movement.Quantity,
                        TextColor = movement.Type == "INCOMING" ? Colors.Green : Colors.Red
                    });
                }
            }
        }

        private void UpdateCategoryDistribution(List<Product> products)
        {
            _categories.Clear();
            var categoryGroups = products
                .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "Uncategorized" : p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var totalProducts = products.Count;
            var colors = new[] { "#4CAF50", "#2196F3", "#FFC107", "#F44336", "#9C27B0", "#FF5722" };
            var colorIndex = 0;

            foreach (var category in categoryGroups)
            {
                _categories.Add(new CategoryItem
                {
                    Category = category.Category,
                    Count = category.Count,
                    Percentage = (double)category.Count / totalProducts,
                    Color = colors[colorIndex % colors.Length]
                });
                colorIndex++;
            }
        }

        private void UpdateMovementSummary(List<ProductMovement> movements, List<Product> products)
        {
            var existingProductIds = products.Select(p => p.Id).ToHashSet();
            var validMovements = movements.Where(m => existingProductIds.Contains(m.ProductId));

            incomingCountLabel.Text = validMovements.Count(m => m.Type == "INCOMING").ToString();
            outgoingCountLabel.Text = validMovements.Count(m => m.Type == "OUTGOING").ToString();
        }

        private void UpdateAlerts(List<Product> products, List<ProductMovement> movements)
        {
            _alerts.Clear();
            const int LOW_STOCK_THRESHOLD = 10;
            var now = DateTime.Now;

            // products with low stock
            foreach (var product in products.Where(p => p.Quantity > 0 && p.Quantity <= LOW_STOCK_THRESHOLD))
            {
                _alerts.Add(new AlertItem
                {
                    Icon = "⚠️",
                    Message = $"Low stock for {product.Name} ({product.Quantity} units remaining)",
                    Color = Colors.Orange
                });
            }

            // products out of stock
            foreach (var product in products.Where(p => p.Quantity == 0))
            {
                _alerts.Add(new AlertItem
                {
                    Icon = "❗",
                    Message = $"Out of stock: {product.Name}",
                    Color = Colors.Red
                });
            }

            // products with no recent movements
            var lastMovements = movements
                .GroupBy(m => m.ProductId)
                .ToDictionary(g => g.Key, g => g.Max(m => m.Date));

            foreach (var product in products)
            {
                if (!lastMovements.ContainsKey(product.Id))
                {
                    _alerts.Add(new AlertItem
                    {
                        Icon = "ℹ️",
                        Message = $"No movements for {product.Name}",
                        Color = Colors.Blue
                    });
                }
                else if ((now - lastMovements[product.Id]).TotalDays > 30)
                {
                    _alerts.Add(new AlertItem
                    {
                        Icon = "📊",
                        Message = $"No recent movements for {product.Name}",
                        Color = Colors.Purple
                    });
                }
            }
        }

        private void UpdateTopProducts(List<Product> products, List<ProductMovement> movements)
        {
            _topProducts.Clear();

            var productMovements = movements
                .GroupBy(m => m.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    MovementCount = g.Count(),
                    TotalQuantity = g.Sum(m => m.Quantity)
                })
                .OrderByDescending(x => x.MovementCount)
                .Take(5);

            var productDict = products.ToDictionary(p => p.Id);

            foreach (var pm in productMovements)
            {
                if (productDict.TryGetValue(pm.ProductId, out var product))
                {
                    _topProducts.Add(new TopProduct
                    {
                        Name = product.Name,
                        Category = product.Category ?? "Uncategorized",
                        MovementCount = pm.MovementCount,
                        Value = product.Price * product.Quantity
                    });
                }
            }
        }

        private async void OnLastWeekClicked(object sender, EventArgs e)
        {
            _startDate = DateTime.Now.AddDays(-7);
            await LoadDashboardData();
        }

        private async void OnLastMonthClicked(object sender, EventArgs e)
        {
            _startDate = DateTime.Now.AddDays(-30);
            await LoadDashboardData();
        }

        private async void OnAllTimeClicked(object sender, EventArgs e)
        {
            _startDate = DateTime.MinValue;
            await LoadDashboardData();
        }
    }

}