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

namespace AwesomeStream.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using YYHEggEgg.AwesomeStream;

public partial class Base64EncodeStreamTests_Async
{
    private static async Task<string> OutputFromBase64EncodeStream(byte[] bytes)
    {
        var input = new MemoryStream(bytes);
        var transform = new Base64EncodeStream(input);
        var buf = new byte[transform.Length];
        var output = new MemoryStream(buf);
        await transform.CopyToAsync(output);
        return Encoding.Default.GetString(buf);
    }

    private static async Task<string> OutputFromBase64EncodeStream(byte[] bytes, int offset, int length)
    {
        var input = new MemoryStream(bytes, offset, length);
        var transform = new Base64EncodeStream(input);
        var buf = new byte[transform.Length];
        var output = new MemoryStream(buf);
        await transform.CopyToAsync(output);
        return Encoding.Default.GetString(buf);
    }

    [Fact]
    public static async Task KnownByteSequence()
    {
        var inputBytes = new byte[4];
        for (int i = 0; i < 4; i++)
            inputBytes[i] = (byte)(i + 5);

        // The sequence of bits for this byte array is
        // 00000101000001100000011100001000
        // Encoding adds 16 bits of trailing bits to make this a multiple of 24 bits.
        // |        +         +         +         +
        // 000001010000011000000111000010000000000000000000
        // which is, (Interesting, how do we distinguish between '=' and 'A'?)
        // 000001 010000 011000 000111 000010 000000 000000 000000
        // B      Q      Y      H      C      A      =      =

        Assert.Equal("BQYHCA==", await OutputFromBase64EncodeStream(inputBytes));
    }

    [Fact]
    public static async Task ZeroLength()
    {
        byte[] inputBytes = Convert.FromBase64String("test");
        Assert.Equal(string.Empty, await OutputFromBase64EncodeStream(inputBytes, 0, 0));
    }

    public static IEnumerable<object[]> ConvertToBase64StringTests_TestData() =>
        Base64EncodeStreamTests.ConvertToBase64StringTests_TestData();

    [Theory]
    [MemberData(nameof(ConvertToBase64StringTests_TestData))]
    public static async Task ConvertToBase64String(byte[] inputBytes, string expectedBase64)
    {
        Assert.Equal(expectedBase64, await OutputFromBase64EncodeStream(inputBytes));
    }

    [Fact]
    public static async Task ReadAfterEof()
    {
        var stream = new Base64EncodeStream(new MemoryStream(Encoding.UTF8.GetBytes("1")));
        Assert.Equal(4, await stream.ReadAsync(new byte[4096]));
        Assert.Equal(0, await stream.ReadAsync(new byte[4096]));
    }

    [Theory]
    [MemberData(nameof(ConvertToBase64StringTests_TestData))]
    public static async Task ConvertToBase64HttpData(byte[] inputBytes, string expectedBase64)
    {
        var transformStream = new Base64EncodeStream(new MemoryStream(inputBytes));
        var content = new StreamContent(transformStream);
        var outStream = new MemoryStream();
        await content.CopyToAsync(outStream, null, default);
        outStream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(outStream);
        var outStr = reader.ReadToEnd();
        Assert.Equal(expectedBase64, outStr);
    }
}
