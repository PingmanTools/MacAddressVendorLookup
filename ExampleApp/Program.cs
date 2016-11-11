using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ExampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var resourceStream = MacAddressVenderLookup.Vendors.ManufBinResource.GetStream().Result;
            var reader = new MacAddressVenderLookup.MacVendorBinaryReader();
            reader.Init(resourceStream).Wait();
            var entries = reader.GetEntries();
            var addressMatcher = new MacAddressVenderLookup.AddressMatcher(reader);

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(ni => !ni.GetPhysicalAddress().Equals(PhysicalAddress.None));

            foreach (var ni in networkInterfaces)
            {
                var vendorInfo = addressMatcher.FindInfo(ni.GetPhysicalAddress());
                Console.WriteLine("\nAdapter: " + ni.Description);
                Console.WriteLine($"\t{vendorInfo}");
                Console.WriteLine($"\tMAC Address: {BitConverter.ToString(ni.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':')}");
            }
            Console.ReadKey();
        }
    }
}
