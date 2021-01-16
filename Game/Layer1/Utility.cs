using System;
using System.IO;
using System.Text.Json;
using MonoGame.Extended;

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

        public static RectangleF ExpandRect(RectangleF r, float distance) {
            return new RectangleF(r.X - distance, r.Y - distance, r.Width + distance * 2, r.Height + distance * 2);
        }
        public static float ScreenArea(float square) {
            return ScreenArea(square, square);
        }
        public static float ScreenArea(float width, float height) {
            return MathF.Pow(width * height * Camera.ScreenToWorldScale, 2);
        }

        public static int Mod(int x, int m) {
            return (x % m + m) % m;
        }
    }
}
