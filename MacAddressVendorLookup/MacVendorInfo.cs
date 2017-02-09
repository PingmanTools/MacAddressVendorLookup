using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace MacAddressVendorLookup
{
    public class MacVendorInfo
    {
        Lazy<string> _identiferString;
        public string IdentiferString => _identiferString.Value;

        /// <summary>
        /// The MAC Address identifer mask encoded as a network-order (big endian) long
        /// </summary>
        public long Identifier { get; private set; }

        /// <summary>
        /// The bit length of the identifer mask
        /// </summary>
        public byte MaskLength { get; private set; }

        /// <summary>
        /// The long name of the vendor / organization associated with the identifer mask
        /// </summary>
        public string Organization { get; private set; }

        public MacVendorInfo(long identifier, byte maskLength, string organization)
        {
            Identifier = identifier;
            MaskLength = maskLength;
            Organization = organization;
            _identiferString = new Lazy<string>(() => GetIdentiferString(Identifier, MaskLength));
        }

        /// <summary>
        /// Gets a string representation of the mask in the smallest form possible. The mask length is appended similar to IP address CIDR format when the mask is not byte aligned.
        /// </summary>
        /// <returns>
        /// Examples:
        ///     "F4:96:51"
        ///     "00:52:18"
        ///     "00:55:DA:F0/28"
        ///     "40:D8:55:00:F0/36"
        ///     "20:53:45:4E:44/40"
        /// </returns>
        static string GetIdentiferString(long identifer, byte maskLength)
        {
            var bytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(identifer)).Take((int)Math.Ceiling(maskLength / 8d)).ToArray();
            var str = BitConverter.ToString(bytes).Replace('-', ':');
            // non-byte aligned, add mask..
            if (maskLength % 8 != 0)
            {
                str += $"/{maskLength}";
            }
            return str;
        }

        public override string ToString()
        {
            return $"[MacVendorInfo: IdentiferString={IdentiferString}, Organization={Organization}]";
        }
    }
}
