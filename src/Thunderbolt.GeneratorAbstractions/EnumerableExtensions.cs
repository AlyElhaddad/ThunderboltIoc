using System.Linq;

namespace Thunderbolt.GeneratorAbstractions;

public delegate bool SelectWhereDelegate<in T, TResult>(T item, out TResult result);
public static class EnumerableExtensions
{
    public static IEnumerable<TResult> SelectWhere<T, TResult>(this IEnumerable<T> source, SelectWhereDelegate<T, TResult> predicateSelector)
    {
        var enumerator = source.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                if (predicateSelector(enumerator.Current, out TResult result))
                {
                    yield return result;
                }
            }
        }
        finally
        {
            enumerator.Dispose();
        }
    }
    /// <remarks>
    /// does not implement fail-early paradigm
    /// </remarks>
    public static IEnumerable<(TLeft left, TRight right)> IndexTupleJoin<TLeft, TRight>(this IEnumerable<TLeft> left, IEnumerable<TRight> right)
    {
        IEnumerator<TLeft> leftE = left.GetEnumerator();
        IEnumerator<TRight> rightE = right.GetEnumerator();
        bool leftMoved, rightMoved;
        while ((leftMoved = leftE.MoveNext()) & (rightMoved = rightE.MoveNext())) //logical AND (&) is intended
        {
            yield return (leftE.Current, rightE.Current);
        }
        try
        {
            if (leftMoved != rightMoved)
                throw new InvalidOperationException();
        }
        finally
        {
            leftE.Dispose();
            rightE.Dispose();
        }
    }
    public static IEnumerable<T> If<T>(this IEnumerable<T> source, bool condition, Func<IEnumerable<T>, IEnumerable<T>> selector)
        => condition ? selector(source) : source;
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
        => condition ? source.Where(predicate) : source;
    public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T>? source)
        => source?.Any() == true ? source : null;
    public static IEnumerable<T> AsEnumerable<T>(this T obj)
    {
        yield return obj;
    }
    public static IEnumerable<T> MoveSafely<T>(this IEnumerable<T> source)
        => source.MoveSafely(null);
    public static IEnumerable<T> MoveSafely<T>(this IEnumerable<T> source, Func<Exception, T>? failureItem)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        IEnumerator<T> enumerator = source.GetEnumerator();
        while (true)
        {
            bool movedNext;
            Exception? exception = null;
            try
            {
                movedNext = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                movedNext = false;
                exception = ex;
            }
            if (movedNext)
            {
                yield return enumerator.Current;
            }
            else if (exception is not null && failureItem is not null)
            {
                //If you throw an exception in failureItem, you deserve it. I ain't gonna handle that.
                yield return failureItem(exception);
            }
            else
            {
                break;
            }
        }
        enumerator.Dispose();
    }
    public static bool TryFind<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T? result)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
        foreach (var item in source)
        {
            if (predicate(item))
            {
                result = item;
                return true;
            }
        }
        result = default;
        return false;
    }

    public static int FirstIndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        int currentIndex = -1;
        foreach (var item in source)
        {
            ++currentIndex;
            if (predicate(item))
            {
                return currentIndex;
            }
        }
        return -1;
    }

    public static IEnumerable<T> Exclude<T>(this IEnumerable<T> source, IEnumerable<T> excluded, IEqualityComparer<T>? equalityComparer)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (excluded is null)
            throw new ArgumentNullException(nameof(excluded));
        if (equalityComparer is null)
            equalityComparer = EqualityComparer<T>.Default;
        HashSet<T> set = new(equalityComparer);
        foreach(var item in excluded) set.Add(item);
        foreach(var item in source)
        {
            if (set.Add(item))
            {
                yield return item;
                set.Remove(item); //Linq.Except does not remove and therefore filters out elements
            }
        }
    }
    public static IEnumerable<T> Exclude<T>(this IEnumerable<T> source, IEnumerable<T> excluded)
        => source.Exclude(excluded, null);
}
