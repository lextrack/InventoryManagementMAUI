using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Services;
using System.Diagnostics;

namespace InventoryManagementMAUI.Pages;

public partial class ProductPage : ContentPage
{
    private readonly DatabaseService _database;
    private Product _product;
    private bool _isQuantityValid = true;
    private bool _isPriceValid = true;
    private List<string> _categories;
    private List<string> _locations;

    public ProductPage(Product product = null)
    {
        InitializeComponent();
        _database = new DatabaseService();
        _product = product;

        newProductButtons.IsVisible = product == null;
        existingProductButtons.IsVisible = product != null;

        InitializePageAsync();

        if (product != null)
        {
            nameEntry.Text = product.Name;
            skuLabel.Text = product.SKU ?? "No SKU";
            descriptionEntry.Text = product.Description;
            quantityEntry.Text = product.Quantity.ToString();
            priceEntry.Text = product.Price.ToString("F2");
            SetInitialCategory(product.Category);
            UpdateTotal();
        }

        UpdateSaveButtonState();
    }

    private async void InitializePageAsync()
    {
        await LoadCategoriesAsync();
        await LoadLocationsAsync();

        if (_product != null)
        {
            SetInitialCategory(_product.Category);
            SetInitialLocation(_product.Location);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _categories = await _database.GetAllCategoriesAsync();
            categoryPicker.ItemsSource = _categories;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load categories: " + ex.Message, "OK");
        }
    }

    private void SetInitialCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) return;

        var index = _categories?.IndexOf(category) ?? -1;
        if (index >= 0)
        {
            categoryPicker.SelectedIndex = index;
        }
        else if (_categories != null)
        {
            _categories.Add(category);
            categoryPicker.ItemsSource = null;
            categoryPicker.ItemsSource = _categories;
            categoryPicker.SelectedIndex = _categories.Count - 1;
        }
    }

    private void OnCategorySelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateSaveButtonState();
    }

    private async void OnAddCategoryClicked(object sender, EventArgs e)
    {
        string newCategory = await DisplayPromptAsync("New Category",
            "Enter the name for the new category:",
            accept: "Add",
            cancel: "Cancel");

        if (!string.IsNullOrWhiteSpace(newCategory))
        {
            if (_categories == null)
                _categories = new List<string>();

            if (!_categories.Contains(newCategory))
            {
                _categories.Add(newCategory);
                categoryPicker.ItemsSource = null;
                categoryPicker.ItemsSource = _categories;
                categoryPicker.SelectedItem = newCategory;
            }
        }
    }

    private async Task LoadLocationsAsync()
    {
        try
        {
            _locations = await _database.GetAllLocationsAsync();
            locationPicker.ItemsSource = _locations;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load locations: " + ex.Message, "OK");
        }
    }

    private void SetInitialLocation(string location)
    {
        if (string.IsNullOrEmpty(location)) return;

        var index = _locations?.IndexOf(location) ?? -1;
        if (index >= 0)
        {
            locationPicker.SelectedIndex = index;
        }
        else if (_locations != null)
        {
            _locations.Add(location);
            locationPicker.ItemsSource = null;
            locationPicker.ItemsSource = _locations;
            locationPicker.SelectedIndex = _locations.Count - 1;
        }
    }

    private async void OnAddLocationClicked(object sender, EventArgs e)
    {
        string newLocation = await DisplayPromptAsync("New Location",
            "Enter the name for the new location:",
            accept: "Add",
            cancel: "Cancel");

        if (!string.IsNullOrWhiteSpace(newLocation))
        {
            if (_locations == null)
                _locations = new List<string>();

            if (!_locations.Contains(newLocation))
            {
                _locations.Add(newLocation);
                locationPicker.ItemsSource = null;
                locationPicker.ItemsSource = _locations;
                locationPicker.SelectedItem = newLocation;
            }
        }
    }

    private async void OnRegisterOutputClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProductOutputPage(_product));
    }

    private async void OnViewMovementsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProductMovementsPage(_product));
    }

    private void UpdateTotal()
    {
        try
        {
            Debug.WriteLine($"Quantity text: '{quantityEntry.Text}'");
            Debug.WriteLine($"Price text: '{priceEntry.Text}'");

            if (string.IsNullOrWhiteSpace(quantityEntry.Text) ||
                string.IsNullOrWhiteSpace(priceEntry.Text))
            {
                totalLabel.Text = "$ 0.00";
                return;
            }

            if (int.TryParse(quantityEntry.Text.Trim(), out int quantity) &&
                decimal.TryParse(priceEntry.Text.Trim(), System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out decimal price))
            {
                decimal total = quantity * price;
                totalLabel.Text = $"$ {total:N2}";

                Debug.WriteLine($"Converted quantity: {quantity}");
                Debug.WriteLine($"Converted price: {price}");
                Debug.WriteLine($"Calculated total: {total}");
            }
            else
            {
                totalLabel.Text = "$ 0.00";
                Debug.WriteLine("Could not convert quantity or price");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UpdateTotal: {ex.Message}");
            totalLabel.Text = "$ 0.00";
        }
    }

    private void OnQuantityTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _isQuantityValid = false;
            quantityError.IsVisible = false;
        }
        else
        {
            _isQuantityValid = int.TryParse(e.NewTextValue, out int quantity) && quantity >= 0;
            quantityError.IsVisible = !_isQuantityValid;

            if (!_isQuantityValid && !string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                quantityError.IsVisible = true;
                ((Entry)sender).Text = e.OldTextValue;
            }
        }

        UpdateSaveButtonState();
        UpdateTotal();
    }

    private void OnPriceTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _isPriceValid = false;
            priceError.IsVisible = false;
        }
        else
        {
            var newText = e.NewTextValue.Trim();
            var decimalPoints = newText.Count(c => c == '.');

            _isPriceValid = decimal.TryParse(newText,
                                          System.Globalization.NumberStyles.Any,
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          out decimal price) &&
                           price >= 0 &&
                           decimalPoints <= 1;

            priceError.IsVisible = !_isPriceValid;

            if (!_isPriceValid && !string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                priceError.IsVisible = true;
                ((Entry)sender).Text = e.OldTextValue;
            }
        }

        UpdateSaveButtonState();
        UpdateTotal();
    }

    private void UpdateSaveButtonState()
    {
        bool isValid = _isQuantityValid &&
                      _isPriceValid &&
                      !string.IsNullOrWhiteSpace(nameEntry.Text);

        if (_product == null)
        {
            saveButton.IsEnabled = isValid;
        }
        else
        {
            saveExistingButton.IsEnabled = isValid;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nameEntry.Text))
            {
                await DisplayAlert("Error", "Name is required", "OK");
                return;
            }

            if (!_isQuantityValid || !_isPriceValid)
            {
                await DisplayAlert("Error", "Please correct the fields marked in red", "OK");
                return;
            }

            if (categoryPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please select or add a category", "OK");
                return;
            }

            if (locationPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Please select or add a location", "OK");
                return;
            }

            if (_product == null)
                _product = new Product();

            _product.Name = nameEntry.Text;
            _product.Description = descriptionEntry.Text;
            _product.Quantity = int.Parse(quantityEntry.Text);
            _product.Price = decimal.Parse(priceEntry.Text);
            _product.Category = categoryPicker.SelectedItem.ToString();
            _product.Location = locationPicker.SelectedItem.ToString();

            await _database.SaveProductAsync(_product);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not save the product. Please check the fields.", "OK");
        }
    }

    private async void OnCopyToClipboardClicked(object sender, EventArgs e)
    {
        try
        {
            var productData = $"Product: {nameEntry.Text}\n" +
                            $"SKU: {skuLabel.Text}\n" +
                            $"Description: {descriptionEntry.Text}\n" +
                            $"Quantity: {quantityEntry.Text}\n" +
                            $"Price: ${priceEntry.Text}\n" +
                            $"Location: {locationPicker.SelectedItem}\n" +
                            $"Category: {categoryPicker.SelectedItem}\n" +
                            $"Total: {totalLabel.Text}";

            await Clipboard.SetTextAsync(productData);
            await DisplayAlert("Success", "Data copied to clipboard", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not copy the data: " + ex.Message, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_product != null)
        {
            bool answer = await DisplayAlert("Confirm",
                "Are you sure you want to delete this product?",
                "Yes", "No");

            if (answer)
            {
                await _database.DeleteProductAsync(_product);
                await Navigation.PopAsync();
            }
        }
    }

    public void SetProductData(Product product)
    {
        nameEntry.Text = product.Name;
        skuLabel.Text = product.SKU;
        descriptionEntry.Text = product.Description;
        quantityEntry.Text = product.Quantity.ToString();
        priceEntry.Text = product.Price.ToString("F2");
        SetInitialCategory(product.Category);
        SetInitialLocation(product.Location);
        UpdateTotal();
    }

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        if (sender == nameEntry)
            descriptionEntry.Focus();
        else if (sender == descriptionEntry)
            quantityEntry.Focus();
        else if (sender == quantityEntry)
            priceEntry.Focus();
        else if (sender == priceEntry)
            priceEntry.Unfocus();
    }
}
