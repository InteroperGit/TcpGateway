using System;
using System.Linq;

namespace Spbec.TcpGateway
{
    public static class HexHelper
    {
        public static string ToHex(byte[] data, int length = 0)
        {
            length = length > 0 ? length : data.Length;
            return BitConverter.ToString(data, 0, length).Replace("-", string.Empty);
        }

        public static byte[] ToBinary(string data)
        {
            return Enumerable.Range(0, data.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(data.Substring(x, 2), 16))
                     .ToArray();
        }
    }
}