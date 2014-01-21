using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarmServer.Plants;

namespace FarmServer
{
    class PacketCreator
    {
        private static PacketWriter PW;

        /// <summary>
        /// Sends a packet to all the players that a client has disconnected
        /// </summary>
        /// <param name="client">The client that disconnected</param>
        public static void PlayerDisconnected(Client client)
        {
            Program.ClientStorage.removeClient(client);
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.PLAYERDISCONNECTED);
            PW.Write(client.ClientID);
            Program.ClientStorage.sendPacketToAllClients(PW.ToArray());
        }

        /// <summary>
        /// Sends a packet to all players updating them on the status of a plant
        /// </summary>
        /// <param name="plant">The plant being updated</param>
        /// <param name="opCode">What to do the plant being updated</param>
        public static void PlantData(Plant plant, PlantSubOPCode opCode)
        {
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.PLANTDATA);
            PW.Write((int)opCode);
            PW.Write(plant.serialize());
            Program.ClientStorage.sendPacketToAllClients(PW.ToArray());
        }

        public static void UpdateAllPlantData()
        {
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.PLANTDATA);
            PW.Write((int)PlantSubOPCode.UpdateAll);
            PW.Write(Program.PlantStorage.serialize());
            Program.ClientStorage.sendPacketToAllClients(PW.ToArray());
        }
    }
}
