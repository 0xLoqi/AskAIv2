using System.Windows;
using System.Windows.Controls;

namespace UI
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? UserTemplate { get; set; }
        public DataTemplate? AssistantTemplate { get; set; }
        public DataTemplate? SystemTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is Overlay.Message msg)
            {
                return msg.Role switch
                {
                    "user" => UserTemplate,
                    "assistant" => AssistantTemplate,
                    "system" => SystemTemplate,
                    _ => AssistantTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
    }

    public class TTSProviderIconConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isElevenLabs = value is bool b && b;
            return isElevenLabs ? "🧑‍🔬" : "🗣️";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 