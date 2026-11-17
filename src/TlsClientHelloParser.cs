using System;
using System.Collections.Generic;
using System.Text;

namespace NologGuard
{
    public sealed class SniParseInfo
    {
        public bool IsTlsHandshake { get; set; }
        public int ExpectedRecordTotalLength { get; set; }
        public bool SniFound { get; set; }
        public string Hostname { get; set; }
        public int SniOffset { get; set; }
        public int SniLength { get; set; }
    }

    public struct ByteSlice
    {
        public int Offset;
        public int Count;

        public ByteSlice(int offset, int count)
        {
            Offset = offset;
            Count = count;
        }
    }

    /// <summary>
    /// Parses TLS ClientHello records to locate the exact byte span of the SNI extension
    /// and constructs non-overlapping TCP split slices.
    /// </summary>
    public static class TlsClientHelloParser
    {
        public static bool IsTlsClientHelloPrefix(byte[] buf, int length)
        {
            if (buf == null || length < 6) return false;
            return buf[0] == 0x16 && buf[1] == 0x03 && buf[2] <= 0x04 && buf[5] == 0x01;
        }

        public static int GetExpectedTlsRecordLength(byte[] buf, int length)
        {
            if (buf == null || length < 5) return 0;
            if (buf[0] != 0x16 || buf[1] != 0x03 || buf[2] > 0x04) return 0;
            int payloadLen = (buf[3] << 8) | buf[4];
            return 5 + payloadLen;
        }

        public static SniParseInfo Parse(byte[] buf, int length)
        {
            SniParseInfo info = new SniParseInfo();
            if (buf == null || length < 44) return info;
            if (!IsTlsClientHelloPrefix(buf, length)) return info;

            info.IsTlsHandshake = true;
            info.ExpectedRecordTotalLength = GetExpectedTlsRecordLength(buf, length);

            int limit = Math.Min(length, info.ExpectedRecordTotalLength);
            int pos = 9;

            if (pos + 34 > limit) return info;
            pos += 34;

            if (pos + 1 > limit) return info;
            int sessionIdLen = buf[pos];
            pos += 1 + sessionIdLen;
            if (pos + 2 > limit) return info;

            int cipherSuitesLen = (buf[pos] << 8) | buf[pos + 1];
            pos += 2 + cipherSuitesLen;
            if (pos + 1 > limit) return info;

            int compLen = buf[pos];
            pos += 1 + compLen;
            if (pos + 2 > limit) return info;

            int extTotalLen = (buf[pos] << 8) | buf[pos + 1];
            pos += 2;
            int extEnd = Math.Min(limit, pos + extTotalLen);

            while (pos + 4 <= extEnd)
            {
                int extType = (buf[pos] << 8) | buf[pos + 1];
                int extLen = (buf[pos + 2] << 8) | buf[pos + 3];
                pos += 4;

                if (extLen < 0 || pos + extLen > extEnd) break;

                if (extType == 0x0000 && extLen >= 5)
                {
                    int listLen = (buf[pos] << 8) | buf[pos + 1];
                    int p = pos + 2;
                    int listEnd = Math.Min(pos + extLen, p + listLen);

                    while (p + 3 <= listEnd)
                    {
                        byte nameType = buf[p];
                        int nameLen = (buf[p + 1] << 8) | buf[p + 2];
                        int nameStart = p + 3;

                        if (nameLen <= 0 || nameStart + nameLen > listEnd) break;

                        if (nameType == 0x00)
                        {
                            info.SniFound = true;
                            info.Hostname = Encoding.ASCII.GetString(buf, nameStart, nameLen);
                            info.SniOffset = nameStart;
                            info.SniLength = nameLen;
                            return info;
                        }

                        p = nameStart + nameLen;
                    }
                }

                pos += extLen;
            }

            return info;
        }

        public static List<ByteSlice> BuildSplitSlices(
            int length,
            SniSplitMode mode,
            int chunkSize,
            SniParseInfo sni)
        {
            List<ByteSlice> slices = new List<ByteSlice>();
            if (length <= 0) return slices;

            if (mode == SniSplitMode.Off || length <= 2)
            {
                slices.Add(new ByteSlice(0, length));
                return slices;
            }

            if (mode == SniSplitMode.SmartSni)
            {
                int p1 = Math.Min(2, length);
                slices.Add(new ByteSlice(0, p1));

                if (sni != null && sni.SniFound && sni.SniLength >= 2)
                {
                    int sniMid = sni.SniOffset + (sni.SniLength / 2);
                    if (sniMid > p1 && sniMid < length)
                    {
                        slices.Add(new ByteSlice(p1, sniMid - p1));
                        slices.Add(new ByteSlice(sniMid, length - sniMid));
                        return slices;
                    }
                }

                int mid = Math.Max(p1 + 1, length / 2);
                if (mid < length)
                {
                    slices.Add(new ByteSlice(p1, mid - p1));
                    slices.Add(new ByteSlice(mid, length - mid));
                }
                else if (p1 < length)
                {
                    slices.Add(new ByteSlice(p1, length - p1));
                }
                return slices;
            }

            int step = Math.Max(1, Math.Min(chunkSize, 64));
            int cursor = 0;

            int headerEnd = Math.Min(length, Math.Max(16, step * 4));
            cursor = AppendChunkedRange(slices, cursor, headerEnd, step);

            if (sni != null && sni.SniFound && sni.SniLength > 0)
            {
                int sniStart = Math.Max(cursor, sni.SniOffset);
                if (sniStart > cursor)
                {
                    slices.Add(new ByteSlice(cursor, sniStart - cursor));
                    cursor = sniStart;
                }

                int sniEnd = Math.Min(length, sni.SniOffset + sni.SniLength);
                if (sniEnd > cursor)
                {
                    cursor = AppendChunkedRange(slices, cursor, sniEnd, step);
                }
            }

            if (cursor < length)
            {
                slices.Add(new ByteSlice(cursor, length - cursor));
            }

            return slices;
        }

        private static int AppendChunkedRange(List<ByteSlice> slices, int start, int end, int step)
        {
            int pos = start;
            while (pos < end)
            {
                int count = Math.Min(step, end - pos);
                slices.Add(new ByteSlice(pos, count));
                pos += count;
            }
            return pos;
        }
    }
}
