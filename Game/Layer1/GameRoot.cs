using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Input;
using Dcrew.Spatial;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class GameRoot : Game {
        public GameRoot() {
            _graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            base.Initialize();
        }

        protected override void LoadContent() {
            _s = new SpriteBatch(GraphicsDevice);

            Settings settings = Utility.LoadJson<Settings>("Settings.json");

            _graphics.PreferredBackBufferWidth = settings.Width;
            _graphics.PreferredBackBufferHeight = settings.Height;
            IsFixedTimeStep = settings.IsFixedTimeStep;
            _graphics.SynchronizeWithVerticalRetrace = settings.IsVSync;
            _graphics.ApplyChanges();

            _quadtree = new Quadtree<Entity>();
            Camera.Setup();
            _selection = new RectEdit();
            _edit = new RectEdit();

            Assets.LoadFonts(Content, GraphicsDevice);
            Assets.Setup(Content);

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();

            if (Triggers.Quit.Pressed())
                Exit();

            bool shiftModifier = Triggers.AddToSelection.Held();
            bool ctrlModifier = Triggers.RemoveFromSelection.Held();

            Camera.UpdateInput();
            if (!shiftModifier && !ctrlModifier) {
                _edit.UpdateInput(Camera.MouseWorld, false);
            }
            var isSelectionDone = _selection.UpdateInput(Camera.MouseWorld);

            _hoveredEntities.Clear();
            if (_selection.Rect != null) {
                var r = _selection.Rect.Value;
                _hoveredEntities.UnionWith(_quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height)));
            } else {
                var hoverUnderMouse = _quadtree.Query(Camera.MouseWorld.ToPoint()).OrderBy(e => e);
                var selectedAndHovered = _selectedEntities.Where(eo => hoverUnderMouse.Contains(eo.Entity)).Select(eo => eo.Entity).OrderBy(e => e);
                int cycleReset = 0;
                if (selectedAndHovered.Count() > 0) {
                    cycleReset = hoverUnderMouse.Count() - 1 - hoverUnderMouse.ToList().IndexOf(selectedAndHovered.Last());
                    if (_cycleMouse == null) {
                        _cycleIndex = cycleReset;
                    }
                }

                if (_cycleMouse != null && Vector2.DistanceSquared(_cycleMouse.Value, Camera.MouseWorld) > Utility.ScreenArea(10)) {
                    _cycleIndex = cycleReset;
                    _cycleMouse = null;
                }
                int scrollDelta = InputHelper.NewMouse.ScrollWheelValue - InputHelper.OldMouse.ScrollWheelValue;
                if (scrollDelta != 0 && Triggers.SelectionCycle.Held()) {
                    _cycleIndex += MathF.Sign(scrollDelta);
                    _cycleMouse = Camera.MouseWorld;
                }

                var result = _quadtree.Query(Camera.MouseWorld.ToPoint()).OrderBy(e => e).ToList();
                if (result.Count > 0) {
                    _hoveredEntities.Add(result[Utility.Mod(result.Count - 1 - _cycleIndex, result.Count)]);
                }
            }

            if (Triggers.CreateEntity.Pressed()) {
                var newEntity = new Entity(GetNextId(), new RectangleF(Camera.MouseWorld, new Vector2(100, 100)), GetNextSortOrder());
                _quadtree.Add(newEntity);
                _entities.Add(newEntity.Id, newEntity);

                isSelectionDone = true;
                _hoveredEntities.Clear();
                _hoveredEntities.Add(newEntity);
                _selection.Rect = null;
            }

            if (isSelectionDone) {
                if (!shiftModifier && !ctrlModifier) {
                    _selectedEntities.Clear();
                }
                if (ctrlModifier) {
                    foreach (var e in _hoveredEntities) {
                        _selectedEntities.RemoveWhere(se => e == se.Entity);
                    }
                } else {
                    _selectedEntities.UnionWith(_hoveredEntities.Select(e => {
                        return new EntityOffset(e, new Vector2(0, 0));
                    }));
                }

                if (_selectedEntities.Count >= 1) {
                    using (IEnumerator<EntityOffset> e = _selectedEntities.GetEnumerator()) {
                        e.MoveNext();
                        var first = e.Current;
                        var pos1 = first.Entity.Bounds.XY;

                        float x1 = first.Entity.Bounds.X;
                        float x2 = first.Entity.Bounds.X + first.Entity.Bounds.Width;
                        float y1 = first.Entity.Bounds.Y;
                        float y2 = first.Entity.Bounds.Y + first.Entity.Bounds.Height;

                        while (e.MoveNext()) {
                            var current = e.Current;
                            x1 = MathF.Min(current.Entity.Bounds.X, x1);
                            x2 = MathF.Max(current.Entity.Bounds.X + current.Entity.Bounds.Width, x2);
                            y1 = MathF.Min(current.Entity.Bounds.Y, y1);
                            y2 = MathF.Max(current.Entity.Bounds.Y + current.Entity.Bounds.Height, y2);

                            var pos2 = current.Entity.Bounds.XY;

                            current.Offset = pos2 - pos1;
                        }

                        _edit.IsResizable = _selectedEntities.Count == 1;
                        _edit.Rect = new RectangleF(x1, y1, x2 - x1, y2 - y1);
                        first.Offset = pos1 - new Vector2(x1, y1);
                    }
                } else {
                    _edit.Rect = null;
                }
                _selection.Rect = null;
            }

            if (_edit.Rect != null) {
                using (IEnumerator<EntityOffset> e = _selectedEntities.GetEnumerator()) {
                    e.MoveNext();
                    var first = e.Current;
                    var bound = first.Entity.Bounds;
                    bound.XY = first.Offset + _edit.Rect.Value.Position;
                    first.Entity.Bounds = bound;

                    while (e.MoveNext()) {
                        var current = e.Current;
                        bound = current.Entity.Bounds;
                        bound.XY = current.Offset + first.Entity.Bounds.XY;
                        current.Entity.Bounds = bound;
                        _quadtree.Update(current.Entity);
                    }

                    if (_selectedEntities.Count == 1) {
                        bound.Size = _edit.Rect.Value.Size;
                        first.Entity.Bounds = bound;
                    }
                    _quadtree.Update(first.Entity);
                }
            }

            InputHelper.UpdateCleanup();
            _quadtree.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(new Color(22, 22, 22));

            _s.Begin(transformMatrix: Camera.View);
            foreach (var e in _quadtree.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin).OrderBy(e => e))
                e.Draw(_s);

            _selection.Draw(_s);
            _edit.Draw(_s);

            foreach (var e in _hoveredEntities)
                e.DrawHighlight(_s, -2f, 3f, Color.Black);
            foreach (var eo in _selectedEntities)
                eo.Entity.DrawHighlight(_s, 0f, 2f, Color.White);
            _s.End();

            var font = Assets.FontSystem.GetFont(30);
            _s.Begin();
            // Draw UI
            _s.DrawString(font, $"{_cycleMouse} - {Camera.MouseWorld}", new Vector2(10, 10), Color.White);
            _s.End();

            base.Draw(gameTime);
        }

        private uint GetNextId() {
            return _lastId++;
        }
        private uint GetNextSortOrder() {
            return _sortOrder++;
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        uint _lastId = 0;
        uint _sortOrder = 0;
        int _cycleIndex = 0;
        Vector2? _cycleMouse = Vector2.Zero;

        RectEdit _selection;
        RectEdit _edit;
        Quadtree<Entity> _quadtree;
        Dictionary<uint, Entity> _entities = new Dictionary<uint, Entity>();

        HashSet<Entity> _hoveredEntities = new HashSet<Entity>();
        HashSet<EntityOffset> _selectedEntities = new HashSet<EntityOffset>();

        class EntityOffset {
            public EntityOffset(Entity entity, Vector2 offset) {
                Entity = entity;
                Offset = offset;
            }

            public Entity Entity {
                get;
                set;
            }
            public Vector2 Offset {
                get;
                set;
            }

            public override int GetHashCode() {
                return Entity.GetHashCode();
            }
            public override bool Equals(object obj) {
                return obj is EntityOffset && Entity.Equals(((EntityOffset)obj).Entity);
            }
        }
    }
}
