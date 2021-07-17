using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject {
    public class World {
        public World() { }

        public Dictionary<uint, Entity> Entities = new Dictionary<uint, Entity>();
        public AABBTree<Entity> AABBTree = new AABBTree<Entity>();

        public AABBTree<Entity> Lilypads = new AABBTree<Entity>();
        public AABBTree<Entity> Woods = new AABBTree<Entity>();
        public AABBTree<Entity> Clouds = new AABBTree<Entity>();

        [Flags]
        public enum LayerType {
            None = 0,
            Lilypads = 1,
            Woods = 2,
            Clouds = 4
        }

        public void DrawBackground(SpriteBatch s) {
            int width = s.GraphicsDevice.Viewport.Width;
            int height = s.GraphicsDevice.Viewport.Height;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            Matrix uv_transform = GetUVTransform(Assets.Background, new Vector2(0, 0), 10f, s.GraphicsDevice.Viewport);

            Assets.Infinite.Parameters["view_projection"].SetValue(Matrix.Identity * projection);
            Assets.Infinite.Parameters["uv_transform"].SetValue(Matrix.Invert(uv_transform));

            s.Begin(effect: Assets.Infinite, samplerState: SamplerState.LinearWrap);
            s.Draw(Assets.Background, s.GraphicsDevice.Viewport.Bounds, Color.White);
            s.End();
        }
        public void Draw(SpriteBatch s) {
            foreach (var e in AABBTree.Query(Camera.ViewRect).OrderBy(e => e))
                e.Draw(s);
        }

        private Matrix GetUVTransform(Texture2D t, Vector2 offset, float scale, Viewport v) {
            return
                Matrix.CreateScale(t.Width, t.Height, 1f) *
                Matrix.CreateScale(scale, scale, 1f) *
                Matrix.CreateTranslation(offset.X, offset.Y, 0f) *
                Camera.GetView(-10f) *
                Matrix.CreateScale(1f / v.Width, 1f / v.Height, 1f);
        }
    }
}
