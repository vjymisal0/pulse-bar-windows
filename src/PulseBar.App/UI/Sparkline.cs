using System.Windows;
using System.Windows.Media;

namespace PulseBar.App.UI;

/// <summary>Tiny bar chart of recent values.</summary>
public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    private IReadOnlyList<double?> _values = [];
    private double? _max = 100;

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public int Capacity { get; set; } = Core.History.DefaultCapacity;

    /// <param name="max">Value that fills the full height; null scales to the largest value shown.</param>
    public void SetValues(IReadOnlyList<double?> values, double? max = 100)
    {
        _values = values;
        _max = max;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        var baseline = Fill.Clone();
        baseline.Opacity = 0.15;
        drawingContext.DrawRectangle(baseline, null, new Rect(0, h - 1, w, 1));

        var max = _max ?? Math.Max(1, _values.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0).Max());
        var step = w / Capacity;
        var barWidth = Math.Max(1, step - 1);
        var offset = Capacity - _values.Count;
        for (var i = 0; i < _values.Count; i++)
        {
            if (_values[i] is not { } v)
                continue;
            var barHeight = Math.Max(1, Math.Min(1, v / max) * h);
            drawingContext.DrawRectangle(Fill, null, new Rect((offset + i) * step, h - barHeight, barWidth, barHeight));
        }
    }
}
