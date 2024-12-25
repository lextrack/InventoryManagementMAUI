using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Services;

namespace InventoryManagementMAUI.Pages;

public partial class ProductOutputPage : ContentPage
{
    private readonly DatabaseService _database;
    private readonly Product _product;
    private bool _isQuantityValid = false;

    public ProductOutputPage(Product product)
    {
        InitializeComponent();
        _database = new DatabaseService();
        _product = product;
        BindingContext = product;
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
            _isQuantityValid = int.TryParse(e.NewTextValue, out int quantity) &&
                              quantity > 0 &&
                              quantity <= _product.Quantity;

            quantityError.IsVisible = !_isQuantityValid;
            if (!_isQuantityValid)
            {
                quantityError.Text = quantity <= 0
                    ? "Quantity must be greater than 0"
                    : "Insufficient stock";
            }
        }

        registerButton.IsEnabled = _isQuantityValid;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        try
        {
            if (!_isQuantityValid || string.IsNullOrWhiteSpace(quantityEntry.Text))
            {
                await DisplayAlert("Error", "Please enter a valid quantity", "OK");
                return;
            }

            int quantity = int.Parse(quantityEntry.Text);
            string notes = notesEntry.Text ?? string.Empty;

            await _database.RegisterProductOutput(_product.Id, quantity, notes);
            await DisplayAlert("Success", "Output registered successfully", "OK");
            await Navigation.PopToRootAsync();

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}