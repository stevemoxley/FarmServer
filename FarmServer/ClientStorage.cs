using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace FarmServer
{
    class ClientStorage
    {
        public int clientCounter = 0; //Assigns a unique clientID
        private List<Client> clients = new List<Client>(); //Main list for holding all the clients
        private Dictionary<int, Client> clientDictionary = new Dictionary<int,Client>(); //Indexed list for grabbing clients quickly. Same as the list
        Thread keepAliveThread;
        private Client curKeepAliveClient;

        public ClientStorage()
        {
            keepAliveThread = new Thread(new ThreadStart(KeepAlive));
            keepAliveThread.Start();
        }

        public int GetClientID()
        {
            return clientCounter;
        }

        public void addClient(Client client)
        {
            //Add to the lists
            clientCounter++;
            try
            {
                clients.Add(client);
                clientDictionary.Add(client.ClientID, client);
            }
            catch
            {
                clientCounter++;
                client.ClientID = clientCounter;
                clients.Add(client);
                clientDictionary.Add(client.ClientID, client);

            }
        }

        public void removeClient(Client client)
        {
            clients.Remove(client);
            clientDictionary.Remove(client.ClientID);
        }

        public void removeClient(int clientID)
        {
            Client remove = clientDictionary[clientID];
            clients.Remove(remove);
            clientDictionary.Remove(remove.ClientID);
        }

        public List<Client> getClients()
        {
            return clients;
        }

        public void sendPacketToAllClients(byte[] packet)
        {
            try
            {
                foreach (Client client in clients)
                {
                    curKeepAliveClient = client;
                    client.sendPacket(packet);
                }
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine("Client " + curKeepAliveClient.ClientID + " connection closed");
                PacketCreator.PlayerDisconnected(curKeepAliveClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client " + curKeepAliveClient.ClientID + " connection closed");
                PacketCreator.PlayerDisconnected(curKeepAliveClient);
            }
        }

        public void sendPacketToClientID(byte[] packet, int clientID)
        {
            Client sendTo = (Client)clientDictionary[clientID];
            sendTo.sendPacket(packet);
        }

        public Client getClientFromID(int clientID)
        {
            return clientDictionary[clientID];
        }

        public Client getClientFromTCP(TcpClient tcpClient)
        {
            foreach (Client client in clients)
            {
                if (client.tcpClient == tcpClient)
                    return client;
            }
            return null;
        }

        private void KeepAlive()
        {
            while (true)
            {
                try
                {

                    PacketWriter pw = new PacketWriter();
                    pw.Write((int)SendOPCodes.KEEPALIVE);
                    try
                    {
                        foreach (Client client in clients)
                        {

                            client.sendPacket(pw.ToArray());
                            curKeepAliveClient = client;

                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        PacketCreator.PlayerDisconnected(curKeepAliveClient);
                        continue;
                    }
                }
                catch(ObjectDisposedException ode)
                {
                    Console.WriteLine("Client "+curKeepAliveClient.ClientID+" connection closed");
                    PacketCreator.PlayerDisconnected(curKeepAliveClient);
                }

                Thread.Sleep(5 * 1000);
            }
        }
    }
}
