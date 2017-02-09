using System;
using System.Collections.Generic;

namespace MacAddressVendorLookup
{
    public interface IMacVendorInfoProvider
    {
        IEnumerable<MacVendorInfo> GetEntries();
    }
}
