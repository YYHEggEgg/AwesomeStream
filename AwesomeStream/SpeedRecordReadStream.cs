namespace YYHEggEgg.AwesomeStream;

/// <summary>
/// The data collector of <see cref="SpeedRecordReadStream"/>. This can be passed
/// to multiple <see cref="SpeedRecordReadStream"/> to be used concurrently.
/// </summary>
/// <remarks>
/// Observer must poll from <see cref="FetchAndCleanState"/> on a scheduled basis
/// to get the sum data. The invocation would also reset the collected number.
/// </remarks>
public class SpeedRecordCollection
{
    private long _passedBytes = 0;
    private DateTimeOffset _lastQuery = DateTimeOffset.UtcNow;

    public void PushRead(int length) => Interlocked.Add(ref _passedBytes, length);
    public (long BytesCount, TimeSpan Passed) FetchAndCleanState()
    {
        var nowTime = DateTimeOffset.UtcNow;
        var result = (_passedBytes, nowTime - _lastQuery);
        Interlocked.Add(ref _passedBytes, 0 - result._passedBytes);
        _lastQuery = nowTime;
        return result;
    }
}

/// <summary>
/// A <see cref="Stream"/> read wrapper to record the number of bytes passed
/// from read attempts.
/// </summary>
/// <param name="baseStream"></param>
/// <param name="reportTo"></param>
/// <remarks>
/// This transmitter only support and collect from Read attempts and don't
/// support Write (regardless of underlying <see cref="Stream"/>).
/// <see cref="Seek(long, SeekOrigin)"/> is supported if the underlying
/// <see cref="Stream"/> does.
/// </remarks>
public sealed class SpeedRecordReadStream(Stream baseStream, SpeedRecordCollection reportTo) : Stream
{
    public override bool CanRead => baseStream.CanRead;
    public override bool CanSeek => baseStream.CanSeek;
    public override bool CanWrite => false;

    public override long Length => baseStream.Length;

    public override long Position { get => baseStream.Position; set => baseStream.Position = value; }

    public override void Flush()
    {
        baseStream.Flush();
    }

    public override int Read(Span<byte> buffer)
    {
        var result = baseStream.Read(buffer);
        reportTo.PushRead(result);
        return result;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var result = await baseStream.ReadAsync(buffer, cancellationToken);
        reportTo.PushRead(result);
        return result;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => baseStream.Seek(offset, origin);
    public override void SetLength(long value) => baseStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
