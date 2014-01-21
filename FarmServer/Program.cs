using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using FarmServer.Plants;

namespace FarmServer
{
    class Program
    {
        //Object Storage spaces
        public static ClientStorage ClientStorage;
        public static PlantStorage PlantStorage;

        //Networking Things
        private static TcpListener LISTENER;
        private static DatabaseConnection dbc;
        private static string ipAddress = "127.0.0.1";
        private static int port = 3000;

        static void Main(string[] args)
        {
            Console.WriteLine("Farm Game Server Software");
            IPAddress SERVERIP = IPAddress.Parse(ipAddress);
            LISTENER = new TcpListener(SERVERIP, port);
            Thread LISTENTHREAD;

            LISTENTHREAD = new Thread(new ThreadStart(LISTENFORCLIENTS));
            LISTENTHREAD.Start();

            Console.WriteLine("Listening on " + ipAddress + ":" + port.ToString());

            dbc = new DatabaseConnection();
            ClientStorage = new ClientStorage();
            PlantStorage = new PlantStorage();
            PlantStorage.Load(); //Load all of the plants
        }


        private static void LISTENFORCLIENTS()
        {
            LISTENER.Start();

            while (true)
            {
                TcpClient CLIENT = LISTENER.AcceptTcpClient();

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                Client newClient = new Client(CLIENT, clientThread);
            }
        }

        private static void HandleClientComm(object _client)
        {
            Client client = (Client)_client;
            NetworkStream clientStream = client.tcpClient.GetStream();

            byte[] message = new byte[65536];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 65536);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                PacketHandler PH = new PacketHandler();
                PH.Handle(message, bytesRead, client);

            }

            client.tcpClient.Close();
        }

    }
}
