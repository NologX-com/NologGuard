using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NologGuard
{
    /// <summary>
    /// RFC 1035 / RFC 8484 wire-format DNS query encoder and response decoder.
    /// </summary>
    public static class DnsWireCodec
    {
        public const ushort TypeA = 1;
        public const ushort TypeAAAA = 28;

        public sealed class DnsParseResult
        {
            public int RCode { get; set; }
            public List<IPAddress> Addresses { get; set; }
            public int MinTtlSeconds { get; set; }
        }

        public static byte[] BuildQuery(string hostname, ushort qType)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                throw new ArgumentException("Hostname cannot be empty.", "hostname");
            }

            string clean = hostname.Trim().TrimEnd('.');
            if (clean.Length == 0 || clean.Length > 253)
            {
                throw new ArgumentException("Invalid hostname length.", "hostname");
            }

            using (MemoryStream ms = new MemoryStream(64))
            {
                ms.WriteByte(0x00); ms.WriteByte(0x00);
                ms.WriteByte(0x01); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(0x01);
                ms.WriteByte(0x00); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(0x00);

                string[] labels = clean.Split('.');
                for (int i = 0; i < labels.Length; i++)
                {
                    byte[] labelBytes = Encoding.ASCII.GetBytes(labels[i]);
                    if (labelBytes.Length == 0 || labelBytes.Length > 63)
                    {
                        throw new ArgumentException("Invalid DNS label in hostname.", "hostname");
                    }
                    ms.WriteByte((byte)labelBytes.Length);
                    ms.Write(labelBytes, 0, labelBytes.Length);
                }
                ms.WriteByte(0x00);

                ms.WriteByte((byte)((qType >> 8) & 0xFF));
                ms.WriteByte((byte)(qType & 0xFF));
                ms.WriteByte(0x00);
                ms.WriteByte(0x01);

                return ms.ToArray();
            }
        }

        public static DnsParseResult ParseResponse(byte[] data, ushort targetType)
        {
            DnsParseResult result = new DnsParseResult
            {
                RCode = -1,
                Addresses = new List<IPAddress>(),
                MinTtlSeconds = 60
            };

            if (data == null || data.Length < 12)
            {
                return result;
            }

            int flags = (data[2] << 8) | data[3];
            if ((flags & 0x8000) == 0)
            {
                return result;
            }

            result.RCode = flags & 0x0F;
            int qdCount = (data[4] << 8) | data[5];
            int anCount = (data[6] << 8) | data[7];

            int offset = 12;

            for (int i = 0; i < qdCount; i++)
            {
                offset = SkipName(data, offset);
                if (offset < 0 || offset + 4 > data.Length) return result;
                offset += 4;
            }

            int minTtl = int.MaxValue;

            for (int i = 0; i < anCount; i++)
            {
                offset = SkipName(data, offset);
                if (offset < 0 || offset + 10 > data.Length) break;

                ushort rrType = (ushort)((data[offset] << 8) | data[offset + 1]);
                ushort rrClass = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
                int ttl = (data[offset + 4] << 24) | (data[offset + 5] << 16) | (data[offset + 6] << 8) | data[offset + 7];
                int rdLen = (data[offset + 8] << 8) | data[offset + 9];
                offset += 10;

                if (rdLen < 0 || offset + rdLen > data.Length) break;

                if (rrClass == 1 && rrType == targetType)
                {
                    if ((targetType == TypeA && rdLen == 4) || (targetType == TypeAAAA && rdLen == 16))
                    {
                        byte[] ipBytes = new byte[rdLen];
                        Buffer.BlockCopy(data, offset, ipBytes, 0, rdLen);
                        result.Addresses.Add(new IPAddress(ipBytes));
                        if (ttl >= 0 && ttl < minTtl) minTtl = ttl;
                    }
                }

                offset += rdLen;
            }

            if (minTtl != int.MaxValue)
            {
                result.MinTtlSeconds = Math.Max(30, Math.Min(minTtl, 3600));
            }

            return result;
        }

        private static int SkipName(byte[] data, int offset)
        {
            int pos = offset;
            int jumps = 0;

            while (pos < data.Length)
            {
                byte len = data[pos];
                if (len == 0)
                {
                    return pos + 1;
                }

                if ((len & 0xC0) == 0xC0)
                {
                    if (pos + 1 >= data.Length) return -1;
                    return pos + 2;
                }

                if ((len & 0xC0) != 0)
                {
                    return -1;
                }

                pos += 1 + len;
                jumps++;
                if (jumps > 128) return -1;
            }

            return -1;
        }
    }
}
