using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Entity : IBounds {
        public Entity(RectangleF r) {
            Bounds = new RotRect(r.X, r.Y, r.Width, r.Height);
        }

        public RotRect Bounds {
            get;
            set;
        }

        public void Draw(SpriteBatch s) {
            s.FillRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), Color.Red * 0.7f);
        }
        public void DrawHighlight(SpriteBatch s, Color c) {
            s.DrawRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), c * 0.7f, 2f * Camera.ScreenToWorldScale);
        }
    }
}
