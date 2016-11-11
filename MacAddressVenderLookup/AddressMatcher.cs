using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace MacAddressVenderLookup
{
    public class AddressMatcher
    {

        Dictionary<byte, Dictionary<long, MacVendorInfo>> _dicts = new Dictionary<byte, Dictionary<long, MacVendorInfo>>();

        public AddressMatcher(IMacVendorInfoProvider ouiEntryProvider)
        {
            BuildEntryDictionaries(ouiEntryProvider);
        }

        void BuildEntryDictionaries(IMacVendorInfoProvider ouiEntryProvider)
        {
            foreach (var entry in ouiEntryProvider.GetEntries())
            {
                Dictionary<long, MacVendorInfo> entryDict;
                if (!_dicts.TryGetValue(entry.MaskLength, out entryDict))
                {
                    entryDict = new Dictionary<long, MacVendorInfo>();
                    _dicts.Add(entry.MaskLength, entryDict);
                }

                entryDict[entry.Identifier] = entry;
            }
        }

        const long MAX_LONG = unchecked((long)ulong.MaxValue);

        public MacVendorInfo FindInfo(PhysicalAddress macAddress)
        {
            var longBytes = new byte[8];
            var macAddrBytes = macAddress.GetAddressBytes();
            macAddrBytes.CopyTo(longBytes, 0);
            var identifier = IPAddress.HostToNetworkOrder(BitConverter.ToInt64(longBytes, 0));

            foreach (var dict in _dicts)
            {
                int mask = dict.Key;

                var maskedIdent = identifier & (MAX_LONG << (64 - mask));

                MacVendorInfo entry;
                if (dict.Value.TryGetValue(maskedIdent, out entry))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
