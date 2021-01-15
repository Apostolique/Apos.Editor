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

        public void Draw(SpriteBatch s) {
            s.FillRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), Color.Red * 0.7f);
        }
        public void DrawHighlight(SpriteBatch s, Color c) {
            s.DrawRectangle(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), c, 2f * Camera.ScreenToWorldScale);
        }

        public int CompareTo(Entity value) {
            if (SortOrder == value.SortOrder) {
                return Id.CompareTo(value.Id);
            }
            return SortOrder.CompareTo(value.SortOrder);
        }
        public override int GetHashCode() {
            return Id.GetHashCode();
        }
        public override bool Equals(object obj) {
            return obj is Entity && Id == ((Entity)obj).Id;
        }
    }
}
