using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;

namespace NexNovaCo.Web.Services;

// Conservative signature/container checks, not a decoder, sanitizer or malware scanner.
// Never serve user MIME types; uploads are inert raster content with nosniff.
internal static class ImageSignatureValidator
{
    public static void Validate(ReadOnlySpan<byte> bytes, string extension)
    {
        bool valid = extension switch
        {
            ".png" => Png(bytes),
            ".jpg" => Jpeg(bytes),
            ".webp" => WebP(bytes),
            _ => false
        };
        if (!valid) throw new ValidationException("The file is not a supported, intact JPG, PNG or WebP image.");
    }
    private static bool Dimensions(uint width, uint height) =>
        width is > 0 and <= 8192 && height is > 0 and <= 8192 && (ulong)width * height <= 32_000_000;
    private static uint Big(ReadOnlySpan<byte> b) => BinaryPrimitives.ReadUInt32BigEndian(b);
    private static uint Little(ReadOnlySpan<byte> b) => BinaryPrimitives.ReadUInt32LittleEndian(b);

    private static bool Png(ReadOnlySpan<byte> b)
    {
        if (b.Length < 57 || !b[..8].SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return false;
        var offset = 8; var header = false; var data = false;
        while (offset <= b.Length - 12)
        {
            var length = Big(b[offset..]);
            if (length > b.Length - offset - 12) return false;
            var size = (int)length;
            var type = b.Slice(offset + 4, 4);
            var chunk = b.Slice(offset + 8, size);
            if (Crc(b.Slice(offset + 4, size + 4)) != Big(b[(offset + 8 + size)..])) return false;
            if (!header)
            {
                if (!type.SequenceEqual("IHDR"u8) || size != 13 || !Dimensions(Big(chunk), Big(chunk[4..])) ||
                    chunk[10] != 0 || chunk[11] != 0 || chunk[12] > 1) return false;
                var depth = chunk[8]; var color = chunk[9];
                if (!(color switch { 0 => depth is 1 or 2 or 4 or 8 or 16, 2 or 4 or 6 => depth is 8 or 16, 3 => depth is 1 or 2 or 4 or 8, _ => false })) return false;
                header = true;
            }
            else if (type.SequenceEqual("IHDR"u8) || type.SequenceEqual("acTL"u8)) return false;
            if (type.SequenceEqual("IDAT"u8) && size > 0) data = true;
            offset += size + 12;
            if (type.SequenceEqual("IEND"u8)) return data && size == 0 && offset == b.Length;
        }
        return false;
    }
    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        return ~crc;
    }
    private static bool Jpeg(ReadOnlySpan<byte> b)
    {
        if (b.Length < 20 || b[0] != 0xff || b[1] != 0xd8 || b[^2] != 0xff || b[^1] != 0xd9) return false;
        var offset = 2; var frame = false; var scan = false;
        while (offset < b.Length)
        {
            if (b[offset++] != 0xff) return false;
            while (offset < b.Length && b[offset] == 0xff) offset++;
            if (offset >= b.Length) return false;
            var marker = b[offset++];
            if (marker == 0xd9) return frame && scan && offset == b.Length;
            if (marker is 0 or 0xd8 || marker is >= 0xd0 and <= 0xd7) return false;
            if (offset + 2 > b.Length) return false;
            var length = BinaryPrimitives.ReadUInt16BigEndian(b[offset..]);
            if (length < 2 || offset + length > b.Length) return false;
            if (marker is 0xc0 or 0xc2)
            {
                if (length < 8 || b[offset + 2] != 8 ||
                    !Dimensions(BinaryPrimitives.ReadUInt16BigEndian(b[(offset + 5)..]), BinaryPrimitives.ReadUInt16BigEndian(b[(offset + 3)..]))) return false;
                frame = true;
            }
            offset += length;
            if (marker != 0xda) continue;
            if (!frame) return false;
            scan = true;
            // Entropy bytes permit FF00 escaping and restart markers. Parse later scan headers too.
            while (offset < b.Length - 1)
            {
                if (b[offset] != 0xff) { offset++; continue; }
                var next = b[offset + 1];
                if (next == 0 || next is >= 0xd0 and <= 0xd7) { offset += 2; continue; }
                break;
            }
        }
        return false;
    }
    private static bool WebP(ReadOnlySpan<byte> b)
    {
        if (b.Length < 26 || !b[..4].SequenceEqual("RIFF"u8) || !b.Slice(8, 4).SequenceEqual("WEBP"u8) || Little(b[4..]) != b.Length - 8) return false;
        var offset = 12; var image = false;
        while (offset <= b.Length - 8)
        {
            var type = b.Slice(offset, 4); var length = Little(b[(offset + 4)..]);
            if (length > b.Length - offset - 8) return false;
            var chunk = b.Slice(offset + 8, (int)length);
            if (type.SequenceEqual("ANIM"u8) || type.SequenceEqual("ANMF"u8)) return false;
            if (type.SequenceEqual("VP8X"u8))
            {
                if (offset != 12 || length != 10 || (chunk[0] & 2) != 0 ||
                    !Dimensions(1 + U24(chunk[4..]), 1 + U24(chunk[7..]))) return false;
            }
            if (type.SequenceEqual("VP8 "u8))
            {
                if (image || length < 10 || (chunk[0] & 1) != 0 || !chunk.Slice(3,3).SequenceEqual(new byte[] {0x9d,1,0x2a}) ||
                    !Dimensions((uint)(BinaryPrimitives.ReadUInt16LittleEndian(chunk[6..]) & 0x3fff),
                        (uint)(BinaryPrimitives.ReadUInt16LittleEndian(chunk[8..]) & 0x3fff))) return false;
                image = true;
            }
            if (type.SequenceEqual("VP8L"u8))
            {
                if (image || length < 5 || chunk[0] != 0x2f) return false;
                var bits = Little(chunk[1..]);
                if ((bits >> 29) != 0 || !Dimensions((bits & 0x3fff) + 1, ((bits >> 14) & 0x3fff) + 1)) return false;
                image = true;
            }
            offset += 8 + (int)length + ((int)length & 1);
        }
        return image && offset == b.Length;
    }
    private static uint U24(ReadOnlySpan<byte> b) => (uint)(b[0] | b[1] << 8 | b[2] << 16);
}
