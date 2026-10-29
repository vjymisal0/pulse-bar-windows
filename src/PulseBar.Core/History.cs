namespace PulseBar.Core;

/// <summary>Fixed-size window of recent values for the sparklines. Oldest first.</summary>
public sealed class History(int capacity = History.DefaultCapacity)
{
    public const int DefaultCapacity = 40;

    private readonly Queue<double?> _values = new(capacity);

    public int Capacity { get; } = capacity;

    public void Add(double? value)
    {
        if (_values.Count == Capacity)
            _values.Dequeue();
        _values.Enqueue(value);
    }

    public IReadOnlyList<double?> Values => [.. _values];
}
