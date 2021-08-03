using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecFlow.VisualStudio.UI
{
    public class UiResourceProvider : IUiResourceProvider
    {
        public static UiResourceProvider Instance = new UiResourceProvider();

        private readonly Lazy<ResourceDictionary> _iconResources = new Lazy<ResourceDictionary>(() =>
        {
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("pack://application:,,,/SpecFlow.VisualStudio.UI;Component/Icons.xaml", UriKind.Absolute);
            return dictionary;
        });

        public Drawing GetIconDrawing(string key)
        {
            return (Drawing)_iconResources.Value[key];
        }

        public Image GetIcon(string iconName)
        {
            return new Image {Source = new DrawingImage(GetIconDrawing(iconName))};
        }
    }
}
