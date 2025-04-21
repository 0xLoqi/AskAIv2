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
} 