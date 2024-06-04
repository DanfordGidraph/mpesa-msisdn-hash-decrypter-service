using System;
using Microsoft.Data.Sqlite;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public static class DatabaseGenerator
    {
        static readonly List<string> seeds =
        [
            "254700000000", "254701000000", "254702000000", "254703000000", "254704000000", "254705000000", "254706000000","254707000000","254708000000","254709000000",
            "254710000000", "254711000000", "254712000000", "254713000000", "254714000000", "254715000000", "254716000000","254717000000","254718000000","254719000000",
            "254720000000", "254721000000", "254722000000", "254723000000", "254724000000", "254725000000", "254726000000","254727000000","254728000000","254729000000",
            "254740000000", "254741000000", "254742000000", "254743000000",  "254745000000", "254746000000","254748000000","254757000000","254758000000","254759000000",
            "254768000000","254769000000",
            "254790000000", "254791000000", "254792000000", "254793000000", "254794000000", "254795000000", "254796000000","254797000000","254798000000","254799000000",
            "254110000000", "254111000000", "254112000000", "254113000000", "254114000000", "254115000000", "254116000000","254117000000","254118000000","254119000000",
        ];

        public static void GenerateDatabase()
        {
            // Create the Table if it does not exist
            SqliteConnection connection = DatabaseUtils.CreateConnection();
            DatabaseUtils.CreateTable(connection, "PhoneNumbers", "hash PRIMARY KEY, msisdn INTEGER", "WITHOUT ROWID");
            connection.Close();

            PopulateDtabase();
        }

        public static void PopulateDtabase()
        {


            var overalStart = DateTime.Now;
            Console.WriteLine($"Starting at {overalStart}");

            SqliteConnection connection = DatabaseUtils.CreateConnection();

            // var tasks = new List<Task>();
            foreach (var seed in seeds)
            {
                // tasks.Add(Task.Run(() =>
                // {
                var start = DateTime.Now;
                var seedData = new List<string>();
                for (int i = 0; i <= 999999; i++) seedData.Add(i.ToString());

                Console.WriteLine($"\nCrypto hashing::{seed} Start {seedData.Count}");

                string[] dbValues = new string[seedData.Count];
                foreach (var num in seedData)
                {
                    // Console.WriteLine($"hashing::{seed}::{num}");
                    string msisdn = (long.Parse(seed) + int.Parse(num)).ToString();
                    string hash = CryptoUtils.HashToShorter(msisdn);
                    // Write to database
                    dbValues[int.Parse(num)] = $"('{hash}', '{msisdn}')";
                }
                Console.WriteLine($"Writing {dbValues.Length} records from {seed} to Database");
                DatabaseUtils.InsertData(connection, "PhoneNumbers", "hash, msisdn", string.Join(",", dbValues));
                var end = DateTime.Now;
                Console.WriteLine($"hashing::{seed} took {(end - start).TotalSeconds} s");

                // return "done";
                // }));
                // Console.WriteLine($"Task added {tasks.Count}");
            }
            connection.Close();
            var overalEnd = DateTime.Now;
            Console.WriteLine($"Overall took {(overalEnd - overalStart).TotalSeconds} s");

            // try
            // {
            //     Task.WaitAll([.. tasks]);
            //     Console.WriteLine($"All tasks completed {tasks.Count}");
            //     connection.Close();
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine(ex);
            // }
        }

        public static void SplitDatabase()
        {

            SqliteConnection sourceConnection = DatabaseUtils.CreateConnection();
            //  Users
            var startUsers = DateTime.Now;

            string dest_users_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/splits/users.sqlite");

            DatabaseUtils.CreateDatabase(dest_users_db_path);

            SqliteConnection destUsersConnection = DatabaseUtils.CreateConnection(dest_users_db_path);

            string[] Users = DatabaseUtils
                .ReadData(sourceConnection, "Users", "email, name, role, token", "")
                .Select(user =>
                {
                    var splitUser = user.Split(",");
                    return $"('{splitUser[0]}', '{splitUser[1]}', '{splitUser[2]}', '{splitUser[3]}')";
                }).ToArray();

            // Console.WriteLine($"Splitting {Users.Count} Users:: {string.Join(",", Users)}");

            DatabaseUtils.CreateTable(destUsersConnection, "Users", "email TEXT PRIMARY KEY, name TEXT , role TEXT, token TEXT", "WITHOUT ROWID");

            DatabaseUtils.InsertData(destUsersConnection, "Users", " email, name, role, token", string.Join(",", Users));

            destUsersConnection.Close();

            var endUsers = DateTime.Now;
            Console.WriteLine($"Users took {(endUsers - startUsers).TotalSeconds} s");

            var startNumbers = DateTime.Now;
            List<string> rootSeeds = ["25470", "25471", "25472", "25474", "25475", "25476", "25479", "25411"];
            // Phone Numbers
            rootSeeds.ForEach(seed =>
            {
                var startNumber = DateTime.Now;
                var dbName = $"PhoneNumbers_{seed}";
                string dest_numbers_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/splits/", $"{dbName}.sqlite");

                DatabaseUtils.CreateDatabase(dest_numbers_db_path);

                string[] NumberRows = DatabaseUtils
                    .ReadData(sourceConnection, "PhoneNumbers", "hash, msisdn", $"msisdn LIKE '{seed}%'")
                    .Select(numberRow => $"('{numberRow.Split(",")[0]}', '{numberRow.Split(",")[1]}')").ToArray();

                Console.WriteLine($"Splitting {NumberRows.Length} PhoneNumbers::");

                SqliteConnection destConnection = DatabaseUtils.CreateConnection(dest_numbers_db_path);
                DatabaseUtils.CreateTable(destConnection, "PhoneNumbers", "hash TEXT PRIMARY KEY, msisdn INTEGER", "WITHOUT ROWID");

                DatabaseUtils.InsertData(destConnection, "PhoneNumbers", "hash, msisdn", string.Join(",", NumberRows));

                destConnection.Close();
                var endNumber = DateTime.Now;
                Console.WriteLine($"Numbers::{seed} took {(endNumber - startNumber).TotalSeconds} s");
            });
            sourceConnection.Close();

            var endNumbers = DateTime.Now;
            Console.WriteLine($"Numbers took {(endNumbers - startNumbers).TotalSeconds} s");

        }

        public static void RehydrateDatabase()
        {
            try
            {
                // Check if the database exists
                SqliteConnection rootConnection;

                string root_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/", "database.sqlite");
                if (File.Exists(root_db_path))
                {
                    Console.WriteLine("Database exists, deleting and recreating");
                    File.Delete(root_db_path);
                }
                DatabaseUtils.CreateDatabase(root_db_path);
                rootConnection = DatabaseUtils.CreateConnection(root_db_path);
                DatabaseUtils.CreateTable(rootConnection, "PhoneNumbers", "hash TEXT PRIMARY KEY , msisdn INTEGER", "WITHOUT ROWID");
                DatabaseUtils.CreateTable(rootConnection, "Users", "email TEXT PRIMARY KEY , name TEXT, role TEXT, token TEXT", "WITHOUT ROWID");

                // Rehydrate Users
                var startUsers = DateTime.Now;
                string source_users_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/splits/users.sqlite");
                if (!File.Exists(source_users_db_path))
                {
                    Console.WriteLine($"Database {source_users_db_path} does not exist");
                }
                else
                {
                    SqliteConnection sourceUsersConnection = DatabaseUtils.CreateConnection(source_users_db_path);

                    var Users = DatabaseUtils
                    .ReadData(sourceUsersConnection, "Users", "email, name,  role, token", "")
                    .Select(user =>
                    {
                        var splitUser = user.Split(",");
                        return $"('{splitUser[0]}', '{splitUser[1]}', '{splitUser[2]}', '{splitUser[3]}')";
                    }).ToArray();


                    DatabaseUtils.InsertData(rootConnection, "Users", "email, name, role, token", string.Join(",", Users));

                    sourceUsersConnection.Close();

                    var endUsers = DateTime.Now;
                    Console.WriteLine($"Users took {(endUsers - startUsers).TotalSeconds} s");
                }

                // Rehydrate Phone Numbers
                var startNumbers = DateTime.Now;
                List<string> rootSeeds = ["25470", "25471", "25472", "25474", "25475", "25476", "25479", "25411"];
                rootSeeds.ForEach(seed =>
                {
                    var startNumber = DateTime.Now;
                    var dbName = $"PhoneNumbers_{seed}";
                    string source_numbers_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/splits/", $"{dbName}.sqlite");
                    if (!File.Exists(source_numbers_db_path))
                    {
                        Console.WriteLine($"Database {source_numbers_db_path} does not exist");
                        return;
                    }

                    SqliteConnection sourceConnection = DatabaseUtils.CreateConnection(source_numbers_db_path);

                    var seedNumbers = DatabaseUtils
                        .ReadData(sourceConnection, "PhoneNumbers", "hash, msisdn", "")
                        .Select(numberRow => $"('{numberRow.Split(",")[0]}', '{numberRow.Split(",")[1]}')").ToArray();

                    DatabaseUtils.InsertData(rootConnection, "PhoneNumbers", "hash, msisdn", string.Join(",", seedNumbers));

                    sourceConnection.Close();
                    var endNumber = DateTime.Now;
                    Console.WriteLine($"Numbers::{seed} took {(endNumber - startNumber).TotalSeconds} s");
                });
                rootConnection.Close();
                var endNumbers = DateTime.Now;
                Console.WriteLine($"Numbers took {(endNumbers - startNumbers).TotalSeconds} s");
                Console.WriteLine($"Process took {(endNumbers - startUsers).TotalSeconds} s");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:: => " + ex.Message);
                throw ex;
            }
        }

        public static void CleanupDatabse()
        {
            try
            {
                // Check if the database exists
                string root_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/", "database.sqlite");
                SqliteConnection rootConnection = DatabaseUtils.CreateConnection(root_db_path);
                List<string> matched = ["25474", "25475"];
                matched.ForEach(match =>
                {
                    DatabaseUtils.DeleteData(rootConnection, "PhoneNumbers", $"msisdn LIKE '{match}%'");
                });
                rootConnection.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:: => " + ex.Message);
                throw ex;
            }
        }
    }
}