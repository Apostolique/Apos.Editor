using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Entity : IComparable<Entity> {
        public Entity(uint id, RectangleF r, uint order, int type) {
            Id = id;
            Inset = new RectangleF(r.X, r.Y, r.Width, r.Height);
            Order = order;
            Type = type;
        }

        public uint Id { get; set; }
        public bool IsNegative { get; set; } = false;
        public int Leaf1 { get; set; } = -1;
        public int Leaf2 { get; set; } = -1;
        public int Leaf3 { get; set; } = -1;
        public RectangleF Rect {
            get => _rect;
            set {
                _rect = value;

                var bleeder = Assets.Bleeders[_type];
                float width = _rect.Width * bleeder.Inset.Width;
                float height = _rect.Height * bleeder.Inset.Height;

                float x = _rect.X + _rect.Width * bleeder.Inset.X;
                float y = _rect.Y + _rect.Height * bleeder.Inset.Y;

                _inset = new RectangleF(x, y, width, height);
            }
        }
        public RectangleF Inset {
            get => _inset;
            set {
                _inset = value;

                var bleeder = Assets.Bleeders[_type];
                float width = _inset.Width / bleeder.Inset.Width;
                float height = _inset.Height / bleeder.Inset.Height;

                float x = _inset.X - width * bleeder.Inset.X;
                float y = _inset.Y - height * bleeder.Inset.Y;

                _rect = new RectangleF(x, y, width, height);
            }
        }
        public uint Order { get; set; }
        public int Type {
            get => _type;
            set {
                Bleeder? bleeder;
                if (Assets.Bleeders.TryGetValue(value, out bleeder)) {
                    _type = value;
                } else {
                    var b = Assets.Bleeders.First();
                    _type = b.Key;
                    bleeder = b.Value;
                }

                Inset = new RectangleF(_inset.Position, bleeder.Source.Size.ToVector2() * bleeder.Inset.Size);
                Layer = bleeder.Layer;
            }
        }
        public World.LayerType Layer { get; set; }

        // Not really part of the object. Useful for the editor.
        public Vector2 Offset { get; set; }
        public uint NextOrder { get; set; }

        public void Draw(SpriteBatch s) {
            var bleeder = Assets.Bleeders[_type];
            s.Draw(bleeder.Texture, new Rectangle((int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height), bleeder.Source, Color.White);
        }
        public void DrawHighlight(SpriteBatch s, float distance, float thickness, Color c) {
            s.DrawRectangle(Utility.ExpandRect(new RectangleF(_rect.X, _rect.Y, _rect.Width, _rect.Height), distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);
            s.DrawRectangle(Utility.ExpandRect(new RectangleF(_inset.X, _inset.Y, _inset.Width, _inset.Height), distance * Camera.ScreenToWorldScale), c, thickness * Camera.ScreenToWorldScale);
        }

        public int CompareTo(Entity? value) {
            if (value == null) return 1;
            int compareLayer = Layer.CompareTo(value.Layer);
            int compareOrder = compareLayer == 0 ? Order.CompareTo(value.Order) : compareLayer;
            return compareOrder == 0 ? Id.CompareTo(value.Id) : compareOrder;
        }
        public int GetHashCode([DisallowNull] Entity obj) {
            return obj.Id.GetHashCode();
        }
        public bool Equals([AllowNull] Entity a, [AllowNull] Entity b) {
            if (a == null && b == null) return true;
            else if (a == null || b == null) return false;
            else return a.Id == b.Id;
        }

        RectangleF _rect;
        RectangleF _inset;
        int _type;
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
