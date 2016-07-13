using System;
using System.Net;

namespace Stj.Security.Extensions
{
    public static class IPAddressExtensions
    {
        private static IPAddress empty = IPAddress.Parse("0.0.0.0");
        private static IPAddress intranetMask1 = IPAddress.Parse("10.255.255.255");
        private static IPAddress intranetMask2 = IPAddress.Parse("172.16.0.0");
        private static IPAddress intranetMask3 = IPAddress.Parse("172.31.255.255");
        private static IPAddress intranetMask4 = IPAddress.Parse("192.168.255.255");

        private static void CheckIPVersion(IPAddress ipAddress, IPAddress mask, out byte[] addressBytes, out byte[] maskBytes)
        {
            if (mask == null)
            {
                throw new ArgumentException();
            }
            addressBytes = ipAddress.GetAddressBytes();
            maskBytes = mask.GetAddressBytes();
            if (addressBytes.Length != maskBytes.Length)
            {
                throw new ArgumentException("The address and mask don't use the same IP standard");
            }
        }

        public static IPAddress And(this IPAddress ipAddress, IPAddress mask)
        {
            byte[] addressBytes;
            byte[] maskBytes;
            CheckIPVersion(ipAddress, mask, out addressBytes, out maskBytes);

            byte[] resultBytes = new byte[addressBytes.Length];
            for (int i = 0; i < addressBytes.Length; ++i)
            {
                resultBytes[i] = (byte)(addressBytes[i] & maskBytes[i]);
            }

            return new IPAddress(resultBytes);
        }

        public static bool IsOnIntranet(this IPAddress ipAddress)
        {
            if (empty.Equals(ipAddress))
            {
                return false;
            }
            bool onIntranet = IPAddress.IsLoopback(ipAddress);
            onIntranet = onIntranet ||
            ipAddress.Equals(And(ipAddress, intranetMask1)); //10.255.255.255
            onIntranet = onIntranet ||
            ipAddress.Equals(And(ipAddress, intranetMask4)); ////192.168.255.255
            onIntranet = onIntranet || (intranetMask2.Equals(And(ipAddress, intranetMask2))
            && ipAddress.Equals(And(ipAddress, intranetMask3)));
            return onIntranet;
        }
    }
}