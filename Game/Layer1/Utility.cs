using System;
using System.IO;
using System.Text.Json;

namespace GameProject {
    public static class Utility {
        public static string RootPath => AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
        public static string GetPath(string name) => Path.Combine(RootPath, name);
        public static T LoadJson<T>(string name) where T : new() {
            T json;

            string jsonPath = Utility.GetPath(name);

            if (File.Exists(jsonPath)) {
                json = JsonSerializer.Deserialize<T>(File.ReadAllText(jsonPath));
            } else {
                json = new T();
                var options = new JsonSerializerOptions {
                    WriteIndented = true
                };
                string jsonString = JsonSerializer.Serialize(json, options);
                File.WriteAllText(jsonPath, jsonString);
            }

            return json;
        }
    }
}
