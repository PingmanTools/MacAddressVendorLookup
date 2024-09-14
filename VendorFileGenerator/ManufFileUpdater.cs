using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using MacAddressVendorLookup;

namespace VendorFileGenerator
{
    public static class ManufFileUpdater
    {
        public const string WIRESHARK_MANUF_DOWNLOAD_URL = "https://www.wireshark.org/download/automated/data/manuf";

        /// <summary>
        /// Uses the Wireshark.org manuf download mirror to download the latest manuf file. Wireshark.org automatically updates this file
        /// Converts the Wireshark manuf into a smaller binary format and compresses. 
        /// </summary>
        /// <param name="pathToManufBinFile"></param>
        /// <param name="wiresharkManufHTTPDownloadUrl"></param>
        /// <param name="testReadbackOfFile"></param>
        /// <returns>Returns true if file was downloaded and updated, false if already have latest version</returns>
        public static async Task<bool> UpdateManufBin(string pathToManufBinFile, string wiresharkManufHTTPDownloadUrl = WIRESHARK_MANUF_DOWNLOAD_URL, bool testReadbackOfFile = false)
        {
            if (File.Exists(pathToManufBinFile))
            {
                try
                {
                    using var zipFile = ZipFile.OpenRead(pathToManufBinFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading existing file: "+ex.Message+" - skipping");
                }
            }
            
            
            var manufParser = new WiresharkManufReader();
            Console.WriteLine("Downloading latest manuf file from "+wiresharkManufHTTPDownloadUrl+"...");
            using (var manufMemStream = new MemoryStream())
            {
                // download manuf file into memory stream
                using (var httpResponse = await WebRequest.CreateHttp(wiresharkManufHTTPDownloadUrl).GetResponseAsync())
                using (var manufStream = httpResponse.GetResponseStream())
                {
                    await manufStream.CopyToAsync(manufMemStream);
                    manufMemStream.Position = 0;
                }

                // parse mem stream into vendor infos
                await manufParser.Init(manufMemStream);
            }

            // write vendor info into binary format into temp file
            var outputFile = Path.GetTempFileName();
            using (var outFileStream = File.OpenWrite(outputFile))
            {
                var vendorWriter = new MacVendorBinaryWriter(manufParser);
                vendorWriter.Write(outFileStream);
            }

            // true to test readback of file..
            if (testReadbackOfFile)
            {
                var vendorReader = new MacVendorBinaryReader();

                // test read back binary format of vendor info
                using (var fileReadStream = File.OpenRead(outputFile))
                {
                    await vendorReader.Init(fileReadStream);
                    var entries = vendorReader.GetEntries();
                }
                if (vendorReader.GetEntries().Count() != manufParser.GetEntries().Count())
                {
                    throw new Exception("Something wrong with writing or reading");
                }
                foreach (var entry in vendorReader.GetEntries())
                {
                    //Console.WriteLine(entry);
                }
            }

            File.Delete(pathToManufBinFile);
            using (var zipFileStream = ZipFile.Open(pathToManufBinFile, ZipArchiveMode.Create))
            {
                zipFileStream.CreateEntryFromFile(outputFile, "manuf.bin");
            }

            File.Delete(outputFile);

            return true;
        }
    }
}
