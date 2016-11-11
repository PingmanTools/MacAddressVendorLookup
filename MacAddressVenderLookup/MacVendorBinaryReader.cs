using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MacAddressVenderLookup
{
    /// <summary>
    /// Reads the compact binary format of vender info entries
    /// </summary>
    public class MacVendorBinaryReader : IMacVendorInfoProvider
    {
        public bool IsInitialized { get; private set; }

        List<MacVendorInfo> _entries = new List<MacVendorInfo>();

        public MacVendorBinaryReader()
        {
        }

        public async Task Init(Stream binaryVendorData)
        {
            IsInitialized = false;
            _entries.Clear();
            await Task.Run(() => ParseBinaryVendorData(binaryVendorData));
            IsInitialized = true;
        }

        void ParseBinaryVendorData(Stream data)
        {
            using (var binaryReader = new BinaryReader(data, Encoding.UTF8, leaveOpen: true))
            {
                while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
                {
                    byte maskLength = binaryReader.ReadByte();
                    var identBytes = new byte[8];
                    var identBytesContainerSize = maskLength / 8 + Math.Min(1, maskLength % 8);
                    for (var i = 0; i < identBytesContainerSize; i++)
                    {
                        identBytes[8 - (identBytesContainerSize - i)] = binaryReader.ReadByte();
                    }
                    var ident = BitConverter.ToInt64(identBytes, 0);

                    var name = binaryReader.ReadString();
                    var info = new MacVendorInfo(ident, maskLength, name.Length == 0 ? null : name);
                    _entries.Add(info);
                }
            }
        }

        public IEnumerable<MacVendorInfo> GetEntries()
        {
            if (!IsInitialized)
            {
                throw new Exception("Must be first be initialized");
            }
            return _entries;
        }
    }
}
