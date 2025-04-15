
using System.Net;

namespace BACnetScanner.LibClass
{
    public class Utils
    {
        public static int fnCheckIsNumber(string port)
        {
            if (int.TryParse(port, out int n))
            {
                return n;
            }
            return -1;
        }

        public static string fnCheckIsIP(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddress))
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                    ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    return ipAddress.ToString();
                }
            }
            return null;
        }
    }
}
