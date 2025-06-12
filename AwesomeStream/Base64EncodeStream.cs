using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace YYHEggEgg.AwesomeStream;

/// <summary>
/// Implement a transform stream that make <paramref name="input"/> be able to
/// be read as ASCII character stream of its base64 encode result.
/// </summary>
/// <param name="input"></param>
/// <param name="contentLength"></param>
/// <remarks>
/// The underlying stream's length must be known in advance.
/// If <paramref name="contentLength"/> is not provided, then
/// <paramref name="input"/> is required to support <see cref="Stream.Length"/>.
/// </remarks>
public sealed class Base64EncodeStream(Stream input, long contentLength = -1) : Stream
{
    private readonly long _contentLength = contentLength < 0 ? input.Length : contentLength;
    private long _inputPos = 0;

    #region Common Properties
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => (long)(Math.Ceiling((double)_contentLength / 3) * 4);

    public override bool CanTimeout => input.CanTimeout;
    public override int ReadTimeout => input.ReadTimeout;
    public override int WriteTimeout => input.WriteTimeout;

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
    #endregion

    #region Compatiable .NET Runtime Methods
    // The MIT License (MIT)
    // 
    // Copyright (c) .NET Foundation and Contributors
    // 
    // All rights reserved.
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.

    // No argument checking is done here. It is up to the caller.
    private static int ReadAtLeastCore(Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
    {
        Debug.Assert(minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new EndOfStreamException();
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }

    // No argument checking is done here. It is up to the caller.
#if NET8_0_OR_GREATER
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
#endif
    private static async ValueTask<int> ReadAtLeastAsyncCore(Stream stream, Memory<byte> buffer, int minimumBytes, bool throwOnEndOfStream, CancellationToken cancellationToken)
    {
        Debug.Assert(minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = await stream.ReadAsync(buffer.Slice(totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new EndOfStreamException();
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }
    #endregion

    #region Encoding Methods
    private static int WriteBase64EncodedBuffer(Span<byte> bytesSource, Span<byte> stringTarget)
    {
        var encoded = Convert.ToBase64String(bytesSource);
        return Encoding.ASCII.GetBytes(encoded, stringTarget);
    }

    public override int Read(Span<byte> buffer)
    {
        var padsCount = buffer.Length / 4;
        var readLimit = padsCount * 3;
        var remainingLength = _contentLength - _inputPos;
        var bytesBuffer = new byte[Math.Min(readLimit, remainingLength)];
        var read = ReadAtLeastCore(input, bytesBuffer, bytesBuffer.Length, false);
        if (read == 0) return 0;
        _inputPos += bytesBuffer.Length;
        return WriteBase64EncodedBuffer(bytesBuffer, buffer);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var padsCount = buffer.Length / 4;
        var readLimit = padsCount * 3;
        var remainingLength = _contentLength - _inputPos;
        var bytesBuffer = new byte[Math.Min(readLimit, remainingLength)];
        var read = await ReadAtLeastAsyncCore(input, bytesBuffer, bytesBuffer.Length, false, cancellationToken);
        if (read == 0) return 0;
        return WriteBase64EncodedBuffer(bytesBuffer, buffer.Span);
    }
    #endregion

    #region Redirected Methods
    public override int Read(byte[] buffer, int offset, int count) =>
        Read(new Span<byte>(buffer, offset, count));
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
    #endregion

    #region Throw Exception
    public override void Flush() => throw new NotImplementedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    #endregion
}
