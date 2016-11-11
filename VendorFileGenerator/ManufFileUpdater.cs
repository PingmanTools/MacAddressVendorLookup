using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using MacAddressVenderLookup;

namespace VendorFileGenerator
{
    public static class ManufFileUpdater
    {

        public const string WIRESHARK_GITHUB_API = "https://api.github.com/repos/wireshark/wireshark/contents/";

        [DataContract]
        class GithubApiFileEntry
        {
            [DataMember]
            public string download_url { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string sha { get; set; }
        }

        static T DeserializeJson<T>(string result)
        {
            var jsonSer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result)))
            {
                ms.Position = 0;
                return (T)jsonSer.ReadObject(ms);
            }
        }

        /// <summary>
        /// Uses the Github API to checks the Wireshark repo for an updated manuf file. If found, downloads
        /// and converts into the smaller binary format and compresses. 
        /// </summary>
        /// <param name="pathToManufBinFile">Path of the manuf.bin.zip file. If exists will be used to see if an update is available. If doesn't exist will always download and create one.</param>
        /// <param name="testReadbackOfFile">True to force a readback/parse of the newly generated file to ensure it works.</param>
        /// <returns>Returns true if file was downloaded and updated, false if already have latest version</returns>
        public static async Task<bool> UpdateManufBin(string pathToManufBinFile, string wiresharkGithub = WIRESHARK_GITHUB_API, bool testReadbackOfFile = false)
        {
            string existingManufSha = "";

            if (File.Exists(pathToManufBinFile))
            {
                using (var zipFile = ZipFile.OpenRead(pathToManufBinFile))
                using (var shaEntry = zipFile.GetEntry("manuf.sha").Open())
                using (var shaStreamReader = new StreamReader(shaEntry, Encoding.UTF8))
                {
                    existingManufSha = await shaStreamReader.ReadToEndAsync();
                }
            }

            string manufDownloadLink;
            string manufLatestSha;

            // query github API for meta data about manuf file
            var httpRequest = WebRequest.CreateHttp(wiresharkGithub);
            httpRequest.UserAgent = "manuf";
            using (var httpResponse = await httpRequest.GetResponseAsync())
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8))
            {
                var repoMetadata = await streamReader.ReadToEndAsync();
                var contentsJson = DeserializeJson<GithubApiFileEntry[]>(repoMetadata);
                var manufMeta = contentsJson.FirstOrDefault(e => e.name == "manuf");
                manufLatestSha = manufMeta.sha;
                manufDownloadLink = manufMeta.download_url;
            }


            // we already have the latest based on sha comparison
            if (existingManufSha == manufLatestSha)
            {
                return false;
            }

            // download latest manuf and parse into vendor infos
            var manufParser = new WiresharkManufReader();
            using (var manufMemStream = new MemoryStream())
            {
                // download manuf file into memory stream
                using (var httpResponse = await WebRequest.CreateHttp(manufDownloadLink).GetResponseAsync())
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
                var shaEntry = zipFileStream.CreateEntry("manuf.sha");
                using (var shaStreamWriter = new StreamWriter(shaEntry.Open(), Encoding.UTF8))
                {
                    shaStreamWriter.Write(manufLatestSha);
                }
            }

            File.Delete(outputFile);

            return true;
        }
    }
}
