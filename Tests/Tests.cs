using System.Text;
using System.Net.NetworkInformation;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void DbStreamTest()
        {
            using (var resourceStream = MacAddressVendorLookup.ManufBinResource.GetStream().Result)
            {
                var buffer = new byte[10];
                int bytesRead = resourceStream.Read(buffer, 0, buffer.Length);
                Assert.IsTrue(bytesRead == buffer.Length, "Could not read more than 10 bytes from manufbin DB resource stream");
            }
        }

        [Test]
        public void ManufBinReaderTest()
        {
            using (var resourceStream = MacAddressVendorLookup.ManufBinResource.GetStream().Result)
            {
                var reader = new MacAddressVendorLookup.MacVendorBinaryReader();
                reader.Init(resourceStream).Wait();
                Assert.IsTrue(reader.GetEntries().Count() > 1, "BinaryReader did not get any vendor info entries");
            }
        }

        [Test]
        public void WiresharkManufFileReaderTest()
        {
            using (var manufTxtFile = File.OpenRead("manuf.txt"))
            {
                var reader = new MacAddressVendorLookup.WiresharkManufReader();
                reader.Init(manufTxtFile).Wait();
                Assert.IsTrue(reader.GetEntries().Count() > 1, "Wireshark Manuf.txt reader did not get any vendor info entries");
            }
        }

        [Test]
        public void TestMatcher()
        {
            var manuData = @"#
00-80-A3	Lantroni	Lantronix	# (see also 0800A3)
00:00:43	MicroTec    MICRO TECHNOLOGY
00:C0:57	MycoElec	Myco Electronics
00:50:C2:97:E0:00/36	RfIndust    RF Industries
08:00:29	Megatek Megatek Corporation
98:7B:F3	TexasIns    Texas Instruments
98:6D:35:A0:00:00/28	IwaveJap    iWave Japan, Inc. #Comment Test...
00:50:C2:43:B0:00/36	A3ip
01-10-18-00-00-00/24	FCoE-group
20-52-45-43-56-00/40	Receive
33-33-00-00-00-00/16	IPv6mcast
01-80-C2-00-00-00/44	Spanning-tree-(for-bridges)
";

            var expectedMatches = new Dictionary<PhysicalAddress, string> {
                { PhysicalAddress.Parse("00-00-43-F4-E1-B8"), "MICRO TECHNOLOGY" },
                { PhysicalAddress.Parse("00-C0-57-00-48-A4"), "Myco Electronics" },
                { PhysicalAddress.Parse("00-50-C2-97-E4-83"), "RF Industries" },
                { PhysicalAddress.Parse("08-00-29-26-0E-D1"), "Megatek Corporation" },
                { PhysicalAddress.Parse("98-7B-F3-28-74-9D"), "Texas Instruments" },
                { PhysicalAddress.Parse("98-6D-35-AA-39-61"), "iWave Japan, Inc." },
                { PhysicalAddress.Parse("00-50-C2-43-B7-56"), "A3ip" },
                { PhysicalAddress.Parse("01-10-18-63-93-18"), "FCoE-group" },
                { PhysicalAddress.Parse("20-52-45-43-56-11"), "Receive" },
                { PhysicalAddress.Parse("33-33-74-29-39-60"), "IPv6mcast" },
                { PhysicalAddress.Parse("01-80-C2-00-00-05"), "Spanning-tree-(for-bridges)" },
                { PhysicalAddress.Parse("00-80-A3-00-00-99"), "Lantronix" },
            };

            var expectedNonMatches = new[] {
                PhysicalAddress.Parse("98-6D-35-1A-39-61"),
                PhysicalAddress.Parse("01-80-C2-00-00-15"),
                PhysicalAddress.Parse("01-80-C2-00-00-15"),
                PhysicalAddress.Parse("01-00-43-F4-E1-B8"),
                PhysicalAddress.Parse("10-00-43-F4-E1-B8"),
                PhysicalAddress.Parse("33-23-74-29-39-60"),
                PhysicalAddress.Parse("33-32-74-29-39-60")
            };

            var reader = new MacAddressVendorLookup.WiresharkManufReader();
            var memStream = new MemoryStream(Encoding.UTF8.GetBytes(manuData));
            reader.Init(memStream).Wait();

            var addressMatcher = new MacAddressVendorLookup.AddressMatcher(reader);
        
            foreach(var m in expectedMatches)
            {
                var result = addressMatcher.FindInfo(m.Key);
                Assert.IsNotNull(result, "Expected match not found for " + m.Value);
                Assert.AreEqual(m.Value, result?.Organization, "Incorrect match - "+m.Value);
            }

            foreach(var m in expectedNonMatches)
            {
                var result = addressMatcher.FindInfo(m);
                Assert.IsNull(result, "Should not have found entry");
            }
            
        }
        
        [Test]
        public void CommentManufacturerTest()
        {
            var vendorInfoProvider = new MacAddressVendorLookup.MacVendorBinaryReader();
            using (var resourceStream = MacAddressVendorLookup.ManufBinResource.GetStream().Result)
            {
                vendorInfoProvider.Init(resourceStream).Wait();
            }
            var addressMatcher = new MacAddressVendorLookup.AddressMatcher(vendorInfoProvider);
            
            var addr = new PhysicalAddress(new byte[] { 0x00, 0x80, 0xa3, 0x00, 0x00, 0x00 });
            var vendorInfo = addressMatcher.FindInfo(addr);
            Assert.AreEqual(vendorInfo.Organization, "Lantronix", "Vendor mismatch! " + vendorInfo.Organization);
        }
    }
}
