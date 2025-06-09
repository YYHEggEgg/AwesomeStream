namespace YYHEggEgg.AwesomeStream;

/// <summary>
/// Implement a <see cref="Stream"/> that maps a specified range of provided
/// <see cref="Stream"/>
/// </summary>
/// <remarks>
/// The underlying <see cref="Stream"/> <b>must support <see cref="Stream.Position"/></b>.
/// <see cref="Stream.CanSeek"/> is not required as long as <see cref="Stream.Position"/>
/// matches the demanded offset when constructing.
/// </remarks>
public sealed class SpanAccessStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _baseStreamOffset;
    private readonly long _maxLength;

    public SpanAccessStream(Stream baseStream, long baseStreamOffset, long maxLength)
    {
        _baseStream = baseStream;
        _baseStreamOffset = baseStreamOffset;
        _maxLength = maxLength;
        if (_baseStream.Position != baseStreamOffset)
            _baseStream.Seek(baseStreamOffset, SeekOrigin.Begin);
    }

    #region Span Limiting Methods
    public override long Length { get => _maxLength; }
    public override long Position
    {
        get => _baseStream.Position - _baseStreamOffset;
        set
        {
            long targetPos = _baseStreamOffset + value;
            if (targetPos < _baseStreamOffset)
                throw new ArgumentOutOfRangeException(nameof(value), "An attempt was made to move the position before the beginning of the stream.");
            _baseStream.Position = targetPos;
        }
    }
    public override bool CanRead { get => _baseStream.CanRead; }
    public override bool CanWrite { get => _baseStream.CanWrite; }
    public override bool CanTimeout { get => _baseStream.CanTimeout; }
    public override int ReadTimeout { get => _baseStream.ReadTimeout; set => _baseStream.ReadTimeout = value; }
    public override int WriteTimeout { get => _baseStream.WriteTimeout; set => _baseStream.WriteTimeout = value; }

    public override bool CanSeek { get => _baseStream.CanSeek; }
    public override long Seek(long offset, SeekOrigin origin)
    {
        long targetOffset;
        switch (origin)
        {
            case SeekOrigin.Begin:
                targetOffset = _baseStreamOffset + offset;
                break;
            case SeekOrigin.Current:
                targetOffset = _baseStream.Position + offset;
                break;
            case SeekOrigin.End:
                targetOffset = _baseStreamOffset + _maxLength - 1 + offset;
                break;
            default:
                throw new ArgumentException("There is an invalid SeekOrigin.", nameof(origin));
        }
        #region Raise Exception
        if (targetOffset < _baseStreamOffset)
            throw new ArgumentOutOfRangeException(nameof(offset), "An attempt was made to move the position before the beginning of the stream.");
        #endregion

        return _baseStream.Seek(targetOffset, SeekOrigin.Begin) - _baseStreamOffset;
    }
    #endregion

    #region Reading Methods
    private long AvailableBytes()
    {
        var res = _baseStreamOffset + _maxLength - _baseStream.Position;
        if (res < 0) return 0;
        else return res;
    }

    public override void Close() => _baseStream.Close();
    public override ValueTask DisposeAsync() => _baseStream.DisposeAsync();
    public override int Read(Span<byte> buffer)
    {
        var maxlen = AvailableBytes();
        if (maxlen < buffer.Length) buffer = buffer[..(int)Math.Min(maxlen, int.MaxValue)];
        return _baseStream.Read(buffer);
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        var maxlen = AvailableBytes();
        if (maxlen < buffer.Length) count = (int)Math.Min(maxlen, int.MaxValue);
        return _baseStream.Read(buffer, offset, count);
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var maxlen = AvailableBytes();
        if (maxlen < buffer.Length) buffer = buffer.Slice(0, (int)Math.Min(maxlen, int.MaxValue));
        return await _baseStream.ReadAsync(buffer, cancellationToken);
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var maxlen = AvailableBytes();
        if (maxlen < buffer.Length) count = (int)Math.Min(maxlen, int.MaxValue);
        return await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
    }
    public override int ReadByte()
    {
        var maxlen = AvailableBytes();
        if (maxlen < sizeof(byte)) return -1;
        return _baseStream.ReadByte();
    }
    #endregion

    #region Writing Methods
    private void CheckInRange(long writeCount)
    {
        if (_baseStream.Position + writeCount > _baseStreamOffset + _maxLength)
            throw new ArgumentOutOfRangeException(nameof(writeCount), "An attempt was made to write out of the stream.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        CheckInRange(count);
        _baseStream.Write(buffer, offset, count);
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckInRange(buffer.Length);
        _baseStream.Write(buffer);
    }
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        CheckInRange(count);
        await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        CheckInRange(buffer.Length);
        await _baseStream.WriteAsync(buffer, cancellationToken);
    }
    public override void WriteByte(byte value)
    {
        CheckInRange(sizeof(byte));
        _baseStream.WriteByte(value);
    }
    public override void Flush()
    {
        _baseStream.Flush();
    }
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _baseStream.FlushAsync(cancellationToken);
    }
    public override void SetLength(long value) =>
        throw new NotSupportedException($"SpanAccessStream has defined a fixed length when constructing.");
    #endregion
}
