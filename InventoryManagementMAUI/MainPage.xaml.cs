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

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Load all products
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
                _currentPage = 1; // Reset to first page
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
