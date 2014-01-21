using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace FarmServer
{
    class DatabaseConnection
    {
        public const string SERVERIP = "";
        public const string SERVERPORT = "3306";
        public const string USERNAME = "root";
        public const string PASSWORD = "";
        public const string DBNAME = "";
        private static string CONNECTIONSTRING = "";

        public DatabaseConnection()
        {
            CONNECTIONSTRING = @"server=" + SERVERIP + ";userid=" + USERNAME + ";password=" + PASSWORD + ";database=" + DBNAME;
        }

        public static MySqlConnection GetConnection()
        {
            MySqlConnection newConn = null;
            try
            {
                newConn = new MySqlConnection(CONNECTIONSTRING);
                newConn.Open();

            }
            catch (MySqlException ex)
            {
                Console.WriteLine("MySQL Error: {0}", ex.ToString());
            }
            return newConn;
        }

        public static void Close(MySqlConnection conn)
        {
            if (conn != null)
            {
                conn.Close();
            }
        }
        /// <summary>
        /// Execute a query that returns nothing
        /// </summary>
        /// <param name="queryString"></param>
        public void QueryNoParams(MySqlConnection conn, string queryString)
        {
            if (conn != null)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    MySqlCommand cmd = new MySqlCommand(queryString, conn);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine("Connection not open. Try calling Connect()");
                }
            }
            else
            {
                Console.WriteLine("Must call Connect() before trying to query");
            }
        }
        /// <summary>
        /// Execute a string with parameters
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        public void QueryWithParams(MySqlConnection conn, string queryString, MySqlParameter[] parameters)
        {
            if (conn != null)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    MySqlCommand cmd = new MySqlCommand(queryString, conn);
                    foreach (MySqlParameter parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine("Connection not open. Try calling Connect()");
                }
            }
            else
            {
                Console.WriteLine("Must call Connect() before trying to query");
            }
        }
        /// <summary>
        /// Execute a command that returns nothing
        /// </summary>
        /// <param name="cmd"></param>
        public void ExecuteCommand(MySqlConnection conn, MySqlCommand cmd)
        {
            if (conn != null)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine("Connection not open. Try calling Connect()");
                }
            }
            else
            {
                Console.WriteLine("Must call Connect() before trying to query");
            }
        }

        /// <summary>
        /// Execute a command. Return value is the mysqldatareader
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public MySqlDataReader ExecuteReader(MySqlConnection conn, MySqlCommand cmd)
        {
            if (conn != null)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine("Connection not open. Try calling Connect()");
                }
            }
            else
            {
                Console.WriteLine("Must call Connect() before trying to query");
            }
            return cmd.ExecuteReader();
        }

        public static MySqlCommand CreateCommand(MySqlConnection conn)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            return cmd;
        }

        public static MySqlCommand CreateCommand(MySqlConnection conn, string commandString)
        {
            MySqlCommand cmd = new MySqlCommand(commandString);
            cmd.Connection = conn;
            return cmd;
        }
    }
}
