using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarmServer.Items;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace FarmServer.Items
{
    /// <summary>
    /// The inventory stores all of the player's objects
    /// </summary>
    class Inventory
    {
        private Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
        private Agent agent;

        DatabaseConnection dbc = new DatabaseConnection();

        public Inventory(Agent agent)
        {
            this.agent = agent;
        }

        /// <summary>
        /// Saves the inventory
        /// </summary>
        public void Save()
        {
            foreach (KeyValuePair<ulong,Item> item in items)
            {
                item.Value.Save();
            }
        }

        /// <summary>
        /// Loads the inventory
        /// </summary>
        public void Load()
        {
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn);
            cmd.CommandText = "Select ID from items LEFT JOIN itemtemplates ON items.type = itemtemplates.type WHERE playerID = @playerID";
            cmd.Parameters.AddWithValue("@playerID", agent.agentID);
            MySqlDataReader rdr = dbc.ExecuteReader(conn, cmd);
            while (rdr.Read())
            {
                Item item = new Item((ulong)rdr.GetInt32(0));
                items.Add(item.serial, item);
            }

            foreach (KeyValuePair<ulong, Item> item in items)
            {
                item.Value.Load();
            }
            conn.Close();
        }

        /// <summary>
        /// Check to see if an item of a certain type is in the inventory
        /// </summary>
        /// <param name="item">The item type of the item you are searching for</param>
        /// <returns></returns>
        public bool Contains(int type)
        {
            bool result = false;
            foreach (KeyValuePair<ulong, Item> kvp in items)
            {
                if (kvp.Value.type == type)
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Check to see if an inventory contains an item type and certain amount
        /// </summary>
        /// <param name="type">The type of the item you are searching for</param>
        /// <param name="amount">The amount you desire</param>
        /// <returns></returns>
        public bool Contains(int type, int amount)
        {
            bool result = false;
            foreach (KeyValuePair<ulong, Item> kvp in items)
            {
                if (kvp.Value.type == type)
                {
                    if (kvp.Value.amount >= amount)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Check if the player has a certain item based on its serial
        /// </summary>
        /// <param name="serial"></param>
        /// <returns></returns>
        public bool Contains(ulong serial)
        {
            if(items.ContainsKey(serial))
                return true;
            else 
                return false;
        }

        public Item GetItem(ulong serial)
        {
            if(Contains(serial))
                return items[serial];
            else
                return null;
        }

        public void RemoveItem(ulong itemID, int amount)
        {
            if (items[itemID].amount >= amount && (items[itemID].amount - amount) >= 0)
            {
                items[itemID].amount -= amount;
                if (items[itemID].amount == 0)
                {
                    items.Remove(itemID);
                }
            }
        }

        public void AddItem(Item item)
        {
            if (items.Count < 25)
            {
                foreach (KeyValuePair<ulong, Item> iItem in items)
                {
                    //TODO FINISH THIS
                }
            }
        }

        /// <summary>
        /// Returns the number of items in the inventory
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return items.Count;
        }

        public byte[] serialize()
        {
            byte[] data;
            PacketWriter PW = new PacketWriter();
            PW.Write(Count()); //Write the count
            foreach (KeyValuePair<ulong, Item> item in items)
            {
                PW.Write(item.Value.serialize()); //Serialize all the items
            }
            data = PW.ToArray();
            return data;
        }

        public void UpdateItemPosition(ulong serial, int newX, int newY)
        {
            items[serial].posX = newX;
            items[serial].posY = newY;
            items[serial].Save();
        }

    }
}
