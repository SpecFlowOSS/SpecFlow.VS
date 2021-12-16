using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecFlow.VisualStudio.UI;

public class UiResourceProvider : IUiResourceProvider
{
    public static UiResourceProvider Instance = new();

    private readonly Lazy<ResourceDictionary> _iconResources = new(() =>
    {
        var dictionary = new ResourceDictionary();
        dictionary.Source = new Uri("pack://application:,,,/SpecFlow.VisualStudio.UI;Component/Icons.xaml",
            UriKind.Absolute);
        return dictionary;
    });

    public Image GetIcon(string iconName) => new() {Source = new DrawingImage(GetIconDrawing(iconName))};

    public Drawing GetIconDrawing(string key) => (Drawing) _iconResources.Value[key];
}
