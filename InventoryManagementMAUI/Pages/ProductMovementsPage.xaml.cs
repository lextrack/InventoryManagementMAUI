using InventoryManagementMAUI.Models;
using InventoryManagementMAUI.Services;
using System.Collections.ObjectModel;

namespace InventoryManagementMAUI.Pages;

public partial class ProductMovementsPage : ContentPage
{
    private readonly DatabaseService _database;
    private readonly Product _product;

    public ProductMovementsPage(Product product)
    {
        InitializeComponent();
        _database = new DatabaseService();
        _product = product;

        // Configurar el BindingContext para la informaci�n del producto
        BindingContext = new
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            Movements = new ObservableCollection<ProductMovement>()
        };

        LoadMovements();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMovements();
    }

    private async Task LoadMovements()
    {
        try
        {
            var movements = await _database.GetProductMovements(_product.Id);

            // Ordenar movimientos por fecha descendente (m�s reciente primero)
            movements = movements.OrderByDescending(m => m.Date).ToList();

            // Actualizar la colecci�n de movimientos
            var viewModel = (dynamic)BindingContext;
            var movementsCollection = (ObservableCollection<ProductMovement>)viewModel.Movements;
            movementsCollection.Clear();

            foreach (var movement in movements)
            {
                movementsCollection.Add(movement);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Could not load movements: " + ex.Message, "OK");
        }
    }
}