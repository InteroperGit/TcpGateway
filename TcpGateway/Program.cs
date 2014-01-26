using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Spbec.TcpGateway
{
    class Program
    {
        private static int _modemPort;

        private static int _clientPort;

        private static string _ipAddress;

        private static readonly Dictionary<int, TcpListener> ModemSideTcpListeners = new Dictionary<int, TcpListener>(); 

        private static readonly Dictionary<int, TcpClient> ModemSideTcpClients = new Dictionary<int, TcpClient>();

        private static readonly Dictionary<int, TcpListener> ClientSideTcpListeners = new Dictionary<int, TcpListener>(); 

        private static readonly Dictionary<int, TcpClient> ClientSideTcpClients = new Dictionary<int, TcpClient>();

        private static void CreatePipe(TcpClient inTcpClient, TcpClient outTcpClient)
        {
            Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        if (!inTcpClient.Connected || !outTcpClient.Connected)
                        {
                            return;
                        }

                        var inStream = inTcpClient.GetStream();
                        var data = new byte[1024];
                        var bytesRead = 0;
                        var chunkSize = 1;

                        while (true)
                        {
                            if (inTcpClient.Client.Poll(100, SelectMode.SelectRead))
                            {
                                break;
                            }
                        }

                        if (!inTcpClient.Connected || !outTcpClient.Connected)
                        {
                            return;
                        }

                        if (!inStream.DataAvailable)
                        {
                            break;
                        }

                        while (bytesRead < data.Length && chunkSize > 0 && inStream.DataAvailable)
                        {
                            bytesRead += chunkSize = inStream.Read(data, bytesRead, data.Length - bytesRead);
                        }

                        var endPoint = ((IPEndPoint) inTcpClient.Client.LocalEndPoint);
                        Console.WriteLine("{0}: Data recieved from {1}:{2}, {3}", DateTime.Now, endPoint.Address, endPoint.Port,
                            HexHelper.ToHex(data, bytesRead));

                        var outStream = outTcpClient.GetStream();
                        outStream.Write(data, 0, bytesRead);
                    }
                });
        }

        private static TcpListener StartTcpListener(IPAddress ipAddress, int port)
        {
            var tcpListener = new TcpListener(ipAddress, port);
            tcpListener.Start();
            Console.WriteLine("{0}: Tcp listener was started on IP address {1}, port {2}", DateTime.Now, ipAddress, port);
            return tcpListener;
        }

        private static void AcceptTcpClient(Dictionary<int, TcpListener> tcpListeners, Dictionary<int, TcpClient> tcpClients, int port)
        {
            var tcpListener = tcpListeners[port];

            while (true)
            {
                var tcpClient = tcpListener.AcceptTcpClient();
                tcpClient.ReceiveTimeout = 30000;
                Console.WriteLine("{0}: Connection was accepted on port {1}", DateTime.Now, port);

                if (tcpClients.ContainsKey(port))
                {
                    if (tcpClients[port].Connected)
                    {
                        tcpClients[port].Close();
                    }
                    tcpClients.Remove(port);
                }
                tcpClients.Add(port, tcpClient);
                TryCreatePipe();
            }
        }

        private static void TryCreatePipe()
        {
            if (ModemSideTcpClients.Count > 0 && ClientSideTcpClients.Count > 0)
            {
                CreatePipe(ModemSideTcpClients[_modemPort], ClientSideTcpClients[_clientPort]);
                CreatePipe(ClientSideTcpClients[_clientPort], ModemSideTcpClients[_modemPort]);
            }
        }

        private static void StartModemSide()
        {
            Task.Factory.StartNew(() =>
                {
                    var inEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _modemPort);
                    ModemSideTcpListeners.Add(_modemPort, StartTcpListener(inEndPoint.Address, inEndPoint.Port));

                    Console.WriteLine("{0}: Waiting modem connection...", DateTime.Now);
                    AcceptTcpClient(ModemSideTcpListeners, ModemSideTcpClients, inEndPoint.Port);
                });
        }

        private static void StartClientSide()
        {
            Task.Factory.StartNew(() =>
                {
                    var outEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _clientPort);
                    ClientSideTcpListeners.Add(_clientPort, StartTcpListener(outEndPoint.Address, outEndPoint.Port));

                    Console.WriteLine("{0}: Waiting client connection...", DateTime.Now);
                    AcceptTcpClient(ClientSideTcpListeners,  ClientSideTcpClients, outEndPoint.Port);
                });
        }

        static void Main(string[] args)
        {
            try
            {
                _ipAddress = ConfigurationManager.AppSettings["ip"];
                _modemPort = Convert.ToInt32(ConfigurationManager.AppSettings["inPort"]);
                _clientPort = Convert.ToInt32(ConfigurationManager.AppSettings["outPort"]);

                StartModemSide();
                StartClientSide();

                Console.WriteLine("Enter any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex.StackTrace);
                Console.ReadKey();
            }
        }
    }
}
