using Microsoft.Data.Sqlite;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public static class DatabaseGenerator
    {
        static readonly string root_db_path = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/", "database.sqlite");

        static readonly List<string> safaricomSeeds =
        [
            "254700000000", "254701000000", "254702000000", "254703000000", "254704000000", "254705000000", "254706000000","254707000000","254708000000","254709000000",
            "254710000000", "254711000000", "254712000000", "254713000000", "254714000000", "254715000000", "254716000000","254717000000","254718000000","254719000000",
            "254720000000", "254721000000", "254722000000", "254723000000", "254724000000", "254725000000", "254726000000","254727000000","254728000000","254729000000",
            "254740000000", "254741000000", "254742000000", "254743000000",  "254745000000", "254746000000","254748000000","254757000000","254758000000","254759000000",
            "254768000000","254769000000",
            "254790000000", "254791000000", "254792000000", "254793000000", "254794000000", "254795000000", "254796000000","254797000000","254798000000","254799000000",
            "254110000000", "254111000000", "254112000000", "254113000000", "254114000000", "254115000000", "254116000000","254117000000","254118000000","254119000000",
        ];

        public static void PopulateDtabase(DatabaseContext context)
        {


            var overalStart = DateTime.Now;
            Console.WriteLine($"Starting at {overalStart}");

            PhoneNumber[] dbValues = new PhoneNumber[safaricomSeeds.Count * 1000000];
            var parallelProcessStart = DateTime.Now;
            Parallel.ForEach(safaricomSeeds, (seed, _, outerIndx) =>
            {
                var start = DateTime.Now;
                var seedData = new int[1000000];

                Console.WriteLine($"\nCrypto hashing::{seed} Start {seedData.Length} at {start}");

                Parallel.ForEach(seedData, (_, _, index) =>
                {
                    string msisdn = (long.Parse(seed) + index).ToString();
                    string hash = CryptoUtils.HashToShorter(msisdn);
                    dbValues[(outerIndx * 1000000) + index] = new PhoneNumber(hash, msisdn);
                });
                Console.WriteLine($"Added {seedData.Length} records from {seed} to DB Values List");

                var end = DateTime.Now;
                Console.WriteLine($"hashing::{seed} took {(end - start).TotalSeconds} s");

            });
            var parallelProcessEnd = DateTime.Now;
            Console.WriteLine($"Parallel Process took {(parallelProcessEnd - parallelProcessStart).TotalSeconds} s");

            PhoneNumber[] phoneNumbersChunk;
            for (int i = 0; i < dbValues.Length; i += 5000000)
            {
                var length = dbValues.Length - i < 5000000 ? dbValues.Length - i : 5000000;
                phoneNumbersChunk = new PhoneNumber[length];
                Array.Copy(dbValues, i, phoneNumbersChunk, 0, length);
                // process chunk
                var chunkStart = DateTime.Now;
                Console.WriteLine($"\n Writing {phoneNumbersChunk.Length} records to the Database");
                context.PhoneNumbers.BulkInsertOptimized(phoneNumbersChunk);
                var written = i > 0 ? i : phoneNumbersChunk.Length;
                Console.WriteLine($"\tFinished Writing {written} records to the Database");
                var chunkEnd = DateTime.Now;
                Console.WriteLine($"\tChunk took {(chunkEnd - chunkStart).TotalSeconds} s");
            }

            var overalEnd = DateTime.Now;
            Console.WriteLine($"Overall took {(overalEnd - overalStart).TotalSeconds} s");
        }

        public static void SplitDatabase(DatabaseContext context)
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

        public static void RehydrateDatabase(DatabaseContext context)
        {
            try
            {
                // Check if the database exists
                SqliteConnection rootConnection;

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
                throw;
            }
        }

        public static void CleanupDatabse(DatabaseContext context)
        {
            try
            {
                // Check if the database exists
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
                throw;
            }
        }
    }
}