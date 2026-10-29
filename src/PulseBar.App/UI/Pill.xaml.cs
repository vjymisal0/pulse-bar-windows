using System.Windows;
using System.Windows.Controls;
using PulseBar.App.Services;
using PulseBar.Core;

namespace PulseBar.App.UI;

/// <summary>One "CPU 38% 71°" capsule in the bar.</summary>
public partial class Pill : UserControl
{
    public Pill()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => NameText.Text;
        set => NameText.Text = value;
    }

    public void SetMain(string text, Level level) => Set(ValueText, text, level);

    public void SetExtra(string? text, Level level)
    {
        ExtraText.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
        if (text is not null)
            Set(ExtraText, text, level);
    }

    private static void Set(TextBlock block, string text, Level level)
    {
        block.Text = text;
        // Resource reference, so colours follow live light/dark theme switches.
        block.SetResourceReference(TextBlock.ForegroundProperty, ThemeManager.Key(level));
    }
}
