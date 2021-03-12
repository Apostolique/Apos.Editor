using System;
using Dcrew.Spatial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Entity : IBounds, IComparable<Entity> {
        public Entity(uint id, RectangleF r, uint order, int type) {
            Id = id;
            Bounds = new RotRect(r.X, r.Y, r.Width, r.Height);
            Order = order;
            Type = type;
        }

        public uint Id {
            get;
            set;
        }
        public RotRect Bounds {
            get => _bounds;
            set {
                _bounds = value;

                var bleeder = Assets.Bleeders[Type];
                float width = Bounds.Width * bleeder.Inset.Width;
                float height = Bounds.Height * bleeder.Inset.Height;

                float x = Bounds.X + bleeder.Inset.X * Bounds.Width;
                float y = Bounds.Y + bleeder.Inset.Y * Bounds.Height;

                _inset = new RectangleF(x, y, width, height);
            }
        }
        public RectangleF Inset => _inset;
        public uint Order {
            get;
            set;
        }
        public int Type { get; set; }

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
            var bleeder = Assets.Bleeders[Type];
            s.Draw(bleeder.Texture, new Rectangle((int)Bounds.X, (int)Bounds.Y, (int)Bounds.Width, (int)Bounds.Height), bleeder.Source, Color.White);
        }
        public void DrawHighlight(SpriteBatch s, float distance, float thickness, Color c) {
            s.DrawRectangle(Utility.ExpandRect(new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height), distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);

            s.DrawRectangle(Utility.ExpandRect(_inset, distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);
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

        RotRect _bounds;
        RectangleF _inset;
    }
    public class EntityPaste {
        public EntityPaste(RectangleF rect, int type) {
            Rect = rect;
            Type = type;
        }

        public RectangleF Rect { get; set; }
        public int Type { get; set; }
    }
}
