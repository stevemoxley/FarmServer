using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarmServer;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using FarmServer.Plants;

namespace FarmServer.Items
{

    public enum ItemLocation
    {
        Ground = 1,
        Inventory = 2,
        Container = 3
    }

    class Item : ISaveable
    {
        public int posX;
        public int posY;
        public int type;
        public ulong serial;
        public ulong containerID;
        public int amount;
        public int maxStackSize = -1;
        public ItemLocation location;
        public bool stackable;
        public int useEffect;
        public int useAmount;
        public string name;

        DatabaseConnection dbc = new DatabaseConnection();

        public Item(ulong serial)
        {
            this.serial = serial;
        }

        public byte[] serialize()
        {
            PacketWriter PW = new PacketWriter();
            byte[] data;
            PW.Write(serial); //Write the serial number
            PW.Write(type); //Type
            PW.Write(posX); //posX
            PW.Write(posY); //posY
            PW.Write(amount); //amount
            PW.Write((int)location); //location
            PW.Write(containerID); //Container data
            PW.Write(name.Length); //write name length
            PW.Write(name); //name
            if (stackable)
                PW.Write(1); //stackable
            else
                PW.Write(0);
    
            PW.Write(maxStackSize); //max stack size
            PW.Write(useEffect); //use effect
            PW.Write(useAmount); //use amount
            data = PW.ToArray();
            return data;
        }

        public void Save()
        {
            //Write to the database
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "UPDATE items SET posX = @posX, posY = @posY, amount = @amount WHERE ID = @ID");
            cmd.Parameters.AddWithValue("@posX",posX);
            cmd.Parameters.AddWithValue("@posY",posY);
            cmd.Parameters.AddWithValue("@amount",amount);
            cmd.Parameters.AddWithValue("@ID", serial);
            dbc.ExecuteCommand(conn, cmd);
            conn.Close();
        }

        public void Load()
        {
            //Load from the database from serial
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "Select * from items LEFT JOIN itemtemplates ON items.type = itemtemplates.type WHERE ID = @ID;");
            cmd.Parameters.AddWithValue("@ID", serial);
            MySqlDataReader rdr = dbc.ExecuteReader(conn, cmd);
            while (rdr.Read())
            {
                this.posX = rdr.GetInt32(3);
                this.posY = rdr.GetInt32(4);
                this.amount = rdr.GetInt32(5);
                this.location = (ItemLocation)rdr.GetInt32(6);
                this.containerID = (ulong)rdr.GetInt32(7);
                this.type = rdr.GetInt32(8);
                this.name = rdr.GetString(9);
                if (rdr.GetInt32(10) == 1)
                    stackable = true;
                else
                    stackable = false;
                this.maxStackSize = rdr.GetInt32(11);
                this.useEffect = rdr.GetInt32(12);
                this.useAmount = rdr.GetInt32(13);
            }
            conn.Close();
        }

        /// <summary>
        /// Called when a player uses an item.
        /// Will probably only be called by the packet handler but maybe not
        /// </summary>
        public void Use(int targetX, int targetY)
        {
           //switch based on the use effect
            ItemUseList effect = (ItemUseList)useEffect;
            Plant targetPlant = Program.PlantStorage.GetPlantFromCoords(targetX, targetY);
            switch (effect)
            {
                case ItemUseList.Hoe:
                {
                    if (targetPlant == null)
                    {
                        Plant dirt = new Plant(-1, 0, targetX, targetY, 0, 0, 0);
                        Program.PlantStorage.AddPlant(dirt);
                        dirt.Load();
                        PacketCreator.PlantData(dirt, PlantSubOPCode.Add);
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
    }
}
