//#define RLE_DEBUG

using System;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;

namespace Zavala {
    static public unsafe class UnsafeExt {
        /// <summary>
        /// Shuffles an unmanaged buffer's contents.
        /// </summary>
        static public void Shuffle<T>(T* buffer, int length, Random rng) where T : unmanaged {
            rng.Shuffle(buffer, length);
        }

        /// <summary>
        /// Removes an element from the given unmanaged array using the swap method.
        /// </summary>
        static public void FastRemoveAt<T>(T* buffer, ref int length, int index) where T : unmanaged {
            Assert.True(buffer != null, "Buffer is null");
            if (index != length - 1) {
                buffer[index] = buffer[length - 1];
            }
            length--;
        }

        static public string PrePostString(string inText, string inPrefix, string inPostfix) {
            inPrefix = inPrefix ?? string.Empty;
            inPostfix = inPostfix ?? string.Empty;

            if (inPrefix.Length + inPostfix.Length <= 0) {
                return inText;
            }

            int charBufferSize = inText.Length + inPrefix.Length + inPostfix.Length;
            char* charBuffer = stackalloc char[charBufferSize];

            char* charHead = charBuffer;
            int remaining = charBufferSize;

            int len = inPrefix.Length;
            if (len > 0) {
                fixed (char* pre = inPrefix) {
                    Unsafe.CopyArray(pre, len, charHead);
                    charHead += len;
                    remaining -= len;
                }
            }

            fixed (char* txt = inText) {
                len = inText.Length;
                Unsafe.CopyArray(txt, len, charHead);
                charHead += len;
                remaining -= len;
            }

            len = inPostfix.Length;
            if (len > 0) {
                fixed (char* post = inPostfix) {
                    Unsafe.CopyArray(post, len, charHead);
                }
            }

            return new string(charBuffer, 0, charBufferSize);
        }

        #region Compression

        private const int WindowSize = 256;
        private const int MinRunLength = 3;
        private const int MaxRunLength = 255 + MinRunLength;
        private const int MaxRunLengthThreshold = 32;

        private unsafe struct CompressionMatch
        {
            public byte* Start;
            public int Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CompressionHeader
        {
            public fixed byte Magic[4];
            public ushort Version;
            public ushort Flags;
            public uint UncompressedSize;
            public uint _Reserved;
        }

        static public byte[] Compress(byte[] src) {
            fixed (byte* srcPtr = src) {
                byte* tempBuffer = (byte*)Unsafe.Alloc(src.Length);
                int compressedSize = 0;
                Compress(srcPtr, src.Length, tempBuffer, src.Length, &compressedSize);
                byte[] result = new byte[compressedSize];
                Unsafe.CopyArray(tempBuffer, compressedSize, result);
                Unsafe.Free(tempBuffer);
                return result;
            }
        }

        static public bool Compress(byte* src, int srcSize, byte* dest, int destSize, int* compressedSize) {
            // if too small, don't bother compressing
            if (srcSize <= sizeof(CompressionHeader) + 3) {
                Unsafe.Copy(src, srcSize, dest, destSize);
                *compressedSize = srcSize;
                return false;
            }

            byte* initialSrc = src;
            byte* initialDest = dest;

            CompressionHeader header;
            header.Magic[0] = (byte)'L';
            header.Magic[1] = (byte)'Z';
            header.Magic[2] = (byte)'B';
            header.Magic[3] = (byte)'0';
            header.Version = 0;
            header.Flags = 0;
            header.UncompressedSize = (uint)srcSize;
            header._Reserved = 0;

            int finalSize = 0;
            Unsafe.Write(header, &dest, &finalSize, destSize);

            byte groupSize = 0;
            byte groupMask = 0;
            byte* groupHeaderPtr = src;
            byte* srcEnd = src + srcSize;
            byte* destEnd = dest + destSize;
            byte* windowStart = src;
            int windowSize = 0;
            CompressionMatch match;
            int matchOffset;
            int matchLength;

            while (src < srcEnd && dest < destEnd) {
                if (groupSize == 0) {
                    groupSize = 8;
                    groupMask = 0;
                    groupHeaderPtr = dest++;
                }

                long length = (src - windowStart);
                windowSize = (int)Math.Min(WindowSize, length);

                match = FindMatch(src, Math.Min((int)(srcEnd - src), MaxRunLength), windowSize);
                if (match.Length == 0) {
#if RLE_DEBUG
                    Log.Msg("[{0}] encoding literal: {1}", length, HexString(src, 1));
#endif // RLE_DEBUG
                    *dest++ = *src++;
                }
                else {
                    groupMask |= 1;
                    matchOffset = (int)(src - match.Start);
                    matchLength = match.Length;
#if RLE_DEBUG
                    Log.Msg("[{0}] encoding sequence <{1},{2}>: {3}", length, matchOffset, matchLength, HexString(match.Start, match.Length));
#endif // RLE_DEBUG
                    *dest++ = (byte)(matchOffset - 1);
                    *dest++ = (byte)(matchLength - MinRunLength);
                    src += match.Length;
                }

                if (--groupSize == 0) {
                    // flush mask
                    *groupHeaderPtr = groupMask;
                    // Log.Msg("wrote mask for previous 8 groups: {0}", HexString(groupMask));
                }
                else {
                    groupMask <<= 1;
                }
            }

            // flush mask
            if (groupSize > 0) {
                groupMask <<= (groupSize - 1);
                *groupHeaderPtr = groupMask;
                // Log.Msg("wrote mask for dangling {0} groups: {1}", 8 - groupSize, HexString(groupMask));
            }

            finalSize = (int)(dest - initialDest);

            // if the compressed size is not smaller, then don't bother with compression
            if (finalSize >= srcSize) {
                Unsafe.Copy(initialSrc, srcSize, initialDest, destSize);
                *compressedSize = srcSize;
                return false;
            }

            *compressedSize = finalSize;
            return true;
        }

        static private CompressionMatch FindMatch(byte* targetStart, int targetSize, int windowSize) {
            if (windowSize <= 0) {
                return default(CompressionMatch);
            }

            byte* bestStart = null;
            int bestLength = 0;
            // TODO: Make this... good?
            for (int i = 1; i <= windowSize; i++) {
                byte* seekPtrStart = targetStart - i;
                byte* seekPtr = seekPtrStart;
                byte* seekEnd = seekPtr + targetSize;
                byte* compPtr = targetStart;
                byte* compEnd = targetStart + targetSize;
                while (seekPtr < seekEnd && compPtr < compEnd) {
                    if (*seekPtr != *compPtr) {
                        break;
                    }
                    seekPtr++;
                    compPtr++;
                }

                int length = (int)(seekPtr - seekPtrStart);
                if (length >= 3 && length > bestLength) {
                    bestLength = length;
                    bestStart = seekPtrStart;

                    // if we reach a length of 16 then stop
                    if (length >= MaxRunLengthThreshold) {
                        break;
                    }
                }
            }

            return new CompressionMatch() {
                Start = bestStart,
                Length = bestLength
            };
        }

        static public bool PeekCompression(byte* src, int srcSize, out CompressionHeader header) {
            if (srcSize < sizeof(CompressionHeader)) {
                header = default(CompressionHeader);
                return false;
            }

            header = Unsafe.FastReinterpret<byte, CompressionHeader>(src);
            return header.Magic[0] == 'L' && header.Magic[1] == 'Z' && header.Magic[2] == 'B' && header.Magic[3] == '0';
        }

        static public bool Decompress(byte* src, int srcSize, byte* dest, int destSize, int* uncompressedSize) {
            CompressionHeader header;
            if (!PeekCompression(src, srcSize, out header)) {
                Unsafe.Copy(src, srcSize, dest, destSize);
                *uncompressedSize = srcSize;
                return true;
            }

            return Decompress(src + sizeof(CompressionHeader), srcSize - sizeof(CompressionHeader), dest, destSize, uncompressedSize, header);
        }

        static private bool Decompress(byte* src, int srcSize, byte* dest, int destSize, int* uncompressedSize, CompressionHeader header) {
            byte* srcEnd = src + srcSize;
            byte* destEnd = dest + header.UncompressedSize;
            byte* seekPtr;
            *uncompressedSize = (int)header.UncompressedSize;

            byte* destStart = dest;

            byte groupCount = 0;
            byte groupMask = 0;
            byte* runInfo = stackalloc byte[2];
            int runOffset, runLength;
            while (src < srcEnd && dest < destEnd) {
                if (groupCount == 0) {
                    groupMask = *src++;
                    groupCount = 8;
                    // Log.Msg("read mask for next 8 groups: {0}", HexString(groupMask));
                }

                long length = (dest - destStart);

                if ((groupMask & 0x80) != 0) {
                    // compressed
                    runInfo[0] = *src++;
                    runInfo[1] = *src++;

                    runOffset = runInfo[0] + 1;
                    runLength = runInfo[1] + MinRunLength;

                    seekPtr = dest - runOffset;

                    int lengthRemaining = runLength;
                    while(lengthRemaining-- > 0) {
                        *dest++ = *seekPtr++;
                    }

#if RLE_DEBUG
                    Log.Msg("[{0}] decoding sequence <{1},{2}>: {3}", length, runOffset, runLength, HexString(dest - runOffset - runLength, runLength));
#endif // RLE_DEBUG
                }
                else {
                    // literal
#if RLE_DEBUG
                    Log.Msg("[{0}] decoding literal: {1}", length, HexString(src, 1));
#endif // RLE_DEBUG
                    *dest++ = *src++;
                }

                groupMask <<= 1;
                groupCount--;
            }

            return src == srcEnd && dest == destEnd;
        }

        static public bool Decompress(byte[] src, out byte[] dest) {
            fixed (byte* srcPtr = src) {
                CompressionHeader header;
                if (!PeekCompression(srcPtr, src.Length, out header)) {
                    dest = new byte[src.Length];
                    Unsafe.CopyArray(srcPtr, src.Length, dest);
                    return true;
                }

                byte* decompressionBuffer = stackalloc byte[(int)header.UncompressedSize];
                int size = 0;
                Decompress(srcPtr + sizeof(CompressionHeader), src.Length - sizeof(CompressionHeader), decompressionBuffer, (int)header.UncompressedSize, &size, header);
                dest = new byte[size];
                Unsafe.CopyArray(decompressionBuffer, size, dest);
                return true;
            }
        }

        #endregion // Compression

        #region Encryption

        static private readonly byte[] XORKey = new byte[] { 0x05, 0x02, 0x14, 0x07 };

        static public string XORCrypt(string src) {
            int srcLength = src.Length, keyLength = XORKey.Length;
            char* buffer = stackalloc char[srcLength];
            for (int i = 0; i < srcLength; i++) {
                buffer[i] = (char)(src[i] ^ XORKey[i % keyLength]);
            }
            return new string(buffer, 0, srcLength);
        }

        #endregion // Encryption

#if RLE_DEBUG

        static private string HexString(byte* src, int srcSize) {
            int bufferSize = 3 * srcSize - 1;
            char* buffer = stackalloc char[bufferSize];
            for (int i = 0; i < srcSize - 1; i++) {
                buffer[i * 3 + 2] = ',';
            }
            for (int i = 0; i < srcSize; i++) {
                HexChars(src[i], buffer + (i * 3));
            }
            return new string(buffer, 0, bufferSize);
        }

        static private string HexString(byte src) {
            char* buffer = stackalloc char[2];
            HexChars(src, buffer);
            return new string(buffer, 0, 2);
        }

        static private void HexChars(byte val, char* buffer) {
            buffer[0] = HexSrc[val / 16];
            buffer[1] = HexSrc[val % 16];
        }

        private const string HexSrc = "0123456789ABCDEF";

#endif // RLE_DEBUG
    }
}