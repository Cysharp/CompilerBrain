namespace CompilerBrain;

public static class SpanFormattableStringArrayExtensions
{
    extension(string?[] array)
    {
        public SpanFormattableStringArray Join(string separator) => new SpanFormattableStringArray(array, separator);
    }
}

public readonly struct SpanFormattableStringArray : ISpanFormattable
{
    readonly ReadOnlyMemory<string?> values;
    readonly string separator;

    public SpanFormattableStringArray(ReadOnlyMemory<string?> values, string separator)
    {
        this.values = values;
        this.separator = separator;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        var firstValue = true;
        var valuesSpan = values.Span;
        for (var i = 0; i < valuesSpan.Length; i++)
        {
            var item = valuesSpan[i];
            if (item == null) continue; // skip null values

            if (firstValue)
            {
                firstValue = false;

                if (item.Length > destination.Length)
                {
                    charsWritten = 0;
                    return false;
                }

                charsWritten += item.Length;
            }
            else
            {
                if (item.Length + separator.Length > destination.Length)
                {
                    charsWritten = 0;
                    return false;
                }

                separator.CopyTo(destination);
                destination = destination.Slice(separator.Length);

                charsWritten += item.Length + separator.Length;
            }

            item.CopyTo(destination);
            destination = destination.Slice(item.Length);
        }

        return true;
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
    public override string ToString() => $"{this}";
}
