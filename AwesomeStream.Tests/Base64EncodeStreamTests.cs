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

public partial class Base64EncodeStreamTests
{
    private static string OutputFromBase64EncodeStream(byte[] bytes)
    {
        var input = new MemoryStream(bytes);
        var transform = new Base64EncodeStream(input);
        var buf = new byte[transform.Length];
        var output = new MemoryStream(buf);
        transform.CopyTo(output);
        return Encoding.Default.GetString(buf);
    }

    private static string OutputFromBase64EncodeStream(byte[] bytes, int offset, int length)
    {
        var input = new MemoryStream(bytes, offset, length);
        var transform = new Base64EncodeStream(input);
        var buf = new byte[transform.Length];
        var output = new MemoryStream(buf);
        transform.CopyTo(output);
        return Encoding.Default.GetString(buf);
    }

    [Fact]
    public static void KnownByteSequence()
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

        Assert.Equal("BQYHCA==", OutputFromBase64EncodeStream(inputBytes));
    }

    [Fact]
    public static void ZeroLength()
    {
        byte[] inputBytes = Convert.FromBase64String("test");
        Assert.Equal(string.Empty, OutputFromBase64EncodeStream(inputBytes, 0, 0));
    }

    public static IEnumerable<object[]> ConvertToBase64StringTests_TestData()
    {
        yield return new object[] { Enumerable.Range(0, 0).Select(i => (byte)i).ToArray(), "" };
        yield return new object[] { Enumerable.Range(0, 1).Select(i => (byte)i).ToArray(), "AA==" };
        yield return new object[] { Enumerable.Range(0, 2).Select(i => (byte)i).ToArray(), "AAE=" };
        yield return new object[] { Enumerable.Range(0, 3).Select(i => (byte)i).ToArray(), "AAEC" };
        yield return new object[] { Enumerable.Range(0, 4).Select(i => (byte)i).ToArray(), "AAECAw==" };
        yield return new object[] { Enumerable.Range(0, 5).Select(i => (byte)i).ToArray(), "AAECAwQ=" };
        yield return new object[] { Enumerable.Range(0, 6).Select(i => (byte)i).ToArray(), "AAECAwQF" };
        yield return new object[] { Enumerable.Range(0, 7).Select(i => (byte)i).ToArray(), "AAECAwQFBg==" };
        yield return new object[] { Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), "AAECAwQFBgc=" };
        yield return new object[] { Enumerable.Range(0, 9).Select(i => (byte)i).ToArray(), "AAECAwQFBgcI" };
        yield return new object[] { Enumerable.Range(0, 10).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQ==" };
        yield return new object[] { Enumerable.Range(0, 11).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQo=" };
        yield return new object[] { Enumerable.Range(0, 12).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoL" };
        yield return new object[] { Enumerable.Range(0, 13).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA==" };
        yield return new object[] { Enumerable.Range(0, 14).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0=" };
        yield return new object[] { Enumerable.Range(0, 15).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0O" };
        yield return new object[] { Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODw==" };
        yield return new object[] { Enumerable.Range(0, 17).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxA=" };
        yield return new object[] { Enumerable.Range(0, 18).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAR" };
        yield return new object[] { Enumerable.Range(0, 19).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREg==" };
        yield return new object[] { Enumerable.Range(0, 20).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhM=" };
        yield return new object[] { Enumerable.Range(0, 21).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMU" };
        yield return new object[] { Enumerable.Range(0, 22).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFQ==" };
        yield return new object[] { Enumerable.Range(0, 23).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRY=" };
        yield return new object[] { Enumerable.Range(0, 24).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYX" };
        yield return new object[] { Enumerable.Range(0, 25).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGA==" };
        yield return new object[] { Enumerable.Range(0, 26).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBk=" };
        yield return new object[] { Enumerable.Range(0, 27).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBka" };
        yield return new object[] { Enumerable.Range(0, 28).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGw==" };
        yield return new object[] { Enumerable.Range(0, 29).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxw=" };
        yield return new object[] { Enumerable.Range(0, 30).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwd" };
        yield return new object[] { Enumerable.Range(0, 31).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHg==" };
        yield return new object[] { Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=" };
        yield return new object[] { Enumerable.Range(0, 33).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8g" };
        yield return new object[] { Enumerable.Range(0, 34).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gIQ==" };
        yield return new object[] { Enumerable.Range(0, 35).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISI=" };
        yield return new object[] { Enumerable.Range(0, 36).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIj" };
        yield return new object[] { Enumerable.Range(0, 37).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJA==" };
        yield return new object[] { Enumerable.Range(0, 38).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCU=" };
        yield return new object[] { Enumerable.Range(0, 39).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUm" };
        yield return new object[] { Enumerable.Range(0, 40).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJw==" };
        yield return new object[] { Enumerable.Range(0, 41).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJyg=" };
        yield return new object[] { Enumerable.Range(0, 42).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygp" };
        yield return new object[] { Enumerable.Range(0, 43).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKg==" };
        yield return new object[] { Enumerable.Range(0, 44).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKis=" };
        yield return new object[] { Enumerable.Range(0, 45).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKiss" };
        yield return new object[] { Enumerable.Range(0, 46).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLQ==" };
        yield return new object[] { Enumerable.Range(0, 47).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4=" };
        yield return new object[] { Enumerable.Range(0, 48).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4v" };
        yield return new object[] { Enumerable.Range(0, 49).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMA==" };
        yield return new object[] { Enumerable.Range(0, 50).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDE=" };
        yield return new object[] { Enumerable.Range(0, 51).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEy" };
        yield return new object[] { Enumerable.Range(0, 52).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMw==" };
        yield return new object[] { Enumerable.Range(0, 53).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ=" };
        yield return new object[] { Enumerable.Range(0, 54).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1" };
        yield return new object[] { Enumerable.Range(0, 55).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Ng==" };
        yield return new object[] { Enumerable.Range(0, 56).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc=" };
        yield return new object[] { Enumerable.Range(0, 57).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4" };
        yield return new object[] { Enumerable.Range(0, 58).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OQ==" };
        yield return new object[] { Enumerable.Range(0, 59).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo=" };
        yield return new object[] { Enumerable.Range(0, 60).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7" };
        yield return new object[] { Enumerable.Range(0, 61).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PA==" };
        yield return new object[] { Enumerable.Range(0, 62).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0=" };
        yield return new object[] { Enumerable.Range(0, 63).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+" };
        yield return new object[] { Encoding.Unicode.GetBytes("aaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd"), "YQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAA==" };
        yield return new object[] { Encoding.Unicode.GetBytes("vbnmbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "dgBiAG4AbQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgA" };
        yield return new object[] { Encoding.Unicode.GetBytes("rrrrbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd\0"), "cgByAHIAcgBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAAAA" };
        yield return new object[] { Encoding.Unicode.GetBytes("uuuubbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd\0feffe"), "dQB1AHUAdQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAAAAZgBlAGYAZgBlAA==" };
        yield return new object[] { Encoding.Unicode.GetBytes("kkkkkbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx\u043F\u0440\u0438\u0432\u0435\u0442\u043C\u0438\u0440\u4F60\u597D\u4E16\u754C"), "awBrAGsAawBrAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAeAA/BEAEOAQyBDUEQgQ8BDgEQARgT31ZFk5MdQ==" };
        yield return new object[] { Encoding.Unicode.GetBytes(",,,,bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx\u043F\u0440\u0438\u0432\u0435\u0442\u043C\u0438\u0440\u4F60\u597D\u4E16\u754Cddddeeeeea"), "LAAsACwALABiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgAPwRABDgEMgQ1BEIEPAQ4BEAEYE99WRZOTHVkAGQAZABkAGUAZQBlAGUAZQBhAA==" };
        yield return new object[] { Encoding.Unicode.GetBytes("____bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdcccd"), "XwBfAF8AXwBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAYwBjAGMAZAA=" };
        yield return new object[] { Encoding.Unicode.GetBytes("    bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccd"), "IAAgACAAIABiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQA" };
        yield return new object[] { Encoding.Unicode.GetBytes("\0\0bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "AAAAAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgA" };
        yield return new object[] { Encoding.Unicode.GetBytes("eeeebbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdgggdaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "ZQBlAGUAZQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABnAGcAZwBkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZAB4AA==" };

        var bigData1 = Enumerable.Range(0, 171085).Select(i => (byte)i).ToArray();
        yield return new object[] { bigData1, Convert.ToBase64String(bigData1) };
        var bigData2 = Enumerable.Range(0, 1710851).Select(i => (byte)i).ToArray();
        yield return new object[] { bigData2, Convert.ToBase64String(bigData2) };
    }

    [Theory]
    [MemberData(nameof(ConvertToBase64StringTests_TestData))]
    public static void ConvertToBase64String(byte[] inputBytes, string expectedBase64)
    {
        Assert.Equal(expectedBase64, OutputFromBase64EncodeStream(inputBytes));
    }

    [Fact]
    public static void ReadAfterEof()
    {
        var stream = new Base64EncodeStream(new MemoryStream(Encoding.UTF8.GetBytes("1")));
        Assert.Equal(4, stream.Read(new byte[4096]));
        Assert.Equal(0, stream.Read(new byte[4096]));
    }

    [Theory]
    [MemberData(nameof(ConvertToBase64StringTests_TestData))]
    public static void ConvertToBase64HttpData(byte[] inputBytes, string expectedBase64)
    {
        var transformStream = new Base64EncodeStream(new MemoryStream(inputBytes));
        var content = new StreamContent(transformStream);
        var outStream = new MemoryStream();
        content.CopyTo(outStream, null, default);
        outStream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(outStream);
        var outStr = reader.ReadToEnd();
        Assert.Equal(expectedBase64, outStr);
    }
}
