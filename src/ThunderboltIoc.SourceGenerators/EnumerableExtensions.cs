namespace ThunderboltIoc.SourceGenerators
{
    public static class EnumerableExtensions
    {
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
            if (leftMoved == rightMoved)
            {
                leftE.Dispose();
                rightE.Dispose();
            }
            else
            {
                try
                { throw new InvalidOperationException(); }
                finally
                {
                    leftE.Dispose();
                    rightE.Dispose();
                }
            }
        }
        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
            => condition ? source.Where(predicate) : source;
        public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T>? source)
            => source?.Any() == true ? source : null;
        public static IEnumerable<T> AsEnumerable<T>(this T obj)
        {
            yield return obj;
        }
        public static IEnumerable<T> MoveSafely<T>(this IEnumerable<T> source, Func<Exception, T> failureItem)
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
                else if (exception is not null)
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
    }
}
