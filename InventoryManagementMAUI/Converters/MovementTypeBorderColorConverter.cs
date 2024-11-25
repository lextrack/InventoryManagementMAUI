using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagementMAUI.Converters
{
    public class MovementTypeBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value switch
            {
                "INCOMING" => new Color(0, 200, 0, 2),
                "OUTGOING" => new Color(200, 0, 0, 2),
                _ => Colors.LightGray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
