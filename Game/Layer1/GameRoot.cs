using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Input;
using Apos.History;
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

            _historyHandler = new HistoryHandler(null);

            _quadtree = new Quadtree<Entity>();
            _hoveredEntities = new Quadtree<Entity>();
            _selectedEntities = new Quadtree<Entity>();
            Camera.Setup();
            _selection = new RectEdit();
            _edit = new RectEdit();

            Assets.LoadFonts(Content, GraphicsDevice);
            Assets.Setup(Content);

            InputHelper.Setup(this);
        }

        protected override void Update(GameTime gameTime) {
            // TODO: Start creating an API over the entity quadtree dictionary, etc. For addition, removal, updates.
            InputHelper.UpdateSetup();
            _fps.Update(gameTime.ElapsedGameTime.TotalMilliseconds);

            if (Triggers.Quit.Pressed())
                Exit();

            if (Triggers.ResetDroppedFrames.Pressed())
                _fps.DroppedFrames = 0;
            bool shiftModifier = Triggers.AddToSelection.Held();
            bool ctrlModifier = Triggers.RemoveFromSelection.Held();

            if (Triggers.Redo.Pressed()) {
                Utility.ClearQuadtree(_selectedEntities);
                _historyHandler.Redo();
                _edit.Rect = null;
            }
            if (Triggers.Undo.Pressed()) {
                Utility.ClearQuadtree(_selectedEntities);
                _historyHandler.Undo();
                _edit.Rect = null;
            }

            Camera.UpdateInput();
            bool isEditDone = false;
            if (!shiftModifier && !ctrlModifier && !Triggers.SkipEdit.Held()) {
                isEditDone = _edit.UpdateInput(Camera.MouseWorld, false);
            }
            var isSelectionDone = _selection.UpdateInput(Camera.MouseWorld);

            Utility.ClearQuadtree(_hoveredEntities);
            if (_selection.Rect != null) {
                // Group element hover
                var r = _selection.Rect.Value;
                foreach (var e in _quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height))) {
                    _hoveredEntities.Add(e);
                }
            } else {
                // Do a single element hover
                bool addSelected = false;
                if (_selectedEntities.Count() == 1) {
                    var bounds = _selectedEntities.First().Bounds;
                    addSelected = !bounds.Contains(Camera.MouseWorld) && Utility.ExpandRect(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), _edit.HandleDistanceWorld).Contains(Camera.MouseWorld);
                }

                IOrderedEnumerable<Entity> hoverUnderMouse;
                IOrderedEnumerable<Entity> selectedAndHovered;
                if (addSelected) {
                    hoverUnderMouse = _quadtree.Query(Camera.MouseWorld).Append(_selectedEntities.First()).OrderBy(e => e);
                    selectedAndHovered = _selectedEntities.Query(Camera.MouseWorld).Append(_selectedEntities.First()).OrderBy(e => e);
                } else {
                    hoverUnderMouse = _quadtree.Query(Camera.MouseWorld).OrderBy(e => e);
                    selectedAndHovered = _selectedEntities.Query(Camera.MouseWorld).OrderBy(e => e);
                }
                var hoverCount = hoverUnderMouse.Count();
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
                Utility.ClearQuadtree(_hoveredEntities);
                var all = _selectedEntities.ToArray();
                _historyHandler.AutoCommit = false;
                foreach (var e in all) {
                    HistoryRemoveEntity(e.Id, new RectangleF(e.Bounds.XY, e.Bounds.Size), e.SortOrder);
                    _selectedEntities.Remove(e);
                }
                _historyHandler.Commit();
                _historyHandler.AutoCommit = true;
            }

            if (Triggers.CreateEntity.Pressed()) {
                Utility.ClearQuadtree(_hoveredEntities);
                HistoryCreateEntity(GetNextId(), new RectangleF(Camera.MouseWorld, new Vector2(100, 100)), GetNextSortOrder());

                isSelectionDone = true;
            }

            if (Triggers.SpawnStuff.Pressed()) {
                Utility.ClearQuadtree(_hoveredEntities);
                Random r = new Random();
                _historyHandler.AutoCommit = false;
                for (int i = 0; i < 10000; i++) {
                    var screenBounds = Camera.WorldBounds;
                    var origin = Camera.Origin;
                    float minX = screenBounds.Left;
                    float maxX = screenBounds.Right;
                    float minY = screenBounds.Top;
                    float maxY = screenBounds.Bottom;

                    HistoryCreateEntity(GetNextId(), new RectangleF(new Vector2(r.NextSingle(minX, maxX), r.NextSingle(minY, maxY)) - origin, new Vector2(r.NextSingle(50, 200), r.NextSingle(50, 200))), GetNextSortOrder());
                }
                _historyHandler.Commit();
                _historyHandler.AutoCommit = true;

                isSelectionDone = true;
            }

            if (isSelectionDone) {
                if (!shiftModifier && !ctrlModifier) {
                    Utility.ClearQuadtree(_selectedEntities);
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
                        _editRectStartXY = new Vector2(x1, y1);
                        _editRectStartSize = new Vector2(x2 - x1, y2 - y1);
                        _edit.Rect = new RectangleF(_editRectStartXY, _editRectStartSize);
                        first.Offset = pos1 - _editRectStartXY;
                    }
                } else {
                    _edit.Rect = null;
                }
                _selection.Rect = null;
            }

            if (_edit.Rect != null && !isEditDone) {
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
            if (_edit.Rect != null && isEditDone) {
                using (IEnumerator<Entity> e = _selectedEntities.GetEnumerator()) {
                    _historyHandler.AutoCommit = false;
                    e.MoveNext();
                    var first = e.Current;
                    Vector2 oldFirstStart = first.Offset + _editRectStartXY;
                    Vector2 newFirstSTart = first.Offset + _edit.Rect.Value.Position;
                    HistoryMoveEntity(first.Id, oldFirstStart, newFirstSTart);

                    while (e.MoveNext()) {
                        var current = e.Current;
                        HistoryMoveEntity(current.Id, current.Offset + oldFirstStart, current.Offset + newFirstSTart);
                    }

                    if (_selectedEntities.Count() == 1) {
                        HistoryResizeEntity(first.Id, _editRectStartSize, _edit.Rect.Value.Size);
                    }
                    _historyHandler.Commit();
                    _historyHandler.AutoCommit = true;
                }
            }

            InputHelper.UpdateCleanup();
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

            foreach (var e in _selectedEntities.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.DrawHighlight(_s, 0f, 2f, Color.White);
            foreach (var e in _hoveredEntities.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.DrawHighlight(_s, -2f, 3f, Color.Black);
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
        private void HistoryCreateEntity(uint id, RectangleF r, uint sortOrder) {
            _historyHandler.Add(() => {
                RemoveEntity(id);
            }, () => {
                CreateEntity(id, r, sortOrder);
            });
        }
        private void HistoryRemoveEntity(uint id, RectangleF r, uint sortOrder) {
            _historyHandler.Add(() => {
                CreateEntity(id, r, sortOrder);
            }, () => {
                RemoveEntity(id);
            });
        }
        private void HistoryMoveEntity(uint id, Vector2 oldXY, Vector2 newXY) {
            _historyHandler.Add(() => {
                MoveEntity(id, oldXY);
            }, () => {
                MoveEntity(id, newXY);
            });
        }
        private void HistoryResizeEntity(uint id, Vector2 oldSize, Vector2 newSize) {
            _historyHandler.Add(() => {
                ResizeEntity(id, oldSize);
            }, () => {
                ResizeEntity(id, newSize);
            });
        }
        private void CreateEntity(uint id, RectangleF r, uint sortOrder) {
            Entity e = new Entity(id, r, sortOrder);
            _quadtree.Add(e);
            _entities.Add(e.Id, e);
            _hoveredEntities.Add(e);
        }
        private void RemoveEntity(uint id) {
            Entity e = _entities[id];
            _quadtree.Remove(e);
            _entities.Remove(e.Id);
            _hoveredEntities.Remove(e);
            _selectedEntities.Remove(e);
        }
        private void MoveEntity(uint id, Vector2 xy) {
            Entity e = _entities[id];
            var bound = e.Bounds;
            bound.XY = xy;
            e.Bounds = bound;
            _quadtree.Update(e);
            _hoveredEntities.Update(e);
            _selectedEntities.Update(e);
        }
        private void ResizeEntity(uint id, Vector2 size) {
            Entity e = _entities[id];
            var bound = e.Bounds;
            bound.Size = size;
            e.Bounds = bound;
            _quadtree.Update(e);
            _hoveredEntities.Update(e);
            _selectedEntities.Update(e);
        }

        GraphicsDeviceManager _graphics;
        SpriteBatch _s;

        uint _lastId = 0;
        uint _sortOrder = 0;
        int _cycleIndex = 0;
        Vector2? _cycleMouse = Vector2.Zero;

        RectEdit _selection;
        RectEdit _edit;
        Vector2 _editRectStartXY = Vector2.Zero;
        Vector2 _editRectStartSize = Vector2.Zero;
        Quadtree<Entity> _quadtree;
        Dictionary<uint, Entity> _entities = new Dictionary<uint, Entity>();

        HistoryHandler _historyHandler;
        Quadtree<Entity> _hoveredEntities;
        Quadtree<Entity> _selectedEntities;

        FPSCounter _fps = new FPSCounter();
    }
}
