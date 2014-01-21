using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;


namespace FarmServer.Plants
{
    class Plant
    {
        public ulong serial         { get; set; } //The serial number of the plant
        public int type             { get; set; } //the type of plant this is the same as the list
        public string name          { get; set; } //The name of the plant
        public int stage            { get; set; } //the current growth stage of the plant
        public int posX             { get; set; } //The X location in pixels
        public int posY             { get; set; } //The y location in pixels
        public int water            { get; set; } //The amount of water on the plant
        public int growthTime       { get; set; } //The time until the plant grows
        public int deathTime        { get; set; } //The time until death
        public int[] deathTimes     { get; set; } //The time for death for each stage 0-3
        public int[] growthTimes    { get; set; } //The time for growth for each stage 0-2
        public int[] waterAmounts   { get; set; } //The amount each stage needs to be at 100% watered
        public int harvestEXP       { get; set; } //The amount of exp each player gets when they harvest the plant

        public bool Loaded = false; //This is to make sure the plant is fully loaded before it gets serialized or anything

        //MySQLStuff
        DatabaseConnection dbc = new DatabaseConnection();

        public Plant(int type, int stage, int posX, int posY, int water, int growthTime, int deathTime)
        {
            this.serial = 0;
            this.type = type;
            this.name = name;
            this.stage = stage;
            this.posX = posX;
            this.posY = posY;
            this.water = water;
            this.growthTime = growthTime;
            this.deathTime = deathTime;

            deathTimes = new int[4];
            growthTimes = new int[3];
            waterAmounts = new int[4];

            Load();
        }

        public Plant(ulong serial, int type, string name, int stage, int posX, int posY, int water, int growthTime, int deathTime, int[] deathTimes, int[] growthTimes, int[] waterAmounts, int harvestEXP)
        {
            this.serial = serial;
            this.type = type;
            this.name = name;
            this.stage = stage;
            this.posX = posX;
            this.posY = posY;
            this.water = water;
            this.growthTime = growthTime;
            this.deathTime = deathTime;
            this.deathTimes = deathTimes;
            this.growthTimes = growthTimes;
            this.waterAmounts = waterAmounts;
            this.harvestEXP = harvestEXP;

            deathTimes = new int[4];
            growthTimes = new int[3];
            waterAmounts = new int[4];

            Loaded = true;
        }

        public Plant(ulong serial)
        {
            this.serial = serial;
            deathTimes = new int[4];
            growthTimes = new int[3];
            waterAmounts = new int[4];
        }

        public void Load()
        {
            Loaded = false;
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "select * from plants right join planttemplates on plants.`type` = planttemplates.`type` WHERE id=@ID");
            cmd.Parameters.AddWithValue("@ID", serial);
            MySqlDataReader rdr = dbc.ExecuteReader(conn, cmd);
            while (rdr.Read())
            {
                //Load up the plant
                type = rdr.GetInt32(1);
                stage = rdr.GetInt32("stage");
                posX = rdr.GetInt32("posX");
                posY = rdr.GetInt32("posY");
                water = rdr.GetInt32("water");
                growthTime = rdr.GetInt32("growthtime");
                deathTime = rdr.GetInt32("deathTime");
                name = rdr.GetString("name");
                growthTimes[0] = rdr.GetInt32("stage0growthtime");
                growthTimes[1] = rdr.GetInt32("stage1growthtime");
                growthTimes[2] = rdr.GetInt32("stage2growthtime");
                waterAmounts[0] = rdr.GetInt32("stage0wateramount");
                waterAmounts[1] = rdr.GetInt32("stage1wateramount");
                waterAmounts[2] = rdr.GetInt32("stage2wateramount");
                waterAmounts[3] = rdr.GetInt32("stage3wateramount");
                deathTimes[0] = rdr.GetInt32("stage0deathtime");
                deathTimes[1] = rdr.GetInt32("stage1deathtime");
                deathTimes[2] = rdr.GetInt32("stage2deathtime");
                deathTimes[3] = rdr.GetInt32("stage3deathtime");
                harvestEXP = rdr.GetInt32("harvestexp");
            }
            conn.Close();
            Loaded = true;
        }

        public void Save()
        {

        }

        //Serialize this bitch
        public byte[] serialize()
        {
            byte[] data = null;
            PacketWriter PW = new PacketWriter();
            PW.Write(serial); //Write the serial
            PW.Write(type);
            PW.Write(posX);
            PW.Write(posY);
            PW.Write(stage);
            PW.Write(water);
            PW.Write(growthTime);
            PW.Write(name.Length);
            PW.Write(name);
            for (int i = 0; i < growthTimes.Length; i++)
            {
                PW.Write(growthTimes[i]);
            }
            for (int i = 0; i < waterAmounts.Length; i++)
            {
                PW.Write(waterAmounts[i]);
            }
            for (int i = 0; i < deathTimes.Length; i++)
            {
                PW.Write(deathTimes[i]);
            }
            data = PW.ToArray();
            return data;
        }

        /// <summary>
        /// Saves all the plant data to the database
        /// </summary>
        public void Update()
        {
            Loaded = false;
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "UPDATE plants SET type=@type, stage=@stage, posX=@posX,posY=@posY,water=@water, growthtime=@growthtime, deathtime=@deathtime WHERE ID=@serial;");
            cmd.Parameters.AddWithValue("@type",type);
            cmd.Parameters.AddWithValue("@stage",stage);
            cmd.Parameters.AddWithValue("@posX",posX);
            cmd.Parameters.AddWithValue("@posY",posY);
            cmd.Parameters.AddWithValue("@water",water);
            cmd.Parameters.AddWithValue("@growthtime",growthTime);
            cmd.Parameters.AddWithValue("@deathtime",deathTime);
            cmd.Parameters.AddWithValue("@serial",serial);
            dbc.ExecuteCommand(conn, cmd);
            conn.Close();
            Loaded = true;
        }

        /// <summary>
        /// Just a testing thing
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="cmd"></param>
        public void Update(MySqlConnection conn, MySqlCommand cmd)
        {
            Loaded = false;
            dbc.ExecuteCommand(conn, cmd);
            Loaded = true;
        }

        /// <summary>
        /// Insert a this plant into the database
        /// </summary>
        public void Insert()
        {
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "INSERT INTO plants (type, stage, posX, posY, water, growthtime, deathtime) VALUES (@type, @stage, @posX, @posY, @water, @growthtime, @deathtime);");
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@stage", stage);
            cmd.Parameters.AddWithValue("@posX", posX);
            cmd.Parameters.AddWithValue("@posY", posY);
            cmd.Parameters.AddWithValue("@water", water);
            cmd.Parameters.AddWithValue("@growthtime", growthTime);
            cmd.Parameters.AddWithValue("@deathtime", deathTime);
            dbc.ExecuteCommand(conn, cmd);
            conn.Close();

            //Set the new serial
            conn = DatabaseConnection.GetConnection();
            cmd = DatabaseConnection.CreateCommand(conn, "SELECT MAX(ID) from PLANTS;");
            MySqlDataReader rdr = dbc.ExecuteReader(conn, cmd);
            while (rdr.Read())
            {
                serial = (ulong)rdr.GetInt64(0);
            }
            conn.Close();
        }

        /// <summary>
        /// Removes this plant
        /// </summary>
        public void Delete()
        {
            Program.PlantStorage.RemovePlant(this);
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "DELETE FROM plants WHERE ID=@serial");
            cmd.Parameters.AddWithValue("@serial", serial);
            dbc.ExecuteCommand(conn, cmd);
            conn.Close();
        }

        public void CheckForStageUp()
        {
            PlantList plantType = (PlantList)type;
            if (stage < 3 && plantType != PlantList.Dirt) //Stage 3 plants cannot grow anymore also dirt cant grow
            {
                if (growthTime >= growthTimes[stage])
                {
                    stage++; //Stage up
                    growthTime = 0; //reset growthtime
                }
            }
        }

        /// <summary>
        /// Called during updating. Removes water, grows the plant, and kills the plant
        /// This is a good place to add some fun modifiers
        /// </summary>
        public void GrowWaterDeath()
        {
            PlantList plantType = (PlantList)type;
            if (plantType != PlantList.Dirt) //Dirt cant grow
            {
                growthTime++;
            }

            if (water > 0)
                water--;

            deathTime++;
        }

        public override string ToString()
        {
            return name + " Serial:"+serial.ToString()+" Type:" + type.ToString() + " Stage:" + stage.ToString()+" Location:("+posX.ToString()+","+posY.ToString()+")";
        }
    }
}
