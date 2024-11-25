using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Services;

namespace InventoryManagementMAUI.Pages;

public partial class ProductPage : ContentPage
{
    private readonly DatabaseService _database;
    private Product _product;
    private bool _isQuantityValid = true;
    private bool _isPriceValid = true;

    public ProductPage(Product product = null)
    {
        InitializeComponent();
        _database = new DatabaseService();
        _product = product;

        newProductButtons.IsVisible = product == null;
        existingProductButtons.IsVisible = product != null;

        if (product != null)
        {
            nameEntry.Text = product.Name;
            descriptionEntry.Text = product.Description;
            quantityEntry.Text = product.Quantity.ToString();
            priceEntry.Text = product.Price.ToString("F2");
            categoryEntry.Text = product.Category;
            UpdateTotal();
        }

        UpdateSaveButtonState();
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
            Console.WriteLine($"Quantity text: '{quantityEntry.Text}'");
            Console.WriteLine($"Price text: '{priceEntry.Text}'");

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

                Console.WriteLine($"Converted quantity: {quantity}");
                Console.WriteLine($"Converted price: {price}");
                Console.WriteLine($"Calculated total: {total}");
            }
            else
            {
                totalLabel.Text = "$ 0.00";
                Console.WriteLine("Could not convert quantity or price");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateTotal: {ex.Message}");
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

            if (_product == null)
                _product = new Product();

            _product.Name = nameEntry.Text;
            _product.Description = descriptionEntry.Text;
            _product.Quantity = int.Parse(quantityEntry.Text);
            _product.Price = decimal.Parse(priceEntry.Text);
            _product.Category = categoryEntry.Text;

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
                            $"Description: {descriptionEntry.Text}\n" +
                            $"Quantity: {quantityEntry.Text}\n" +
                            $"Price: ${priceEntry.Text}\n" +
                            $"Category: {categoryEntry.Text}\n" +
                            $"Total: {totalLabel.Text}";

            await Clipboard.SetTextAsync(productData);
            await DisplayAlert("Success", "Data copied to clipboard", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not copy the data: " + ex.Message, "OK");
        }
    }

    private async void OnDuplicateClicked(object sender, EventArgs e)
    {
        try
        {
            bool answer = await DisplayAlert("Confirm",
                "Do you want to create a new product with this data?",
                "Yes", "No");

            if (answer)
            {
                var newProduct = new Product
                {
                    Name = nameEntry.Text + " (Copy)",
                    Description = descriptionEntry.Text,
                    Quantity = int.Parse(quantityEntry.Text),
                    Price = decimal.Parse(priceEntry.Text),
                    Category = categoryEntry.Text
                };

                var newPage = new ProductPage(null);
                await Navigation.PushAsync(newPage);
                newPage.SetProductData(newProduct);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not duplicate the product: " + ex.Message, "OK");
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
        descriptionEntry.Text = product.Description;
        quantityEntry.Text = product.Quantity.ToString();
        priceEntry.Text = product.Price.ToString("F2");
        categoryEntry.Text = product.Category;
        UpdateTotal();
    }
}
