namespace YYHEggEgg.AwesomeStream;

/// <summary>
/// <see cref="MonitorStream"/> is designed for monitoring a certain
/// <see cref="Stream"/>'s availability (often in read-while-download
/// situations, like video streaming).<para/>
/// It wraps a <see cref="Stream"/>, and whenever the <see cref="MonitorStream"/>
/// is requested to write some data, we assume that data become 'available'. A
/// user can use related methods to see the available ranges' info or whether
/// a range is available currently.
/// </summary>
/// <remarks>
/// The underlying <see cref="Stream"/> <b>must support <see cref="Stream.Position"/></b>.
/// </remarks>
/// <param name="rangeManager">
/// The existing observer instance of this <see cref="MonitorStream"/> (or a
/// new one will be created).
/// </param>
public sealed class MonitorStream(Stream baseStream, MonitorRangeManager? rangeManager = null) : Stream
{
    /// <summary>
    /// The observer of this <see cref="MonitorStream"/>.
    /// <see cref="MonitorRangeManager.IsRangeAvailable(long, long)"/> and
    /// <see cref="MonitorRangeManager.MaximumReadable(long)"/> can be used
    /// to query the
    /// </summary>
    public readonly MonitorRangeManager RangeManager = rangeManager ?? new();

    #region Monitoring Method
    public override void Write(byte[] buffer, int offset, int count)
    {
        var pos = baseStream.Position;
        baseStream.Write(buffer, offset, count);
        Flush();
        RangeManager.Add(pos, pos + count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var pos = baseStream.Position;
        baseStream.Write(buffer);
        Flush();
        RangeManager.Add(pos, pos + buffer.Length);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var pos = baseStream.Position;
        await baseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        await FlushAsync(cancellationToken);
        RangeManager.Add(pos, pos + count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var pos = baseStream.Position;
        await baseStream.WriteAsync(buffer, cancellationToken);
        await FlushAsync(cancellationToken);
        RangeManager.Add(pos, pos + buffer.Length);
    }

    public override void WriteByte(byte value)
    {
        var pos = baseStream.Position;
        baseStream.WriteByte(value);
        Flush();
        RangeManager.Add(pos, pos + sizeof(byte));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return baseStream.Seek(offset, origin);
    }

    public override void Flush()
    {
        baseStream.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await baseStream.FlushAsync(cancellationToken);
    }
    #endregion

    #region Redirected Properties
    public override long Position { get => baseStream.Position; set => baseStream.Position = value; }
    public override long Length { get => baseStream.Length; }
    public override bool CanWrite { get => baseStream.CanWrite; }
    public override bool CanTimeout { get => baseStream.CanTimeout; }
    public override bool CanSeek { get => baseStream.CanSeek; }
    public override bool CanRead { get => baseStream.CanRead; }
    public override int ReadTimeout { get => baseStream.ReadTimeout; set => baseStream.ReadTimeout = value; }
    public override int WriteTimeout { get => baseStream.WriteTimeout; set => baseStream.WriteTimeout = value; }
    #endregion

    #region Redirected Methods
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => baseStream.BeginRead(buffer, offset, count, callback, state);
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => baseStream.BeginWrite(buffer, offset, count, callback, state);
    public override void Close() => baseStream.Close();
    public override void CopyTo(Stream destination, int bufferSize) => baseStream.CopyTo(destination, bufferSize);
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
    public override ValueTask DisposeAsync() => baseStream.DisposeAsync();
    public override int EndRead(IAsyncResult asyncResult) => baseStream.EndRead(asyncResult);
    public override void EndWrite(IAsyncResult asyncResult) => baseStream.EndWrite(asyncResult);
    public override int Read(Span<byte> buffer) => baseStream.Read(buffer);
    public override int Read(byte[] buffer, int offset, int count) => baseStream.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => baseStream.ReadAsync(buffer, cancellationToken);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => baseStream.ReadAsync(buffer, offset, count, cancellationToken);
    public override int ReadByte() => baseStream.ReadByte();
    public override void SetLength(long value) => baseStream.SetLength(value);
    #endregion
}

/// <summary>
/// Provides the range monitor algorithm basis for <see cref="MonitorStream"/>.
/// Operations are locked and support concurrent usage of multiple <see cref="MonitorStream"/>.
/// </summary>
/// <remarks>
/// Theoretically, <see cref="MonitorRangeManager"/>'s worst space complexity is
/// <c>O(n/2)</c>, but it cannot be reached unless writing bytes every other one.
/// However, the worst time complexity is <c>O(nlogn)</c>, which can be reached
/// when only <see cref="Stream.WriteByte(byte)"/> is used. Fortunately, its performance
/// can be nice when the writer of <see cref="Stream"/> uses proper chunk size.
/// </remarks>
public class MonitorRangeManager
{
    public List<(long start, long end)> Ranges { get; set; } = [];
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// An internal method to search which range includes the specified number,
    /// or which range (before the number) has the shorted distance to the number.
    /// </summary>
    /// <remarks>
    /// Though <see cref="Ranges"/> are defined as exclusive on the right, if a
    /// number matches one of the ranges' end, the method will still return
    /// <see langword="true"/>. That's because in the merging of ranges, an exclusive
    /// end and an inclusive start (at the same position) can be smoothly spliced,
    /// which make the logic simpler.
    /// </remarks>
    /// <param name="num">The specified number to be searched.</param>
    /// <param name="search_start">The inclusive binary search start index.</param>
    /// <param name="search_end">The exclusive binary search end index.</param>
    /// <param name="idx">
    /// The index of the searched range (whose meaning has been described in Summary).
    /// Notice that it can be <see langword="-1"/> when all elements in
    /// <see cref="Ranges"/> are located after <paramref name="num"/>.
    /// </param>
    /// <returns>
    /// When it returns <see langword="true"/>, it means the range at <paramref name="idx"/>
    /// includes <paramref name="num"/>, or the range's <c>end</c> matches
    /// <paramref name="num"/>; otherwise, it means the range at <paramref name="idx"/>
    /// locates before <paramref name="num"/> and has the shortest distance to it.
    /// </returns>
    private bool TrySearchNum(long num, int search_start, int search_end, out int idx)
    {
        // The search will be out of index when there are no
        // ranges. Just handle this corner case specially.
        if (search_end == 0)
        {
            idx = -1;
            return false;
        }

        var mid = (int)((search_start + search_end) * 0.5);
        // Get the middle of search_start and search_end first.
        var cur = Ranges[mid];
        if (cur.start > num)
        {
            // Because we want to search for the range that
            // contains or lower than the number, 'cur' will
            // never be the answer in this situation, so we
            // can give 'mid' to the exclusive 'search_end'.
            if (mid > 0) return TrySearchNum(num, search_start, mid, out idx);
            else
            {
                // We promise that 'idx' should be -1 when
                // all ranges are greater than it.
                idx = -1;
                return false;
            }
        }
        else
        {
            // Now we know the current range's start is no
            // greater than the number, so it can be the
            // answer of 'idx' as long as the next range's
            // start is greater than the number (no matter
            // whether the current range includes it);
            //
            // by the way we should consider the situation
            // where the current range is the final one in
            // the 'ranges' sequence.
            if ((mid + 1 == Ranges.Count) || (Ranges[mid + 1].start > num))
            {
                idx = mid;
                // It's easy to judge whether the number
                // is in the current range since the left
                // side is ensured to be lower than the
                // provided number.
                return cur.end >= num;
            }
            // Otherwise, just process on the binary search.
            else return TrySearchNum(num, mid, search_end, out idx);
        }
    }

    /// <summary>
    /// Report a specified range has become 'available' for reading.
    /// </summary>
    /// <param name="start">Inclusive start position of the range in the <see cref="Stream"/>.</param>
    /// <param name="end">Exclusive end position of the range in the <see cref="Stream"/>.</param>
    internal void Add(long start, long end)
    {
        _lock.EnterWriteLock();
        try
        {
            bool left_succ = TrySearchNum(start, 0, Ranges.Count, out var left_idx);
            bool right_succ = TrySearchNum(end, 0, Ranges.Count, out var right_idx);
            // The two numbers don't match any of the ranges.
            if (!left_succ && !right_succ)
            {
                // So, if 'left_idx' does not equal to
                // 'right_idx', then it means that some ranges
                // are included in this new one.
                if (left_idx != right_idx)
                {
                    for (int i = right_idx; i > left_idx; i--)
                        Ranges.RemoveAt(i);
                }
                Ranges.Insert(left_idx + 1, (start, end));
            }
            // Now the right side is included in another
            // range; that means we can simply expand it.
            else if (!left_succ && right_succ)
            {
                // We use the reversed sort to remove the
                // included ranges; the expanded range's
                // end depends on the first range to be
                // removed.
                long rmed_first_end = -1;
                for (int i = right_idx; i > left_idx; i--)
                {
                    if (rmed_first_end == -1) rmed_first_end = Ranges[i].end;
                    Ranges.RemoveAt(i);
                }
                if (rmed_first_end == -1)
                    throw new NotImplementedException($"Assertion failed: Not removed any in merge (rmed_first_end: {rmed_first_end})");
                Ranges.Insert(left_idx + 1, (start, rmed_first_end));
            }
            else if (left_succ && !right_succ)
            {
                // Because the left side is included now, we
                // should use 'left_idx' as inclusive, not
                // exclusive.
                long rmed_last_start = -1;
                for (int i = right_idx; i >= left_idx; i--)
                {
                    if (i == left_idx) rmed_last_start = Ranges[i].start;
                    Ranges.RemoveAt(i);
                }
                if (rmed_last_start == -1)
                    throw new NotImplementedException($"Assertion failed: Not removed any in merge (rmed_last_start: {rmed_last_start})");
                Ranges.Insert(left_idx, (rmed_last_start, end));
            }
            else // if (left_succ && right_succ)
            {
                // Just combine the two situations below:
                // remove all the sandwiched ranges, and
                // create a new one with 'start' of the
                // last removed range and 'end' of the first
                // removed range.
                long rmed_first_end = -1;
                long rmed_last_start = -1;
                for (int i = right_idx; i >= left_idx; i--)
                {
                    if (rmed_first_end == -1) rmed_first_end = Ranges[i].end;
                    if (i == left_idx) rmed_last_start = Ranges[i].start;
                    Ranges.RemoveAt(i);
                }
                Ranges.Insert(left_idx, (rmed_last_start, rmed_first_end));
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Query whether the specified range is 'available' for reading.
    /// </summary>
    /// <param name="inclusiveStart">Inclusive start position of the file.</param>
    /// <param name="exclusiveEnd">Exclusive end position of the file.</param>
    /// <returns></returns>
    public bool IsRangeAvailable(long inclusiveStart, long exclusiveEnd)
    {
        if (inclusiveStart == exclusiveEnd) return true;
        _lock.EnterReadLock();
        try
        {
            return TrySearchNum(inclusiveStart, 0, Ranges.Count, out var left_idx)
                && TrySearchNum(exclusiveEnd, 0, Ranges.Count, out var right_idx)
                && (left_idx == right_idx);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Query the farthest position from <paramref name="start"/> that is
    /// contiguously readable.
    /// </summary>
    /// <param name="start">The read start.</param>
    /// <returns>
    /// The farthest exclusive position, which ensures the data from
    /// <paramref name="start"/> to it is fully readable. The method returns
    /// <paramref name="start"/> if there're none.
    /// </returns>
    public long MaximumReadable(long start)
    {
        _lock.EnterReadLock();
        try
        {
            if (TrySearchNum(start, 0, Ranges.Count, out var idx))
                return Ranges[idx].end;
            else return start;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
