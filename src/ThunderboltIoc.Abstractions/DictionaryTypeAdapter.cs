using System.Collections;
using System.Linq;

namespace ThunderboltIoc;

internal readonly struct DictionaryTypeAdapter<TKey, TSourceValue, TTargetValue> : IReadOnlyDictionary<TKey, TTargetValue>
    where TKey : notnull
    where TSourceValue : notnull, TTargetValue
    where TTargetValue : notnull
{
    private readonly IDictionary<TKey, TSourceValue> source;

    internal DictionaryTypeAdapter(in IDictionary<TKey, TSourceValue> source)
    {
        this.source = source;
    }

    public bool ContainsKey(TKey key)
    {
        return source.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TTargetValue value)
    {
        bool succeeded = source.TryGetValue(key, out var srcValue);
        value = srcValue!;
        return succeeded;
    }

    public TTargetValue this[TKey key] => source[key];

    public IEnumerable<TKey> Keys => source.Keys;

    public IEnumerable<TTargetValue> Values => source.Values.Cast<TTargetValue>().ToArray();

    public int Count => source.Count;

    public IEnumerator<KeyValuePair<TKey, TTargetValue>> GetEnumerator()
    {
        return new Enumerator(source.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(source.GetEnumerator());
    }

    private readonly struct Enumerator : IEnumerator<KeyValuePair<TKey, TTargetValue>>
    {
        private readonly IEnumerator<KeyValuePair<TKey, TSourceValue>> sourceEnumerator;
        internal Enumerator(in IEnumerator<KeyValuePair<TKey, TSourceValue>> sourceEnumerator)
        {
            this.sourceEnumerator = sourceEnumerator;
        }
        public KeyValuePair<TKey, TTargetValue> Current => new(sourceEnumerator.Current.Key, sourceEnumerator.Current.Value);

        public void Dispose()
        {
            sourceEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return sourceEnumerator.MoveNext();
        }

        public void Reset()
        {
            sourceEnumerator.Reset();
        }

        object IEnumerator.Current => sourceEnumerator.Current;
    }
}
