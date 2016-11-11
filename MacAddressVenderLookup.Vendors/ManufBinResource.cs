using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MacAddressVenderLookup.Vendors
{
    public static class ManufBinResource
    {
        /// <summary>
        /// Gets the manuf.bin.zip embedded resource and decompresses into a MemoryStream
        /// </summary>
        /// <returns></returns>
        public static async Task<MemoryStream> GetStream()
        {
            var assembly = typeof(ManufBinResource).GetTypeInfo().Assembly;
            var resourceName = assembly.GetManifestResourceNames().First();
            var manufMemStream = new MemoryStream();
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (var zipStream = resourceStream)
            using (var zipArchive = new ZipArchive(zipStream))
            using (var manufStream = zipArchive.Entries[0].Open())
            {
                await manufStream.CopyToAsync(manufMemStream);
                manufMemStream.Position = 0;
                return manufMemStream;
            }

        }
    }
}
