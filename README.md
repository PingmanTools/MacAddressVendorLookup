# MacAddressVendorLookup

# Example
```C#
// Get vendor information for current machine's network interfaces

var vendorInfoProvider = new MacAddressVenderLookup.MacVendorBinaryReader();
using (var resourceStream = MacAddressVenderLookup.Vendors.ManufBinResource.GetStream().Result)
{
    vendorInfoProvider.Init(resourceStream).Wait();
}
var addressMatcher = new MacAddressVenderLookup.AddressMatcher(vendorInfoProvider);

foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
{
    var vendorInfo = addressMatcher.FindInfo(ni.GetPhysicalAddress());
    Console.WriteLine("\nAdapter: " + ni.Description);
    var macAddr = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':');
    Console.WriteLine($"\tMAC Address: {macAddr}");
}

/* Output on Windows (in Parallels VM):

Adapter: Intel(R) 82574L Gigabit Network Connection
        [MacVendorInfo: IdentiferString=00:1C:42, Organization=Parallels, Inc.]
        MAC Address: 00:1C:42:B2:84:35
*/

/* Output on MacOS:

Adapter: en0
	[MacVendorInfo: IdentiferString=78:31:C1, Organization=Apple, Inc.]
	MAC Address: 78:31:C1:B7:C2:8E

Adapter: vnic0
	[MacVendorInfo: IdentiferString=00:1C:42, Organization=Parallels, Inc.]
	MAC Address: 00:1C:42:00:00:08

Adapter: en8
	[MacVendorInfo: IdentiferString=48:D7:05, Organization=Apple, Inc.]
	MAC Address: 48:D7:05:EA:15:A3
*/

```
