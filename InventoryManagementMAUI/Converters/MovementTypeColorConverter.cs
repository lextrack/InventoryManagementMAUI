using System.Globalization;

namespace InventoryManagementMAUI.Converters
{
    public class MovementTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value switch
            {
                "INCOMING" => Colors.Green,
                "OUTGOING" => Colors.IndianRed,
                _ => Colors.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
