using System;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Entity : IBounds, IComparable<Entity> {
        public Entity(uint id, RectangleF r, uint order) {
            Id = id;
            Bounds = new RotRect(r.X, r.Y, r.Width, r.Height);
            Order = order;
        }

        public uint Id {
            get;
            set;
        }
        public RotRect Bounds {
            get;
            set;
        }
        public uint Order {
            get;
            set;
        }

        // Not really part of the object. Useful for the editor.
        public Vector2 Offset {
            get;
            set;
        }
        public uint NextOrder {
            get;
            set;
        }

        public void Draw(SpriteBatch s) {
            s.FillRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), Color.Red * 0.7f);
        }
        public void DrawHighlight(SpriteBatch s, float distance, float thickness, Color c) {
            s.DrawRectangle(Utility.ExpandRect(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);
        }

        public int CompareTo(Entity? value) {
            if (value == null) return 1;
            int compareTo = Order.CompareTo(value.Order);
            return compareTo == 0 ? Id.CompareTo(value.Id) : compareTo;
        }
        public override int GetHashCode() {
            return Id.GetHashCode();
        }
        public override bool Equals(object? obj) {
            return obj is Entity && Id == ((Entity)obj).Id;
        }
    }
    public class EntityPaste {
        public EntityPaste(RectangleF rect) {
            Rect = rect;
        }

        public RectangleF Rect { get; set; }
    }
}
