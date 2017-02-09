# MAC Address Vendor Lookup

Fast MAC address vendor lookup library. Uses [Wireshark's manufactures database of OUIs](https://www.wireshark.org/tools/oui-lookup.html) (Organizationally Unique Identifier).

Takes a MAC address in the form of .NET's [`System.Net.NetworkInformation.PhysicalAddress`](https://msdn.microsoft.com/en-us/library/system.net.networkinformation.physicaladdress(v=vs.110).aspx) and returns a matching `MacVendorInfo` if found. 

The `MacAddressVendorLookup` lib has the parsing and matching logic, and the `MacAddressVendorLookup.Vendors` lib contains a compressed form of Wireshark's manuf file (about 360KB) as an embedded resource.

The `VendorFileGenerator` project checks [Wireshark's Github repo](https://github.com/wireshark/wireshark) for udpates to the [manuf file](https://github.com/wireshark/wireshark/blob/master/manuf) and generates the `manuf_bin.zip` file used as the embedded resource in `MacAddressVendorLookup.Vendors`.

# Example
```C#
// Get vendor information for current machine's network interfaces

var vendorInfoProvider = new MacAddressVendorLookup.MacVendorBinaryReader();
using (var resourceStream = MacAddressVendorLookup.Vendors.ManufBinResource.GetStream().Result)
{
    vendorInfoProvider.Init(resourceStream).Wait();
}
var addressMatcher = new MacAddressVendorLookup.AddressMatcher(vendorInfoProvider);

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
