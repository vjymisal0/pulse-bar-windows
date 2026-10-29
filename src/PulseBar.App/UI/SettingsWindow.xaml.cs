using System.Windows;
using PulseBar.Core;

namespace PulseBar.App.UI;

/// <summary>Edits a copy of the settings; nothing changes until Save.</summary>
public partial class SettingsWindow : Window
{
    private readonly Settings _draft;

    public SettingsWindow(Settings current)
    {
        InitializeComponent();
        _draft = current.Clone();
        DataContext = _draft;
    }

    /// <summary>The validated settings after Save, or null if cancelled.</summary>
    public Settings? Result { get; private set; }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Result = _draft.Normalized();
        DialogResult = true;
    }
}
