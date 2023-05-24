using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VendorFileGenerator
{
    /// <summary>
    /// Run this to auto update the included manuf.bin.zip file.
    /// The MacAddressVendorLookup.DB project references/links to the manuf.bin.zip file in this project.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var manufFilePath = Path.GetFullPath("../../../manuf_bin.zip");
            var fileWasUpdated = ManufFileUpdater.UpdateManufBin(manufFilePath, testReadbackOfFile: true).Result;
            if (fileWasUpdated)
            {
                Console.WriteLine("manuf_bin.zip file was updated");
                Environment.ExitCode = 0;
            }
            else
            {
                Console.WriteLine("manuf_bin.zip file was already up-to-date");
                Environment.ExitCode = -1;
            }
        }
    }
}
