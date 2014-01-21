using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarmServer.Plants;
using FarmServer.Items;
using System.ComponentModel;

namespace FarmServer
{
    class PacketHandler
    {
        PacketReader PR = null;
        PacketWriter PW;
        private delegate void Handler(Client client, byte[] packet, int bytesRead);
        private Dictionary<byte, Handler> Handlers = new Dictionary<byte, Handler>();


        public PacketHandler()
        {
            Handlers.Add((int)ReadOPCodes.LOGIN, HandleLogin);
            Handlers.Add((int)ReadOPCodes.KEEPALIVE, KeepAlive);
            Handlers.Add((int)ReadOPCodes.GETMAPDATA, GetMapData);
            Handlers.Add((int)ReadOPCodes.MOVEMENT, Movement);
            Handlers.Add((int)ReadOPCodes.LOADINVENTORY, LoadInventory);
            Handlers.Add((int)ReadOPCodes.PLANTDATA, PlantData);
            Handlers.Add((int)ReadOPCodes.PLAYERDISCONNECTED, PlayerDisconnect);
            Handlers.Add((int)ReadOPCodes.INVENTORYMOVE, InventoryMove);
            Handlers.Add((int)ReadOPCodes.USEITEM, UseItem);
        }

        public byte[] GetBytes(String _string)
        {
            ASCIIEncoding encoder = new ASCIIEncoding();
            return encoder.GetBytes(_string);
        }

        public void Handle(byte[] packet, int bytesRead, Client client)
        {
            PR = new PacketReader(packet, bytesRead, false);
            byte packetID = PR.ReadByte();
            Handler h = new Handler(Handlers[packetID]);
            h.Invoke(client, packet, bytesRead);
        }

        /// <summary>
        /// Called when a player first logs in
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void HandleLogin(Client client, byte[] packet, int bytesRead)
        {
            int getClientID = PR.ReadInt32();
            Client curClient = null;
            int clientID = -1;

            #region Assign ClientID
            if (getClientID == 1)
            {
                PW = new PacketWriter();
                PW.Write((int)SendOPCodes.LOGIN);
                PW.Write(1); //If I wanted to get a new ID
                clientID = Program.ClientStorage.GetClientID();
                PW.Write(clientID);

                Console.WriteLine("Client #" + clientID.ToString() + " has logged in.");

                //Send clients count
                PW.Write(Program.ClientStorage.getClients().Count);
                //Send the client all the information of the already logged in clients
                try
                {
                    foreach (Client oClient in Program.ClientStorage.getClients())
                    {
                        if (oClient != client) //Dont send information the inquiring client
                        {
                            PW.Write(oClient.agent.serialize(), 0, oClient.agent.serialize().Length);
                            curClient = oClient;
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error at Handle Login. Might or might not be an issue...");
                    PacketCreator.PlayerDisconnected(curClient);
                }

                client.sendPacket(PW.ToArray()); //Send response to client logging in
            }
            #endregion
            #region Use Already AssignedID
            else if (getClientID == 0)
            {
                clientID = PR.ReadInt32();
                PW = new PacketWriter();
                PW.Write((int)SendOPCodes.LOGIN);
                PW.Write(0); //If I wanted to get a new ID
                PW.Write(clientID);

                Console.WriteLine("Client #" + clientID + " has logged in.");

                //Send clients count
                PW.Write(Program.ClientStorage.getClients().Count);
                //Send the client all the information of the already logged in clients
                try
                {
                    foreach (Client oClient in Program.ClientStorage.getClients())
                    {
                        if (oClient != client) //Dont send information the inquiring client
                        {
                            PW.Write(oClient.agent.serialize(), 0, oClient.agent.serialize().Length);
                            curClient = oClient;
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error at Handle Login. Might or might not be an issue...");
                    PacketCreator.PlayerDisconnected(curClient);
                }

                client.sendPacket(PW.ToArray()); //Send response to client logging in
            }
            client.ClientID = clientID;
            client.CreateAgent(clientID);
            Program.ClientStorage.addClient(client);
            #endregion
            #region Inform Other Clients
            //Send a packet to all other clients making them aware that the new client logged in
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.NEWPLAYERLOGIN);
            try
            {
                foreach (Client oClient in Program.ClientStorage.getClients())
                {
                    if (oClient != client) //Dont send information to the inquiring client
                    {
                        PW.Write(client.agent.serialize(), 0, client.agent.serialize().Length);
                        oClient.sendPacket(PW.ToArray());
                        curClient = oClient;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Error at Handle Login. Might or might not be an issue...");
                PacketCreator.PlayerDisconnected(curClient);
            }
            #endregion
        }

        /// <summary>
        /// This is the keep alive packet.
        /// Doesnt do anything yet
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void KeepAlive(Client client, byte[] packet, int bytesRead)
        {


        }

        /// <summary>
        /// Retrieves the map data requested by the client
        /// Used when logging in and changing maps
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void GetMapData(Client client, byte[] packet, int bytesRead)
        {
            PW = new PacketWriter(6400);
            int clientID = PR.ReadInt32(); //Read the client ID
            int mapID = PR.ReadInt32(); //Read the mapID
            int mapWidth = 150;
            int mapHeight = 150;
            //Eventually we will read the map information from either premade maps or the database, but for now i'll just do something for testing
            PW.Write((int)SendOPCodes.GETMAPDATA);
            PW.Write(1); //Map ID
            PW.Write(mapWidth); //Map width
            PW.Write(mapHeight); //Map Height
            StringBuilder SB = new StringBuilder();
            for (int x = 0; x < (mapWidth * mapHeight);x++)
            {
                SB.Append("G");
            }
            string mapString = SB.ToString();
            byte[] data = GetBytes(mapString);
            PW.Write(data, 0, data.Length);
            client.sendPacket(PW.ToArray());
        }

        //This needs work. How movement is handled will be changed
        private void Movement(Client client, byte[] packet, int bytesRead)
        {
            int clientID = PR.ReadInt32(); //Read the client ID
            int moveX = PR.ReadInt32(); //Read Movement X
            int moveY = PR.ReadInt32(); //Read Movement Y
            int speed = PR.ReadInt32(); //Read speed
            int facingDirection = PR.ReadInt32(); //read the facing direction

            //Update the position
            client.agent.posX += moveX * speed;
            client.agent.posY += moveY * speed;
            client.agent.facingDirection = (Agent.FacingDirection)facingDirection; //Set the facing direction
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.MOVEMENT); //send ID packet
            PW.Write(clientID); //send client ID
            PW.Write(client.agent.posX); //New agent PositionX
            PW.Write(client.agent.posY); //new agent PositionY
            Program.ClientStorage.sendPacketToAllClients(PW.ToArray());

        }

        /// <summary>
        /// Called when a player logs in.
        /// Loads their inventory. DUH
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void LoadInventory(Client client, byte[] packet, int bytesRead)
        {
            int clientID = PR.ReadInt32(); //Read the client ID
            client.agent.inventory.Load(); // Load the inventory in the server 
            //Send the inventory information back to the player
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.LOADINVENTORY); //send ID packet
            PW.Write(client.agent.inventory.serialize()); //serialize the inventory
            client.sendPacket(PW.ToArray()); //send it off
        }

        /// <summary>
        /// Handles the plant data
        /// sub codes
        /// 0 = load all data
        /// 1 = new plant
        /// 2 = remove plant
        /// 3 = plant update
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void PlantData(Client client, byte[] packet, int bytesRead)
        {
            PlantSubOPCode subcode = (PlantSubOPCode)PR.ReadInt32(); //Get the subcode
            PW = new PacketWriter();
            if (subcode == PlantSubOPCode.LoadAll) //Send all the plant data to the player
            {
                PW.Write((int)SendOPCodes.PLANTDATA);
                PW.Write((int)PlantSubOPCode.LoadAll);
                PW.Write(Program.PlantStorage.serialize());
                client.sendPacket(PW.ToArray());
            }
        }

        /// <summary>
        /// Called when a player disconnects from the server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void PlayerDisconnect(Client client, byte[] packet, int bytesRead)
        {
            int clientID = PR.ReadInt32();
            Client remove = Program.ClientStorage.getClientFromID(clientID);
            remove.Save();
            Program.ClientStorage.removeClient(remove);
            Console.WriteLine("Client #" + clientID.ToString() + " has disconnected.");

            //Inform the other clients
            PW = new PacketWriter();
            PW.Write((int)SendOPCodes.PLAYERDISCONNECTED);
            PW.Write(clientID);
            Program.ClientStorage.sendPacketToAllClients(PW.ToArray());
        }

        /// <summary>
        /// Called when a player moves an item in their inventory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void InventoryMove(Client client, byte[] packet, int bytesRead)
        {
            ulong inventorySerial = PR.ReadULong();
            int newX = PR.ReadInt32();
            int newY = PR.ReadInt32();
            client.agent.inventory.UpdateItemPosition(inventorySerial, newX, newY);
        }

        /// <summary>
        /// Called when a player uses an item
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        /// <param name="bytesRead"></param>
        private void UseItem(Client client, byte[] packet, int bytesRead)
        {
            ulong itemSerial = PR.ReadULong(); //get the serial of the item to use
            int targetX = PR.ReadInt32(); //get the target X
            int targetY = PR.ReadInt32(); //get the target Y

            BackgroundWorker bw = new BackgroundWorker();

            bw.DoWork += new DoWorkEventHandler(
            delegate(object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                Item useitem = client.agent.inventory.GetItem(itemSerial);
                if (useitem != null)
                    useitem.Use(targetX, targetY);
            });

            bw.RunWorkerAsync();
        }

    }

}
