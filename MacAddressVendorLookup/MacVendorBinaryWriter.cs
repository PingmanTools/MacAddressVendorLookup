using System;
using System.IO;
using System.Text;

namespace MacAddressVendorLookup
{
    /// <summary>
    /// Writes a compact binary encoding format for vendor info entries
    /// </summary>
    public class MacVendorBinaryWriter
    {
        IMacVendorInfoProvider _vendorInfoProvider;

        public MacVendorBinaryWriter(IMacVendorInfoProvider vendorInfoProvider)
        {
            _vendorInfoProvider = vendorInfoProvider;
        }

        public void Write(Stream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var info in _vendorInfoProvider.GetEntries())
                {
                    writer.Write(info.MaskLength);
                    var identBytesContainerSize = info.MaskLength / 8 + Math.Min(1, info.MaskLength % 8);
                    var identBytes = BitConverter.GetBytes(info.Identifier);
                    writer.Write(identBytes, identBytes.Length - identBytesContainerSize, identBytesContainerSize);
                    writer.Write(info.Organization ?? "");
                }
            }
        }
    }
}
