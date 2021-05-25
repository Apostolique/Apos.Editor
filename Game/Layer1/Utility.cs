using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            }

            return json;
        }
        public static T EnsureJson<T>(string name) where T : new() {
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
            return MathF.Pow(square * Camera.ScreenToWorldScale, 2);
        }
        public static float ScreenArea(float width, float height) {
            return width * height * MathF.Pow(Camera.ScreenToWorldScale, 2);
        }

        public static int Mod(int x, int m) {
            return (x % m + m) % m;
        }

        public static void DrawParallelVertical(this SpriteBatch spriteBatch, RectangleF rectangle, Color color, float thickness = 1f, float layerDepth = 0) {
            var texture = Assets.Pixel;
            var topLeft = new Vector2(rectangle.X, rectangle.Y);
            var topRight = new Vector2(rectangle.Right - thickness, rectangle.Y);
            var bottomLeft = new Vector2(rectangle.X, rectangle.Bottom - thickness);
            var horizontalScale = new Vector2(rectangle.Width, thickness);
            var verticalScale = new Vector2(thickness, rectangle.Height);

            spriteBatch.Draw(texture, topLeft, null, color, 0f, Vector2.Zero, verticalScale, SpriteEffects.None, layerDepth);
            spriteBatch.Draw(texture, topRight, null, color, 0f, Vector2.Zero, verticalScale, SpriteEffects.None, layerDepth);
        }
        public static void DrawParallelHorizontal(this SpriteBatch spriteBatch, RectangleF rectangle, Color color, float thickness = 1f, float layerDepth = 0) {
            var texture = Assets.Pixel;
            var topLeft = new Vector2(rectangle.X, rectangle.Y);
            var topRight = new Vector2(rectangle.Right - thickness, rectangle.Y);
            var bottomLeft = new Vector2(rectangle.X, rectangle.Bottom - thickness);
            var horizontalScale = new Vector2(rectangle.Width, thickness);
            var verticalScale = new Vector2(thickness, rectangle.Height);

            spriteBatch.Draw(texture, topLeft, null, color, 0f, Vector2.Zero, horizontalScale, SpriteEffects.None, layerDepth);
            spriteBatch.Draw(texture, bottomLeft, null, color, 0f, Vector2.Zero, horizontalScale, SpriteEffects.None, layerDepth);
        }
    }
}
