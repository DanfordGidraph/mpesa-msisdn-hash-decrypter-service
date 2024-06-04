using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public static class JSONUtils
    {

        public static void WriteJSON(JObject json)
        {
            using (StreamWriter file = File.CreateText(@"c:\videogames.json"))
            using (JsonTextWriter writer = new(file))
            {
                json.WriteTo(writer);
            }
        }

        public static void OptimizeJSON(string source_path, string destination_path)
        {
            // read JSON directly from a file
            Dictionary<string, string> dict = new();
            using StreamReader file = File.OpenText(source_path);
            using JsonTextReader reader = new(file);
            {
                JsonSerializer serializer = new();

                // List<ExportedJsonObjectType> jsonObject = serializer.Deserialize<List<ExportedJsonObjectType>>(reader);
                // foreach (var item in jsonObject)
                // {
                //     dict.Add(item.Hash, item.Msisdn);
                // }
            }
            // Write JSON to a file Optimized
            Console.WriteLine($"Writing optimized JSON to file:: {dict.Count} records found.");

            System.Text.Json.JsonSerializer.Serialize(dict);
        }
    }
}