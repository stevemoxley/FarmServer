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
    class Client
    {
        public int ClientID;
        public TcpClient tcpClient;
        public Agent agent;
        public Thread tcpThread;

        public Client(TcpClient tcpClient, Thread tcpThread)
        {
            this.tcpClient = tcpClient;
            this.tcpThread = tcpThread;
            tcpThread.Start(this);
        }

        public void CreateAgent(int agentID)
        {
            agent = new Agent(agentID);
        }

        public void sendPacket(byte[] packet)
        {
            try
            {
                BinaryWriter BW = new BinaryWriter(tcpClient.GetStream());
                BW.Write(packet);
                BW.Flush();
                BW = null;
            }
            catch (ObjectDisposedException ode)
            {
                Program.ClientStorage.removeClient(this);
            }
            catch (IOException ioe)
            {
                Program.ClientStorage.removeClient(this);
            }
        }

        /// <summary>
        /// Saves the client information
        /// </summary>
        public void Save()
        {
            agent.Save();
        }

    }
}
