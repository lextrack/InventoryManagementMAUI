using System.Windows.Input;

namespace InventoryManagementMAUI.Pages;

public partial class AboutPage : ContentPage
{
    public ICommand OpenGitHubCommand { get; }

    public AboutPage()
    {
        InitializeComponent();
        OpenGitHubCommand = new Command(async () => await OpenGitHub());
        BindingContext = this;
    }

    private async Task OpenGitHub()
    {
        try
        {
            Uri uri = new Uri("https://github.com/lextrack/InventoryManagementMAUI");
            await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            // Handle exception (e.g., no browser available)
            await DisplayAlert("Error", "Could not open the link.", "OK");
        }
    }
}
