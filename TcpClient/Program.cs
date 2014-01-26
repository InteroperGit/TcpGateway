using System;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Spbec.TcpClient
{
    internal class Program
    {
        private static System.Net.Sockets.TcpClient TcpClient;

        private static byte[] ToBinary(string message)
        {
            return Enumerable.Range(0, message.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(message.Substring(x, 2), 16))
                             .ToArray();
        }

        private static void WaitRead()
        {
            Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var stream = TcpClient.GetStream();
                            var buffer = new byte[1024];
                            
                            while (true)
                            {
                                if (TcpClient.Client.Poll(100, SelectMode.SelectRead))
                                {
                                    break;
                                }
                            }

                            var bytesRead = stream.Read(buffer, 0, buffer.Length);
                            Console.WriteLine("Result message: {0}",
                                              BitConverter.ToString(buffer, 0, bytesRead).Replace("-", string.Empty));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message, e.StackTrace);
                        }
                    }
                });
        }

        private static void WaitWrite()
        {
            while (true)
            {
                try
                {
                    var message = Console.ReadLine();
                    var stream = TcpClient.GetStream();
                    var bt = ToBinary(message);
                    stream.Write(bt, 0, bt.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message, e.StackTrace);
                }
            }
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Trying connect to server...");
            try
            {
                var ip = ConfigurationManager.AppSettings["ip"];
                var port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
                TcpClient = new System.Net.Sockets.TcpClient(ip, port) { ReceiveTimeout = 30000 };
                WaitRead();
                WaitWrite();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message, e.StackTrace);
                Console.ReadKey();
            }
        }
    }
}