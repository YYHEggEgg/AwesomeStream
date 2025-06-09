using System.Diagnostics;

namespace YYHEggEgg.AwesomeStream;

/// <summary>
/// Implement a <see cref="Stream"/> that concatenates multiple <see cref="Stream"/>.
/// </summary>
/// <remarks>
/// All underlying <see cref="Stream"/> <b>must be seekable</b> and
/// <b>support <see cref="Stream.Length"/></b>. They should share value of <see langword="true"/>
/// in at least one <see cref="Stream.CanRead"/> and <see cref="Stream.CanWrite"/>.
/// </remarks>
public sealed class CombinedStream : Stream
{
    public CombinedStream(IEnumerable<Stream> streams)
    {
        CanRead = true;
        CanWrite = true;
        CanSeek = true;
        CanTimeout = true;
        Length = 0;
        _streams = streams.ToArray();
        _streamLengths = new long[_streams.Length];
        _offsets = new long[_streams.Length];
        for (int i = 0; i < _streams.Length; i++)
        {
            var stream = _streams[i];
            if (!stream.CanSeek)
                throw new ArgumentException("Provided stream must all have CanSeek set.");
            CanRead &= stream.CanRead;
            CanWrite &= stream.CanWrite;
            CanTimeout &= stream.CanTimeout;
            _offsets[i] = Length;
            _streamLengths[i] = stream.Length;
            Length += _streamLengths[i];
        }
        if (!CanRead && !CanWrite)
            throw new ArgumentException("The provided stream must be all CanRead and/or all CanWrite.");
    }

    public CombinedStream(params Stream[] streamArray)
        : this(streams: streamArray)
    {
    }

    private readonly long[] _offsets;
    private readonly long[] _streamLengths;
    private readonly Stream[] _streams;

    #region Common Properties
    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }

    public override bool CanTimeout { get; }
    private int _readTimeout;
    public override int ReadTimeout
    {
        get => _readTimeout;
        set
        {
            _readTimeout = value;
            foreach (var stream in _streams) stream.ReadTimeout = value;
        }
    }
    private int _writeTimeout;
    public override int WriteTimeout
    {
        get => _writeTimeout;
        set
        {
            _writeTimeout = value;
            foreach (var stream in _streams) stream.WriteTimeout = value;
        }
    }

    private long _position;
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }
    #endregion

    #region Enumerate Methods
    private int FindOffsetStreamId(long offset)
    {
        if (offset >= Length)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"The current position exceeded sum of streams' length ({Length}).");
        int streamId;
        for (streamId = _offsets.Length - 1; streamId >= 0; streamId--)
        {
            if (offset >= _offsets[streamId]) break;
        }
        return streamId;
    }

    private IEnumerable<(Stream stream, long start, long length)> CalculateOpList(long offset, long length)
    {
        var startId = FindOffsetStreamId(offset);
        if (offset + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"The demanded operation's end position exceeded sum of streams' length ({Length}). Offset: {offset}, Length: {length}");
        for (int i = startId; i < _offsets.Length; i++)
        {
            var thisStreamOffset = offset - _offsets[i];
            var thisLength = Math.Min(length, _streamLengths[i] - thisStreamOffset);
            yield return (_streams[i], thisStreamOffset, thisLength);
            length -= thisLength;
            offset += thisLength;
            if (length <= 0)
            {
                Debug.Assert(length == 0);
                yield break;
            }
        }
    }
#endregion

    #region Global Operations
    public override void Flush()
    {
        foreach (var stream in _streams) stream.Flush();
    }
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        foreach (var stream in _streams) await stream.FlushAsync(cancellationToken);
    }
    public override void Close()
    {
        foreach (var stream in _streams) stream.Close();
    }
    public override async ValueTask DisposeAsync()
    {
        foreach (var stream in _streams) await stream.DisposeAsync();
    }
    #endregion

    #region Pointal Operations
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = offset;
                break;
            case SeekOrigin.Current:
                _position += offset;
                break;
            case SeekOrigin.End:
                _position = Length + offset;
                break;
            default:
                throw new ArgumentException("There is an invalid SeekOrigin.", nameof(origin));
        }
        return _position;
    }

    public override int ReadByte()
    {
        var streamId = FindOffsetStreamId(_position);
        var stream = _streams[streamId];
        stream.Seek(_position - _offsets[streamId], SeekOrigin.Begin);
        var res = stream.ReadByte();
        _position++;
        return res;
    }

    public override void WriteByte(byte value)
    {
        var streamId = FindOffsetStreamId(_position);
        var stream = _streams[streamId];
        stream.Seek(_position - _offsets[streamId], SeekOrigin.Begin);
        stream.WriteByte(value);
        _position++;
    }
    #endregion

    #region Throw Exception
    public override void SetLength(long value)
    {
        throw new NotSupportedException("Not allowed to extend combined stream.");
    }
    #endregion

    #region Data Operations
    public override int Read(Span<byte> buffer)
    {
        int readLength = 0;
        foreach (var (stream, start, length) in CalculateOpList(_position, buffer.Length))
        {
            stream.Seek(start, SeekOrigin.Begin);
            var spanlength = Convert.ToInt32(length);
            stream.Read(buffer.Slice(readLength, spanlength));
            readLength += spanlength;
        }
        _position += readLength;
        return readLength;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int readLength = 0;
        foreach (var (stream, start, length) in CalculateOpList(_position, buffer.Length))
        {
            stream.Seek(start, SeekOrigin.Begin);
            var spanlength = Convert.ToInt32(length);
            await stream.ReadAsync(buffer.Slice(readLength, spanlength), cancellationToken);
            readLength += spanlength;
        }
        _position += readLength;
        return readLength;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        int writeLength = 0;
        foreach (var (stream, start, length) in CalculateOpList(_position, buffer.Length))
        {
            stream.Seek(start, SeekOrigin.Begin);
            var spanlength = Convert.ToInt32(length);
            stream.Write(buffer.Slice(writeLength, spanlength));
            writeLength += spanlength;
        }
        _position += writeLength;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int writeLength = 0;
        foreach (var (stream, start, length) in CalculateOpList(_position, buffer.Length))
        {
            stream.Seek(start, SeekOrigin.Begin);
            var spanlength = Convert.ToInt32(length);
            await stream.WriteAsync(buffer.Slice(writeLength, spanlength), cancellationToken);
            writeLength += spanlength;
        }
        _position += writeLength;
    }

    #endregion

    #region Redirected Methods
    public override int Read(byte[] buffer, int offset, int count) =>
        Read(new Span<byte>(buffer, offset, count));
    public override void Write(byte[] buffer, int offset, int count) =>
        Write(new Span<byte>(buffer, offset, count));
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    #endregion
}
