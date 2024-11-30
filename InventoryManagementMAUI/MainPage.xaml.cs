using ClosedXML.Excel;
using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Pages;
using InventoryManagementMAUI.Services;

namespace InventoryManagementMAUI
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _database;
        private List<Product> _allProducts;
        private string _currentSearchText = string.Empty;
        private string _currentCategory = string.Empty;

        private int _pageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;

        public MainPage()
        {
            InitializeComponent();
            _database = new DatabaseService();

            int savedIndex = Preferences.Default.Get("PageSizeIndex", 0);
            pageSizePicker.SelectedIndex = savedIndex;

            LoadData();
        }

        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is Element element && element.BindingContext is Product product)
                {
                    await Navigation.PushAsync(new ProductPage(product));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Could not open the product: " + ex.Message, "OK");
            }
        }

        private async void OnExportClicked(object sender, EventArgs e)
        {
            await ExportToExcel();
        }

        private async Task ExportToExcel()
        {
            try
            {
                var products = await _database.GetProductsAsync();

                if (!products.Any())
                {
                    await DisplayAlert("Error", "No products to export", "OK");
                    return;
                }

                var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var fileName = $"Inventory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(directory, fileName);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inventory");

                    string[] headers = { "ID", "Name", "Description", "Quantity", "Price", "Category", "Created Date" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    int row = 2;
                    foreach (var product in products)
                    {
                        worksheet.Cell(row, 1).Value = product.Id;
                        worksheet.Cell(row, 2).Value = product.Name;
                        worksheet.Cell(row, 3).Value = product.Description;
                        worksheet.Cell(row, 4).Value = product.Quantity;
                        worksheet.Cell(row, 5).Value = product.Price;
                        worksheet.Cell(row, 6).Value = product.Category;
                        worksheet.Cell(row, 7).Value = product.CreatedAt;
                        row++;
                    }

                    worksheet.Column(4).Style.NumberFormat.NumberFormatId = 1; // Quantity as number
                    worksheet.Column(5).Style.NumberFormat.Format = "$#,##0.00"; // Price as currency
                    worksheet.Column(7).Style.NumberFormat.Format = "mm/dd/yyyy"; // Date format

                    int lastRow = products.Count() + 1;
                    worksheet.Cell(lastRow + 1, 3).Value = "Total:";
                    worksheet.Cell(lastRow + 1, 4).FormulaA1 = $"=SUM(D2:D{lastRow})";
                    worksheet.Cell(lastRow + 1, 5).FormulaA1 = $"=SUM(E2:E{lastRow})";

                    worksheet.Columns().AdjustToContents();

                    // Save and share
                    workbook.SaveAs(filePath);
                    await Task.Delay(100);

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            await Share.RequestAsync(new ShareFileRequest
                            {
                                Title = "Export Inventory",
                                File = new ShareFile(filePath)
                            });
                            await DisplayAlert("Success", $"File has been saved at:\n{filePath}", "OK");
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error", $"Error sharing file: {ex.Message}", "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "Could not create Excel file", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Export error: {ex.Message}", "OK");
            }
        }

        private async void OnOptionsClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(
                "Options",
                "Cancel",
                null,
                "Dashboard",
                "Export to Excel",
                "Backup Database",
                "Restore Database");

            var backupService = new DatabaseBackupService(_database);

            switch (action)
            {
                case "Backup Database":
                    try
                    {
                        var backupPath = await backupService.CreateBackup();
                        await Share.RequestAsync(new ShareFileRequest
                        {
                            Title = "Share Database Backup",
                            File = new ShareFile(backupPath)
                        });
                        await DisplayAlert("Success", "Backup created successfully", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Backup failed: {ex.Message}", "OK");
                    }
                    break;

                case "Restore Database":
                    try
                    {
                        var result = await FilePicker.PickAsync(new PickOptions
                        {
                            FileTypes = new FilePickerFileType(
                                new Dictionary<DevicePlatform, IEnumerable<string>>
                                {
                            { DevicePlatform.iOS, new[] { "public.database" } },
                            { DevicePlatform.Android, new[] { "application/x-sqlite3", "application/octet-stream" } },
                            { DevicePlatform.WinUI, new[] { ".db" } }
                                })
                        });

                        if (result != null)
                        {
                            await backupService.RestoreFromBackup(result.FullPath);
                            await LoadData();
                            await DisplayAlert("Success", "Database restored successfully", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Restore failed: {ex.Message}", "OK");
                    }
                    break;
            }
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                _allProducts = await _database.GetProductsAsync();

                var categories = _allProducts
                    .Select(p => p.Category ?? "No category")
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                categories.Insert(0, "All");
                categoryPicker.ItemsSource = categories;
                categoryPicker.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error loading data: " + ex.Message, "OK");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredProducts = _allProducts ?? new List<Product>();

                if (!string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    filteredProducts = filteredProducts
                        .Where(p =>
                            (p.Name?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (p.Description?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (p.Category?.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(_currentCategory) && _currentCategory != "All")
                {
                    filteredProducts = filteredProducts
                        .Where(p => p.Category == _currentCategory)
                        .ToList();
                }

                _totalPages = (int)Math.Ceiling(filteredProducts.Count / (double)_pageSize);
                if (_totalPages == 0) _totalPages = 1;

                if (_currentPage > _totalPages)
                {
                    _currentPage = _totalPages;
                }

                var paginatedProducts = filteredProducts
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                productsCollection.ItemsSource = paginatedProducts;

                UpdatePaginationControls();
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", "Error applying filters: " + ex.Message, "OK");
                });
            }
        }

        private void UpdatePaginationControls()
        {
            currentPageLabel.Text = $"{_currentPage}/{_totalPages}";

            previousButton.IsEnabled = _currentPage > 1;
            nextButton.IsEnabled = _currentPage < _totalPages;
        }

        private void OnPreviousClicked(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyFilters();
            }
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyFilters();
            }
        }

        private void OnFirstPageClicked(object sender, EventArgs e)
        {
            if (_currentPage != 1)
            {
                _currentPage = 1;
                ApplyFilters();
            }
        }

        private void OnLastPageClicked(object sender, EventArgs e)
        {
            if (_currentPage != _totalPages)
            {
                _currentPage = _totalPages;
                ApplyFilters();
            }
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            if (pageSizePicker.SelectedItem != null)
            {
                _pageSize = int.Parse(pageSizePicker.SelectedItem.ToString());
                Preferences.Default.Set("PageSizeIndex", pageSizePicker.SelectedIndex);
                _currentPage = 1;
                ApplyFilters();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSearchText = e.NewTextValue ?? string.Empty;
            ApplyFilters();
        }

        private void OnCategoryFilterChanged(object sender, EventArgs e)
        {
            if (categoryPicker.SelectedItem != null)
            {
                _currentCategory = categoryPicker.SelectedItem.ToString();
                ApplyFilters();
            }
        }

        private void OnClearFiltersClicked(object sender, EventArgs e)
        {
            searchBar.Text = string.Empty;
            categoryPicker.SelectedIndex = 0;
            _currentSearchText = string.Empty;
            _currentCategory = string.Empty;
            ApplyFilters();
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProductPage());
        }

        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Product selectedProduct)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new ProductPage(selectedProduct));
            }
        }
    }
}
