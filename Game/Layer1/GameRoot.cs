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
            _selectedEntities = new Quadtree<Entity>();
            Camera.Setup();
            _selection = new RectEdit();
            _edit = new RectEdit();

            Assets.LoadFonts(Content, GraphicsDevice);
            Assets.Setup(Content);

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            InputHelper.UpdateSetup();
            _fps.Update(gameTime.ElapsedGameTime.TotalMilliseconds);

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
                // Group element hover
                var r = _selection.Rect.Value;
                _hoveredEntities.UnionWith(_quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height)));
            } else {
                // Do a single element hover
                bool addSelected = false;
                if (_selectedEntities.Count() == 1) {
                    var bounds = _selectedEntities.First().Bounds;
                    addSelected = !bounds.Contains(Camera.MouseWorld) && Utility.ExpandRect(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), _edit.HandleDistanceWorld).Contains(Camera.MouseWorld);
                }

                IOrderedEnumerable<Entity> hoverUnderMouse;
                if (addSelected) {
                    hoverUnderMouse = _quadtree.Query(Camera.MouseWorld).Append(_selectedEntities.First()).OrderBy(e => e);
                } else {
                    hoverUnderMouse = _quadtree.Query(Camera.MouseWorld).OrderBy(e => e);
                }
                var hoverCount = hoverUnderMouse.Count();
                var selectedAndHovered = _selectedEntities.Query(Camera.MouseWorld).OrderBy(e => e);
                int cycleReset = 0;
                if (selectedAndHovered.Count() > 0) {
                    cycleReset = hoverCount - 1 - hoverUnderMouse.ToList().IndexOf(selectedAndHovered.Last());
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

                if (hoverCount > 0) {
                    _hoveredEntities.Add(hoverUnderMouse.ElementAt(Utility.Mod(hoverCount - 1 - _cycleIndex, hoverCount)));
                }
            }

            if (Triggers.RemoveEntity.Pressed()) {
                _edit.Rect = null;
                _hoveredEntities.Clear();
                var all = _selectedEntities.ToArray();
                foreach (var e in all) {
                    _quadtree.Remove(e);
                    _entities.Remove(e.Id);
                    _selectedEntities.Remove(e);
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

            if (Triggers.SpawnStuff.Pressed()) {
                _hoveredEntities.Clear();
                Random r = new Random();
                for (int i = 0; i < 1000; i++) {
                    var screenBounds = Camera.WorldBounds;
                    var origin = Camera.Origin;
                    float minX = screenBounds.Left;
                    float maxX = screenBounds.Right;
                    float minY = screenBounds.Top;
                    float maxY = screenBounds.Bottom;

                    var newEntity = new Entity(GetNextId(), new RectangleF(new Vector2(r.NextSingle(minX, maxX), r.NextSingle(minY, maxY)) - origin, new Vector2(r.NextSingle(50, 200), r.NextSingle(50, 200))), GetNextSortOrder());
                    _quadtree.Add(newEntity);
                    _entities.Add(newEntity.Id, newEntity);

                    isSelectionDone = true;
                    _hoveredEntities.Add(newEntity);
                    _selection.Rect = null;
                }
            }

            if (isSelectionDone) {
                if (!shiftModifier && !ctrlModifier) {
                    var all = _selectedEntities.ToArray();
                    foreach (var e in all) {
                        _selectedEntities.Remove(e);
                    }
                }
                if (ctrlModifier) {
                    foreach (var e in _hoveredEntities) {
                        _selectedEntities.Remove(e);
                    }
                } else {
                    foreach (var e in _hoveredEntities) {
                        if (!_selectedEntities.Contains(e)) {
                            _selectedEntities.Add(e);
                        }
                    }
                }

                if (_selectedEntities.Count() >= 1) {
                    using (IEnumerator<Entity> e = _selectedEntities.GetEnumerator()) {
                        e.MoveNext();
                        var first = e.Current;
                        var pos1 = first.Bounds.XY;

                        float x1 = first.Bounds.X;
                        float x2 = first.Bounds.X + first.Bounds.Width;
                        float y1 = first.Bounds.Y;
                        float y2 = first.Bounds.Y + first.Bounds.Height;

                        while (e.MoveNext()) {
                            var current = e.Current;
                            x1 = MathF.Min(current.Bounds.X, x1);
                            x2 = MathF.Max(current.Bounds.X + current.Bounds.Width, x2);
                            y1 = MathF.Min(current.Bounds.Y, y1);
                            y2 = MathF.Max(current.Bounds.Y + current.Bounds.Height, y2);

                            var pos2 = current.Bounds.XY;
                            current.Offset = pos2 - pos1;
                        }

                        _edit.IsResizable = _selectedEntities.Count() == 1;
                        _edit.Rect = new RectangleF(x1, y1, x2 - x1, y2 - y1);
                        first.Offset = pos1 - new Vector2(x1, y1);
                    }
                } else {
                    _edit.Rect = null;
                }
                _selection.Rect = null;
            }

            if (_edit.Rect != null) {
                using (IEnumerator<Entity> e = _selectedEntities.GetEnumerator()) {
                    e.MoveNext();
                    var first = e.Current;
                    var bound = first.Bounds;
                    bound.XY = first.Offset + _edit.Rect.Value.Position;
                    first.Bounds = bound;

                    while (e.MoveNext()) {
                        var current = e.Current;
                        bound = current.Bounds;
                        bound.XY = current.Offset + first.Bounds.XY;
                        current.Bounds = bound;
                        _quadtree.Update(current);
                        _selectedEntities.Update(current);
                    }

                    if (_selectedEntities.Count() == 1) {
                        bound.Size = _edit.Rect.Value.Size;
                        first.Bounds = bound;
                    }
                    _quadtree.Update(first);
                    _selectedEntities.Update(first);
                }
            }

            InputHelper.UpdateCleanup();
            _quadtree.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            _fps.Draw();

            GraphicsDevice.Clear(new Color(22, 22, 22));

            _s.Begin(transformMatrix: Camera.View);
            foreach (var e in _quadtree.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin).OrderBy(e => e))
                e.Draw(_s);

            _selection.Draw(_s);
            _edit.Draw(_s);

            foreach (var e in _hoveredEntities)
                e.DrawHighlight(_s, -2f, 3f, Color.Black);
            foreach (var e in _selectedEntities.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.DrawHighlight(_s, 0f, 2f, Color.White);
            _s.End();

            var font = Assets.FontSystem.GetFont(30);
            _s.Begin();
            // Draw UI
            _s.DrawString(font, $"fps: {_fps.FramesPerSecond} - Dropped Frames: {_fps.DroppedFrames} - Draw ms: {_fps.TimePerFrame} - Update ms: {_fps.TimePerUpdate}", new Vector2(10, 10), Color.White);
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
        Quadtree<Entity> _selectedEntities;

        FPSCounter _fps = new FPSCounter();
    }
}
