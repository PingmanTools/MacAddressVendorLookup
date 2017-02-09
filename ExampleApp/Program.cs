using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var vendorInfoProvider = new MacAddressVendorLookup.MacVendorBinaryReader();
            using (var resourceStream = MacAddressVendorLookup.ManufBinResource.GetStream().Result)
            {
                vendorInfoProvider.Init(resourceStream).Wait();
            }
            var addressMatcher = new MacAddressVendorLookup.AddressMatcher(vendorInfoProvider);

            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                .Where(ni => ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .Where(ni => !ni.GetPhysicalAddress().Equals(System.Net.NetworkInformation.PhysicalAddress.None));

            foreach (var ni in networkInterfaces)
            {
                var vendorInfo = addressMatcher.FindInfo(ni.GetPhysicalAddress());
                Console.WriteLine("\nAdapter: " + ni.Description);
                Console.WriteLine($"\t{vendorInfo}");
                var macAddr = BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':');
                Console.WriteLine($"\tMAC Address: {macAddr}");
            }
            Console.ReadKey();
        }
    }
}
