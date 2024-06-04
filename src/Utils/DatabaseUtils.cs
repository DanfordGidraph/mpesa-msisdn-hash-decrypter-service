using System.Data.SQLite;
using Microsoft.Data.Sqlite;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public static class DatabaseUtils
    {
        static readonly string db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/database.sqlite");

        public static SqliteConnection CreateConnection(string? path = null)
        {

            SqliteConnection sqlite_conn = new SqliteConnection($"Data Source={path ?? db_path}");
            // Open the connection:
            try { sqlite_conn.Open(); }
            catch (Exception ex) { Console.WriteLine($"Connection Failed:: {ex.Message}"); }
            return sqlite_conn;
        }

        public static void CreateDatabase(string? path = null)
        {
            try
            {
                SQLiteConnection.CreateFile(path ?? db_path);
                Console.WriteLine("Database Created Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Creation Failed:: {ex.Message}");
            }
        }

        public static void CreateTable(SqliteConnection conn, string tableName, string columns, string extraCommands = ";")
        {
            try
            {
                SqliteCommand sqlite_cmd;
                sqlite_cmd = conn.CreateCommand();
                string Createsql = $"CREATE TABLE IF NOT EXISTS {tableName} ({columns}) {extraCommands}";
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = Createsql;
                sqlite_cmd.ExecuteNonQuery();
                Console.WriteLine($"Table {tableName} Created Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Table Creation Failed:: {ex.Message}");
            }
        }

        public static void InsertData(SqliteConnection conn, string tableName, string columns, string values)
        {
            try
            {
                SqliteCommand sqlite_cmd;
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = $"INSERT INTO {tableName} ({columns}) VALUES {values}; ";
                sqlite_cmd.ExecuteNonQuery();
                Console.WriteLine($"Data Inserted Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data Insertion Failed:: {ex.Message}");
            }
        }

        public static List<string> ReadData(SqliteConnection conn, string tableName, string columns, string conditions)
        {
            try
            {
                SqliteDataReader sqlite_datareader;
                SqliteCommand sqlite_cmd;
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = conditions.Length > 0 ? $"SELECT {columns} FROM {tableName} WHERE {conditions}" : $"SELECT {columns} FROM {tableName}";

                sqlite_datareader = sqlite_cmd.ExecuteReader();
                var results = new List<string>();
                while (sqlite_datareader.HasRows && sqlite_datareader.Read())
                {
                    // Read whole row into a string separated by commas
                    var row = "";

                    for (int i = 0; i < sqlite_datareader.FieldCount; i++)
                    {
                        row += sqlite_datareader.GetString(i) + ",";
                    }
                    results.Add(row.TrimEnd(','));
                }
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data Read Failed:: {ex.Message}");
                return [];
            }
        }

        public static void DeleteData(SqliteConnection conn, string tableName, string conditions)
        {
            try
            {
                SqliteCommand sqlite_cmd;
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = $"DELETE FROM {tableName} WHERE {conditions}";
                sqlite_cmd.ExecuteNonQuery();
                Console.WriteLine($"Data Deleted Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data Deletion Failed:: {ex.Message}");
            }
        }
    }
}