using System;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Entity : IBounds, IComparable<Entity> {
        public Entity(uint id, RectangleF r, uint sortOrder) {
            Id = id;
            Bounds = new RotRect(r.X, r.Y, r.Width, r.Height);
            SortOrder = sortOrder;
        }

        public uint Id {
            get;
            set;
        }
        public RotRect Bounds {
            get;
            set;
        }
        public uint SortOrder {
            get;
            set;
        }
        public Vector2 Offset {
            get;
            set;
        }

        public void Draw(SpriteBatch s) {
            s.FillRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), Color.Red * 0.7f);
        }
        public void DrawHighlight(SpriteBatch s, float distance, float thickness, Color c) {
            s.DrawRectangle(Utility.ExpandRect(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);
        }

        public int CompareTo(Entity value) {
            int compareTo = SortOrder.CompareTo(value.SortOrder);
            return compareTo == 0 ? Id.CompareTo(value.Id) : compareTo;
        }
        public override int GetHashCode() {
            return Id.GetHashCode();
        }
        public override bool Equals(object obj) {
            return obj is Entity && Id == ((Entity)obj).Id;
        }
    }
}
