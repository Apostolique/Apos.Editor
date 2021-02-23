using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Gui;
using Apos.History;
using Apos.Input;
using Dcrew.Spatial;
using GameProject.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Editor {
        public Editor(World world) {
            _world = world;
            _quadtree = _world.Quadtree;
            _entities = _world.Entities;

            _historyHandler = new HistoryHandler(null);
            _selectedEntities = new Quadtree<Entity>();

            _selection = new RectEdit();
            _edit = new RectEdit();

            _ui = new IMGUI();
            // If we end up with more UIs, this should be called at the beginning of the update loop.
            GuiHelper.CurrentIMGUI = _ui;
        }

        public void Update(GameTime gameTime) {
            _ui.UpdateAll(gameTime);

            if (Triggers.SelectionDrag.Pressed(false)) {
                _ui.GrabFocus(null);
            }

            bool shiftModifier = Triggers.AddToSelection.Held();
            bool ctrlModifier = Triggers.RemoveFromSelection.Held();

            Camera.UpdateInput();

            if (Triggers.Redo.Pressed()) {
                _historyHandler.Redo();
                ComputedSelectionBounds();
            }
            if (Triggers.Undo.Pressed()) {
                _historyHandler.Undo();
                ComputedSelectionBounds();
            }

            bool isEditDone = false;
            if (!shiftModifier && !ctrlModifier && !Triggers.SkipEdit.Held()) {
                isEditDone = _edit.UpdateInput(Camera.MouseWorld, false);
            }
            var isSelectionDone = _selection.UpdateInput(Camera.MouseWorld);

            ApplyEdit(isEditDone);

            SingleHover();

            if (Triggers.Create.Pressed()) {
                Create();
                isSelectionDone = true;
            }
            if (Triggers.CreateStuff.Pressed()) {
                CreateStuff();
                isSelectionDone = true;
            }
            if (Triggers.Paste.Pressed()) {
                Paste(Camera.MouseWorld);
                isSelectionDone = true;
            }

            if (isSelectionDone) {
                ApplySelection(shiftModifier, ctrlModifier);
            }

            if (Triggers.ResetOrder.Pressed()) {
                ResetOrder();
            }

            if (Triggers.Remove.Pressed()) {
                Remove();
            }
            if (Triggers.Cut.Pressed()) {
                Cut();
            }
            if (Triggers.Copy.Pressed()) {
                Copy();
            }
        }
        public void Draw(SpriteBatch s) {
            _selection.Draw(s);
            _edit.Draw(s);

            foreach (var e in _selectedEntities.Query(Camera.WorldBounds, Camera.Angle, Camera.Origin))
                e.DrawHighlight(s, 0f, 2f, Color.White);
            foreach (var e in GetHovers(true))
                e.DrawHighlight(s, -2f, 3f, Color.Black);
        }
        public void DrawUI(SpriteBatch s, GameTime gameTime) {
            _ui.Draw(gameTime);
        }

        private uint GetNextId() {
            return _lastId++;
        }
        private uint GetNextOrder() {
            return _order++;
        }
        private void HistoryCreateEntity(uint id, RectangleF r, uint order, Tile.Type type) {
            _historyHandler.Add(() => {
                RemoveEntity(id);
            }, () => {
                CreateEntity(id, r, order, type);
            });
        }
        private void HistoryRemoveEntity(uint id, RectangleF r, uint order, Tile.Type type) {
            _historyHandler.Add(() => {
                CreateEntity(id, r, order, type);
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
        private void HistoryOrderEntity(uint id, uint oldOrder, uint newOrder) {
            _historyHandler.Add(() => {
                OrderEntity(id, oldOrder);
            }, () => {
                OrderEntity(id, newOrder);
            });
        }
        private void HistoryTypeEntity(uint id, Tile.Type oldType, Tile.Type newType) {
            _historyHandler.Add(() => {
                TypeEntity(id, oldType);
            }, () => {
                TypeEntity(id, newType);
            });
        }
        private void CreateEntity(uint id, RectangleF r, uint order, Tile.Type type) {
            Entity e = new Entity(id, r, order, type);
            _quadtree.Add(e);
            _entities.Add(e.Id, e);
            if (_shouldAddNewToHover) _newEntitiesHover.Push(e.Id);
        }
        private void RemoveEntity(uint id) {
            Entity e = _entities[id];
            _quadtree.Remove(e);
            _entities.Remove(e.Id);
            _selectedEntities.Remove(e);
        }
        private void MoveEntity(uint id, Vector2 xy) {
            Entity e = _entities[id];
            var bound = e.Bounds;
            bound.XY = xy;
            e.Bounds = bound;
            _quadtree.Update(e);
            _selectedEntities.Update(e);
        }
        private void ResizeEntity(uint id, Vector2 size) {
            Entity e = _entities[id];
            var bound = e.Bounds;
            bound.Size = size;
            e.Bounds = bound;
            _quadtree.Update(e);
            _selectedEntities.Update(e);
        }
        private void OrderEntity(uint id, uint order) {
            Entity e = _entities[id];
            e.Order = order;
        }
        private void TypeEntity(uint id, Tile.Type type) {
            Entity e = _entities[id];
            e.Type = type;
        }

        private void SingleHover() {
            _hoveredEntity = null;
            if (_selection.Rect == null) {
                bool addSelected = false;
                Entity? first = null;
                // If there's a single element selected, it can have resize handles. Expand it's bounds to see if we're hovering that.
                if (_selectedEntities.Count() == 1) {
                    first = _selectedEntities.First();
                    var bounds = first.Bounds;
                    addSelected = !bounds.Contains(Camera.MouseWorld) && Utility.ExpandRect(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), _edit.HandleDistanceWorld).Contains(Camera.MouseWorld);
                }

                IOrderedEnumerable<Entity> hoversUnderMouse;
                IOrderedEnumerable<Entity> selectedAndHovered;
                if (addSelected) {
                    hoversUnderMouse = _quadtree.Query(Camera.MouseWorld).Append(first!).OrderBy(e => e);
                    // This can only happen when there's only 1 selection so we can do a special case here.
                    selectedAndHovered = new Entity[] { first! }.OrderBy(e => 1);
                } else {
                    hoversUnderMouse = _quadtree.Query(Camera.MouseWorld).OrderBy(e => e);
                    selectedAndHovered = _selectedEntities.Query(Camera.MouseWorld).OrderBy(e => e);
                }
                var hoverCount = hoversUnderMouse.Count();
                // Skip if there are no hovers.
                if (hoverCount > 0) {
                    int cycleReset = 0;
                    // If there's a selection, always give it priority over everything else.
                    if (selectedAndHovered.Count() > 0) {
                        cycleReset = hoverCount - 1 - hoversUnderMouse.ToList().IndexOf(selectedAndHovered.Last());
                        // If we aren't cycling, then select the last selection. The one that is on top of everything.
                        if (_cycleMouse == null) {
                            _cycleIndex = cycleReset;
                        }
                    }

                    // If we're current cycling over the hovers, we reset when the mouse moves enough.
                    if (_cycleMouse != null && Vector2.DistanceSquared(_cycleMouse.Value, Camera.MouseWorld) > Utility.ScreenArea(10)) {
                        _cycleIndex = cycleReset;
                        _cycleMouse = null;
                    }
                    // If we want to cycle over the hovers, save current mouse position so that we can reset later.
                    int scrollDelta = InputHelper.NewMouse.ScrollWheelValue - InputHelper.OldMouse.ScrollWheelValue;
                    if (scrollDelta != 0 && Triggers.SelectionCycle.Held()) {
                        _cycleIndex += MathF.Sign(scrollDelta);
                        _cycleMouse = Camera.MouseWorld;
                    }

                    // Now we can do the selection.
                    _hoveredEntity = hoversUnderMouse.ElementAt(Utility.Mod(hoverCount - 1 - _cycleIndex, hoverCount));
                }
            }
        }

        private void ApplySelection(bool shiftModifier, bool ctrlModifier) {
            if (!shiftModifier && !ctrlModifier) {
                Utility.ClearQuadtree(_selectedEntities);
            }
            if (ctrlModifier) {
                foreach (var e in GetHovers()) {
                    _selectedEntities.Remove(e);
                }
            } else {
                bool preserveOrder = _selectedEntities.Count() == 0;
                foreach (var e in GetHovers().OrderBy(e => e)) {
                    if (!_selectedEntities.Contains(e)) {
                        if (preserveOrder) {
                            e.NextOrder = e.Order;
                        } else {
                            e.NextOrder = ++_lastOrder;
                        }
                        _lastOrder = e.NextOrder;
                        _selectedEntities.Add(e);
                    }
                }
            }

            ComputedSelectionBounds();

            _selection.Rect = null;
        }
        private void ApplyEdit(bool isDone) {
            // If the edit rectangle isn't null, we have at least one selection.
            if (_edit.Rect != null) {
                using (IEnumerator<Entity> e = _selectedEntities.GetEnumerator()) {
                    e.MoveNext();
                    var first = e.Current;

                    Sidebar.Put();
                    var rect = _edit.Rect!.Value;
                    var newRect = rect;
                    string hoveredX = $"{rect.X}";
                    string hoveredY = $"{rect.Y}";
                    string hoveredWidth = $"{rect.Width}";
                    string hoveredHeight = $"{rect.Height}";
                    string type = $"{(int)first.Type}";
                    Label.Put("X");
                    Textbox.Put(ref hoveredX);
                    Label.Put("Y");
                    Textbox.Put(ref hoveredY);
                    if (_selectedEntities.Count() == 1) {
                        Label.Put("Width");
                        Textbox.Put(ref hoveredWidth);
                        Label.Put("Height");
                        Textbox.Put(ref hoveredHeight);
                        Label.Put("Type");
                        Textbox.Put(ref type);
                    }

                    if (float.TryParse(hoveredX, out float newX)) {
                        newRect.X = newX;
                    }
                    if (float.TryParse(hoveredY, out float newY)) {
                        newRect.Y = newY;
                    }
                    if (float.TryParse(hoveredWidth, out float newWidth)) {
                        if (newWidth > 0) {
                            newRect.Width = newWidth;
                        }
                    }
                    if (float.TryParse(hoveredHeight, out float newHeight)) {
                        if (newHeight > 0) {
                            newRect.Height = newHeight;
                        }
                    }
                    if (int.TryParse(type, out int newType)) {
                        if (newType >= 0) {
                            first.Type = (Tile.Type)newType;
                        }
                    }

                    if (rect.X != newRect.X || rect.Y != newRect.Y || rect.Width != newRect.Width || rect.Height != newRect.Height) {
                        _edit.Rect = new RectangleF(newRect.X, newRect.Y, newRect.Width, newRect.Height);

                        isDone = true;
                    }
                    Sidebar.Pop();

                    if (_editRectStartXY != (Vector2)_edit.Rect.Value.Position || _editRectStartSize != (Vector2)_edit.Rect.Value.Size) {
                        if (!isDone) {
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
                        } else {
                            _historyHandler.AutoCommit = false;
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

                            _editRectStartXY = _edit.Rect.Value.Position;
                            _editRectStartSize = _edit.Rect.Value.Size;
                        }
                    }
                }
            }
        }
        private void ComputedSelectionBounds() {
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
        }

        private void ResetOrder() {
            _historyHandler.AutoCommit = false;
            foreach (var e in _selectedEntities) {
                HistoryOrderEntity(e.Id, e.Order, e.NextOrder);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;

            _order = Math.Max(_order, _lastOrder + 1);
        }
        private void Remove() {
            _edit.Rect = null;
            _hoveredEntity = null;
            var all = _selectedEntities.ToArray();
            _historyHandler.AutoCommit = false;
            foreach (var e in all) {
                HistoryRemoveEntity(e.Id, new RectangleF(e.Bounds.XY, e.Bounds.Size), e.Order, e.Type);
                _selectedEntities.Remove(e);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;
        }
        private void Copy() {
            // TODO: Might be nice to compute the bounding rectangle. Could be useful for centering.
            // NOTE: We already have the bounding rectangle of the selection.
            if (_selectedEntities.Count() > 0) {
                _pasteBuffer.Clear();
                using (IEnumerator<Entity> e = _selectedEntities.OrderBy(e => e).GetEnumerator()) {
                    e.MoveNext();
                    var current = e.Current;
                    var pos1 = current.Bounds.XY;
                    _pasteBuffer.Enqueue(new EntityPaste(new RectangleF(0, 0, current.Bounds.Width, current.Bounds.Height), current.Type));

                    while (e.MoveNext()) {
                        current = e.Current;
                        var pos2 = current.Bounds.XY;

                        _pasteBuffer.Enqueue(new EntityPaste(new RectangleF(pos2 - pos1, new Vector2(current.Bounds.Width, current.Bounds.Height)), current.Type));
                    }
                }
            }
        }
        private void Cut() {
            Copy();
            Remove();
        }
        private void Create() {
            _shouldAddNewToHover = true;
            HistoryCreateEntity(GetNextId(), new RectangleF(Camera.MouseWorld, new Vector2(100, 100)), GetNextOrder(), Tile.Type.Red);
            _shouldAddNewToHover = false;
        }
        private void CreateStuff() {
            _historyHandler.AutoCommit = false;
            for (int i = 0; i < 10000; i++) {
                var screenBounds = Camera.WorldBounds;
                var origin = Camera.Origin;
                float minX = screenBounds.Left;
                float maxX = screenBounds.Right;
                float minY = screenBounds.Top;
                float maxY = screenBounds.Bottom;

                HistoryCreateEntity(GetNextId(), new RectangleF(new Vector2(_random.NextSingle(minX, maxX), _random.NextSingle(minY, maxY)) - origin, new Vector2(_random.NextSingle(50, 200), _random.NextSingle(50, 200))), GetNextOrder(), Tile.Type.Red);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;
        }
        private void Paste(Vector2 anchor) {
            _shouldAddNewToHover = true;
            _historyHandler.AutoCommit = false;
            foreach (var e in _pasteBuffer) {
                HistoryCreateEntity(GetNextId(), new RectangleF(anchor + e.Rect.Position, e.Rect.Size), GetNextOrder(), e.Type);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;
            _shouldAddNewToHover = false;
        }

        private IEnumerable<Entity> GetHovers(bool withinCamera = false) {
            if (_newEntitiesHover.Count > 0) {
                while (_newEntitiesHover.Count > 0) {
                    yield return _entities[_newEntitiesHover.Pop()];
                }
            } else if (_selection.Rect != null) {
                if (!withinCamera) {
                    var r = _selection.Rect.Value;
                    foreach (var e in _quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height)))
                        yield return e;
                } else {
                    var origin = Camera.Origin;
                    var worldBounds = new RectangleF(Camera.WorldBounds.Location.ToVector2() - origin, Camera.WorldBounds.Size);
                    var r = _selection.Rect.Value.Intersection(worldBounds);
                    foreach (var e in _quadtree.Query(new RotRect(r.X, r.Y, r.Width, r.Height)))
                        yield return e;
                }
            } else if (_hoveredEntity != null) {
                yield return _hoveredEntity;
            }
            yield break;
        }

        World _world;
        Quadtree<Entity> _quadtree;
        Dictionary<uint, Entity> _entities;

        Random _random = new Random();

        IMGUI _ui = null!;

        uint _lastId = 0;
        uint _order = 0;
        int _cycleIndex = 0;
        Vector2? _cycleMouse = Vector2.Zero;

        RectEdit _selection = null!;
        RectEdit _edit = null!;
        Vector2 _editRectStartXY = Vector2.Zero;
        Vector2 _editRectStartSize = Vector2.Zero;
        bool _shouldAddNewToHover = false;
        Stack<uint> _newEntitiesHover = new Stack<uint>();

        HistoryHandler _historyHandler = null!;
        Entity? _hoveredEntity;
        Quadtree<Entity> _selectedEntities = null!;
        Queue<EntityPaste> _pasteBuffer = new Queue<EntityPaste>();
        uint _lastOrder = 0;
    }
}
