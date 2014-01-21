using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace FarmServer.Plants
{
    class PlantStorage
    {
        private ConcurrentDictionary<ulong, Plant> plants = new ConcurrentDictionary<ulong, Plant>();
        private ConcurrentDictionary<ulong, Plant> saveList = new ConcurrentDictionary<ulong, Plant>();
        private ConcurrentDictionary<ulong, Plant> serialList = new ConcurrentDictionary<ulong, Plant>();
        private ConcurrentDictionary<ulong, Plant> addWaitList = new ConcurrentDictionary<ulong, Plant>();
        private ConcurrentDictionary<ulong, Plant> removeWaitList = new ConcurrentDictionary<ulong, Plant>();
        Thread updaterThread;
        Thread savingThread;
        private bool Loading = true;
        private bool Reloading = false;
        private bool Saving = false;
  
        DatabaseConnection dbc = new DatabaseConnection();

        public PlantStorage()
        {
            updaterThread = new Thread(new ThreadStart(Run));
            updaterThread.Start();

            savingThread = new Thread(new ThreadStart(RunSave));
            savingThread.Start();
        }

        //Methods for Loading plants
        #region Loading
        /// <summary>
        /// Load all the plants into the storage
        /// This will probably only be called upon loading up the server
        /// </summary>
        public void Load()
        {
            Loading = true;
            Console.WriteLine("Loading plants...");
            LoadPlants();
            Loading = false;
        }

        private void ReLoad()
        {
            if (!Loading)
            {
                plants.Clear();
                LoadPlants();
            }
        }

        private void LoadPlants()
        {
                Reloading = true;
                MySqlConnection conn = DatabaseConnection.GetConnection();
                MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "select * from plants right join planttemplates on plants.type = planttemplates.type WHERE id != 'null';");
                MySqlDataReader rdr = dbc.ExecuteReader(conn, cmd);
                while (rdr.Read())
                {
                    int[] deathTimes = new int[4];
                    int[] growthTimes = new int[3];
                    int[] waterAmounts = new int[4];
                    ulong serial = (ulong)rdr.GetInt32(0);
                    int type = rdr.GetInt32("type");
                    int stage = rdr.GetInt32("stage");
                    int posX = rdr.GetInt32("posX");
                    int posY = rdr.GetInt32("posY");
                    int water = rdr.GetInt32("water");
                    int growthTime = rdr.GetInt32("growthtime");
                    int deathTime = rdr.GetInt32("deathTime");
                    string name = rdr.GetString("name");
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
                    int harvestEXP = rdr.GetInt32("harvestexp");
                    Plant plant = new Plant(serial, type, name, stage, posX, posY, water, growthTime, deathTime, deathTimes, growthTimes, waterAmounts, harvestEXP);
                    plants.TryAdd(plant.serial, plant);
                    Reloading = false;
                }
                conn.Close();
        }

        #endregion

        //Methods for saving the plants
        #region Saving
        /// <summary>
        /// Saves all the plants in the storage
        /// </summary>
        public void Save()
        {
            foreach (KeyValuePair<ulong, Plant> plant in plants)
            {
                plant.Value.Save();
            }
        }
        #endregion

        //Methods for adding a plant
        #region Adding a plant
        /// <summary>
        /// Add a plant
        /// </summary>
        /// <param name="plant"></param>
        public void AddPlant(Plant plant)
        {
            plant.Insert(); //Add to database and get the serial
            addWaitList.TryAdd(plant.serial, plant);
        }
        #endregion

        //Methods for removing a plant
        #region Removing a plant
        /// <summary>
        /// Remove a plant by plant object
        /// </summary>
        /// <param name="plant"></param>
        public void RemovePlant(Plant plant)
        {
            try
            {
                DeleteFromDB(plant.serial);
                plants.TryRemove(plant.serial, out plant);
            }
            catch
            {

            }
        }
        private void DeleteFromDB(ulong serial)
        {
            MySqlConnection conn = DatabaseConnection.GetConnection();
            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "DELETE FROM PLANTS WHERE ID=@ID");
            cmd.Parameters.AddWithValue("@ID", serial);
            dbc.ExecuteCommand(conn, cmd);
            conn.Close();
        }
        #endregion

        //Get a plant from its coordinates
        public Plant GetPlantFromCoords(int x, int y)
        {
            do
            {
            } while (Reloading);
            foreach (KeyValuePair<ulong, Plant> plant in plants)
            {
                if (plant.Value.posX == x && plant.Value.posY == y)
                {
                    return plant.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Serialize the plant storage for sending to clients
        /// </summary>
        /// <returns>The perfect array of bytes to be sent off to the client</returns>
        public byte[] serialize()
        {
            PacketWriter PW = new PacketWriter();
            PacketWriter PW2 = new PacketWriter();
            //Write the number of plants in storage
            int count = 0;
            foreach (KeyValuePair<ulong, Plant> plant in serialList)
            {
                if (plant.Value.Loaded)
                {
                    PW2.Write(plant.Value.serialize()); //serialize all the plants
                    count++;
                }
            }
            PW.Write(count);
            PW.Write(PW2.ToArray());
            return PW.ToArray();
        }

        /// <summary>
        /// The thread process
        /// </summary>
        private void Run()
        {
            while (true)
            {
                if (!Loading)
                {
                    Thread.Sleep(1000); //Make the thread sleep for one second

                    #region Add/Remove
                    //Add from wait list
                    foreach (var key in addWaitList.Keys)
                    {
                        try
                        {
                            plants.TryAdd(key, addWaitList[key]);
                        }
                        catch
                        {
                        }
                    }
                    addWaitList.Clear();

                    //Remove from wait list
                    foreach (var key in removeWaitList.Keys)
                    {
                        Plant removePlant = removeWaitList[key];
                        plants.TryRemove(removePlant.serial, out removePlant);
                    }
                    removeWaitList.Clear();
                    #endregion

                    if(!Saving)
                        saveList = plants;

                    serialList = plants;

                    //Update the plants...do other crap...
                    foreach (var key in plants.Keys)
                    {
                        Plant plant = plants[key];
                        plant.GrowWaterDeath(); //Grow the plant. Remove water. Make it die a little
                        plant.CheckForStageUp(); //Check if its time to grow
                        PacketCreator.PlantData(plant, PlantSubOPCode.Update);
                    }
                }

               // PacketCreator.UpdateAllPlantData();
            }
        }

        private void RunSave()
        {
            while (true)
            {
                if (!Loading)
                {
                    Thread.Sleep(1000 * 30); //Save every 30 seconds

                    Saving = true;
                    Stopwatch SW = new Stopwatch();
                    SW.Start();
                    MySqlConnection conn = DatabaseConnection.GetConnection();
                    lock (saveList)
                    {
                        foreach (var key in saveList.Keys)
                        {
                            MySqlCommand cmd = DatabaseConnection.CreateCommand(conn, "UPDATE plants SET type=@type, stage=@stage, posX=@posX,posY=@posY,water=@water, growthtime=@growthtime, deathtime=@deathtime WHERE ID=@serial;");
                            Plant plant = saveList[key];
                            cmd.Parameters.AddWithValue("@type", plant.type);
                            cmd.Parameters.AddWithValue("@stage", plant.stage);
                            cmd.Parameters.AddWithValue("@posX", plant.posX);
                            cmd.Parameters.AddWithValue("@posY", plant.posY);
                            cmd.Parameters.AddWithValue("@water", plant.water);
                            cmd.Parameters.AddWithValue("@growthtime", plant.growthTime);
                            cmd.Parameters.AddWithValue("@deathtime", plant.deathTime);
                            cmd.Parameters.AddWithValue("@serial", plant.serial);
                            plant.Update(conn, cmd);
                        }
                    }
                    conn.Close();
                    SW.Stop();
                    float saveTime = SW.ElapsedMilliseconds / 1000;
                    Console.WriteLine("Save time:" + saveTime.ToString() + " sec");
                    Saving = false;
                }
            }
        }

    }
}
