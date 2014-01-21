using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarmServer.Items;

namespace FarmServer
{

    class Agent
    {
        public int posX                        { get; set; }
        public int posY                        { get; set; }
        public int agentID                     { get; set; }
        public int experience                  { get; set; }
        public int level                       { get; set; }
        public int money                       { get; set; }
        public int mapID                       { get; set; }
        public Inventory inventory             { get; set; }
        public FacingDirection facingDirection { get; set; }

        public enum FacingDirection
        {
            North = 1,
            South = 2,
            East = 3,
            West = 4
        }

        public Agent(int AgentID)
        {
            this.agentID = AgentID;
            inventory = new Inventory(this);
        }

        public byte[] serialize()
        {
            PacketWriter PW = new PacketWriter();
            byte[] data;
            PW.Write(agentID);
            PW.Write(posX);
            PW.Write(posY);
            data = PW.ToArray();
            return data;
        }

        /// <summary>
        /// Save the agent to the database
        /// </summary>
        public void Save()
        {
            inventory.Save();
        }

    }
}
