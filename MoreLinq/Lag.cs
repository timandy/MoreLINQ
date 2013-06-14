namespace MoreLinq
{
    using System;
    using System.Collections.Generic;

    public static partial class MoreEnumerable
    {
        /// <summary>
        /// Produces a projection of a sequence by evaluating pairs of elements separated by a negative offset.
        /// </summary>
        /// <remarks>
        /// This operator evaluates in a deferred and streaming manner.<br/>
        /// For elements prior to the lag offset, <c>default(T) is used as the lagged value.</c><br/>
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of the source sequence</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence</typeparam>
        /// <param name="source">The sequence over which to evaluate lag</param>
        /// <param name="offset">The offset (expressed as a positive number) by which to lag each value of the sequence</param>
        /// <param name="resultSelector">A projection function which accepts the current and lagged items (in that order) and returns a result</param>
        /// <returns>A sequence produced by projecting each element of the sequence with its lagged pairing</returns>
        public static IEnumerable<TResult> Lag<TSource, TResult>(this IEnumerable<TSource> source, int offset, Func<TSource, TSource, TResult> resultSelector)
        {
            return Lag(source, offset, default(TSource), resultSelector);
        }

        /// <summary>
        /// Produces a projection of a sequence by evaluating pairs of elements separated by a negative offset.
        /// </summary>
        /// <remarks>
        /// This operator evaluates in a deferred and streaming manner.<br/>
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of the source sequence</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence</typeparam>
        /// <param name="source">The sequence over which to evaluate lag</param>
        /// <param name="offset">The offset (expressed as a positive number) by which to lag each value of the sequence</param>
        /// <param name="defaultLagValue">A default value supplied for the lagged value prior to the lag offset</param>
        /// <param name="resultSelector">A projection function which accepts the current and lagged items (in that order) and returns a result</param>
        /// <returns>A sequence produced by projecting each element of the sequence with its lagged pairing</returns>
        public static IEnumerable<TResult> Lag<TSource, TResult>(this IEnumerable<TSource> source, int offset, TSource defaultLagValue, Func<TSource, TSource, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            // NOTE: Theoretically, we could assume that negative (or zero-offset) lags could be
            //       re-written as: sequence.Lead( -lagBy, resultSelector ). However, I'm not sure
            //       that it's an intuitive - or even desirable - behavior. So it's being omitted.
            if (offset <= 0) throw new ArgumentOutOfRangeException("offset");

            return LagImpl(source, offset, defaultLagValue, resultSelector);
        }

        private static IEnumerable<TResult> LagImpl<TSource, TResult>(IEnumerable<TSource> source, int offset, TSource defaultLagValue, Func<TSource, TSource, TResult> resultSelector)
        {
            using (var iter = source.GetEnumerator())
            {
                var lagQueue = new Queue<TSource>(offset);
                // until we progress far enough, the lagged value is defaultLagValue
                var hasMore = true;
                // NOTE: The if statement below takes advantage of short-circuit evaluation
                //       to ensure we don't advance the iterator when we reach the lag offset.
                //       Do not reorder the terms in the condition!
                while (offset-- > 0 && (hasMore = iter.MoveNext()))
                {
                    lagQueue.Enqueue(iter.Current);
                    // until we reach the lag offset, the lagged value is the defaultLagValue
                    yield return resultSelector(iter.Current, defaultLagValue);
                }

                if (hasMore) // check that we didn't consume the sequence yet
                {
                    // now the lagged value is derived from the sequence
                    while (iter.MoveNext())
                    {
                        var lagValue = lagQueue.Dequeue();
                        yield return resultSelector(iter.Current, lagValue);
                        lagQueue.Enqueue(iter.Current);
                    }
                }
            }
        }
    }
}