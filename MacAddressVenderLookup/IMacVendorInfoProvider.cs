using System;
using System.Collections.Generic;

namespace MacAddressVenderLookup
{
    public interface IMacVendorInfoProvider
    {
        IEnumerable<MacVendorInfo> GetEntries();
    }
}
