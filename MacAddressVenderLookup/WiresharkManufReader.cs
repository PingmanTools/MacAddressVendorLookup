using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MacAddressVenderLookup
{
    /// <summary>
    /// Parses the Wireshark manuf file of "Ethernet vendor codes, and well-known MAC addresses"
    /// Found at: https://code.wireshark.org/review/gitweb?p=wireshark.git;a=blob_plain;f=manuf
    /// </summary>
    public class WiresharkManufReader : IMacVendorInfoProvider
    {
        public bool IsInitialized { get; private set; }

        List<MacVendorInfo> _entries = new List<MacVendorInfo>();

        public WiresharkManufReader()
        {
        }

        /// <summary>
        /// Processes a stream of data in parallel. 
        /// </summary>
        /// <param name="manufData">A stream in the wireshark manuf file format </param>
        public async Task Init(Stream manufData)
        {
            IsInitialized = false;
            _entries.Clear();
            await Task.Run(() => ParseManufData(manufData));
            IsInitialized = true;
        }

        static IEnumerable<string> LineGenerator(StreamReader sr)
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                yield return line;
            }
        }

        void ParseManufData(Stream manufData)
        {
            var streamReader = new StreamReader(manufData, Encoding.UTF8);
            var bag = new ConcurrentBag<MacVendorInfo>();

            /* Line format examples:
                  00:0B:73 KodeosCo               # Kodeos Communications
                  00:50:C2:19:00:00/36 Goerlitz               # Goerlitz AG
                  58:FC:DB:00:00:00/28 SpangPow               # Spang Power Electronics
                  00-BF-00-00-00-00/16 MS-NLB-VirtServer
                  00-E0-2B-00-00-04 Extreme-EAPS
                  01-00-0C-00-00/40 ISL-Frame
                  01-00-5E/25    IPv4mcast
                  01-20-25/25    Control-Technology-Inc's-Industrial-Ctrl-Proto.
                  01-00-3C    Auspex-Systems-(Serverguard)
                  09-00-56-FF-00-00/32 Stanford-V-Kernel,-version-6.0
            */

            Parallel.ForEach(LineGenerator(streamReader), line =>
            {
                if (line.TrimStart(new char[0]).StartsWith("#", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                var parts = line.Split(new char[0], 2, StringSplitOptions.RemoveEmptyEntries);
                var macStr = parts[0];
                var descParts = parts[1].Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                var shortName = descParts[0].Trim();

                string longName = null;
                if (descParts.Length > 1)
                {
                    longName = descParts[1].Trim();
                }

                byte mask = 0;
                if (macStr.Contains("/"))
                {
                    var macParts = macStr.Split(new[] { '/' }, 2, StringSplitOptions.None);
                    mask = byte.Parse(macParts[1], CultureInfo.InvariantCulture);
                    macStr = macParts[0];
                }

                var macHexParts = macStr.Split(new[] { ':', '-', '.' }, StringSplitOptions.None);
                var macBytes = new byte[8];
                if (mask == 0)
                {
                    mask = (byte)(macHexParts.Length * 8);
                }

                for (var i = 0; i < macHexParts.Length; i++)
                {
                    macBytes[i] = Convert.ToByte(macHexParts[i], 16);
                }

                var identLong = BitConverter.ToInt64(macBytes, 0);
                identLong = IPAddress.HostToNetworkOrder(identLong);

                var entry = new MacVendorInfo(identLong, mask, longName ?? shortName);
                bag.Add(entry);

            });

            _entries = bag.ToList();
        }

        public IEnumerable<MacVendorInfo> GetEntries()
        {
            if (!IsInitialized)
            {
                throw new Exception("Must be first be initialized");
            }
            return _entries;
        }
    }
}
