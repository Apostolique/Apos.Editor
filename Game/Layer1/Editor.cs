using System;
using System.Collections.Generic;
using System.Linq;
using Apos.Gui;
using Apos.History;
using Apos.Input;
using Apos.Spatial;
using GameProject.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class Editor {
        public Editor(World world, GraphicsDevice graphicsDevice) {
            _world = world;
            _aabbTree = _world.AABBTree;
            _entities = _world.Entities;

            _historyHandler = new HistoryHandler(null);
            _selectedEntities = new AABBTree<Entity>();

            _selection = new RectEdit();
            _edit = new RectEdit();

            _ui = new IMGUI();

            _pathEditor = new PathEditor(_aabbTree);

            WindowResize(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        }
        public void WindowResize(int width, int height) {
            _projection = Matrix.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        }

        public void Update(GameTime gameTime) {
            GuiHelper.CurrentIMGUI = _ui;
            _ui.UpdateAll(gameTime);

            if (Triggers.RectDrag.Pressed(false)) {
                _ui.GrabFocus(null);
            }

            Sidebar.Put(isLeftSide: true);
            string gridSizeString = $"{_gridSize}";
            string gridLockString = $"{(_gridLock ? 1 : 0)}";
            Label.Put("");
            Label.Put("Lock Grid");
            Textbox.Put(ref gridLockString);
            if (Int32.TryParse(gridLockString, out int newGridLock)) {
                _gridLock = newGridLock == 0 ? false : true;
            }
            Label.Put("Grid Size");
            if (_gridLock) {
                Label.Put(gridSizeString);
            } else {
                Textbox.Put(ref gridSizeString);
                if (float.TryParse(gridSizeString, out float newGridSize)) {
                    _gridSize = newGridSize;
                    _gridWorld = _gridSize * Camera.ScreenToWorldScale;
                    _adaptiveGrid = MathF.Max(GetAdaptiveGrid(_gridSize, _gridWorld), _gridSize);
                }
            }
            Label.Put("Adaptive Size");
            Label.Put($"{_adaptiveGrid}");
            Sidebar.Pop();

            bool addModifier = false;
            bool removeModifier = false;
            bool skipEditModifier = false;

            if (!_edit.IsDragged) {
                addModifier = Triggers.AddModifier.Held();
                removeModifier = Triggers.RemoveModifier.Held();
                skipEditModifier = Triggers.SkipEditModifier.Held();
            }

            Camera.UpdateInput();
            Camera.Update();

            if (Triggers.Redo.Pressed()) {
                _historyHandler.Redo();
                ComputedSelectionBounds();
            }
            if (Triggers.Undo.Pressed()) {
                _historyHandler.Undo();
                ComputedSelectionBounds();
            }

            bool isEditDone = false;
            if (!addModifier && !removeModifier && !skipEditModifier) {
                if (Triggers.AlignToGrid.Held()) {
                    isEditDone = _edit.UpdateInput(Camera.MouseWorld, false, grid: new Vector2(_adaptiveGrid));
                } else {
                    isEditDone = _edit.UpdateInput(Camera.MouseWorld, false);
                }
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
                ApplySelection(addModifier, removeModifier);
            }

            if (Triggers.ResetOrder.Pressed()) {
                ResetOrder();
            }

            if (Triggers.ResetResize.Pressed()) {
                ResetResize();
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

            _pathEditor.UpdateInput();
        }
        public void DrawBackground(SpriteBatch s) {
            DrawGrid(s, _adaptiveGrid, _gridSize, new Color(60, 60, 60));
        }
        public void Draw(SpriteBatch s) {
            _pathEditor.Draw(s);

            _selection.Draw(s);
            // if (_selectedEntities.Count() == 1) {
                _edit.Draw(s);
            // }

            foreach (var e in _selectedEntities.Query(Camera.ViewRect))
                e.DrawHighlight(s, 0f, 2f, Color.White);
            foreach (var e in GetHovers(true))
                e.DrawHighlight(s, -2f, 3f, Color.Black);
        }
        public void DrawUI(SpriteBatch s, GameTime gameTime) {
            _ui.Draw(gameTime);
        }

        private void DrawGrid(SpriteBatch s, float gridSize, float minGrid, Color color) {
            Assets.Grid.Parameters["view_projection"].SetValue(Matrix.Identity *  _projection);
            Assets.Grid.Parameters["tex_transform"].SetValue(Matrix.Invert(Camera.View));

            float screenToWorld = Camera.ScreenToWorldScale;

            Assets.Grid.Parameters["line_size"].SetValue(new Vector2(1f * screenToWorld));
            float smallerGrid = gridSize / 2f;
            while (smallerGrid >= 8f * screenToWorld && smallerGrid >= minGrid) {
                Assets.Grid.Parameters["grid_size"].SetValue(new Vector2(smallerGrid));
                s.Begin(effect: Assets.Grid, samplerState: SamplerState.LinearWrap);
                s.Draw(Assets.Pixel, Vector2.Zero, s.GraphicsDevice.Viewport.Bounds, color * 0.2f);
                s.End();
                smallerGrid /= 2f;
            }

            Assets.Grid.Parameters["line_size"].SetValue(new Vector2(1f * screenToWorld));
            Assets.Grid.Parameters["grid_size"].SetValue(new Vector2(gridSize));
            s.Begin(effect: Assets.Grid, samplerState: SamplerState.LinearWrap);
            s.Draw(Assets.Pixel, Vector2.Zero, s.GraphicsDevice.Viewport.Bounds, color);
            s.End();
        }

        private float GetAdaptiveGrid(float gridSize, float gridWorld) {
            return gridSize * MathF.Pow(2, MathF.Ceiling(MathF.Log2(gridWorld / gridSize)));
        }

        private uint GrabNextId() {
            return _lastId++;
        }
        private uint GrabNextOrder() {
            return _order++;
        }
        private void HistoryCreateEntity(uint id, RectangleF r, uint order, int type) {
            _historyHandler.Add(() => {
                RemoveEntity(id);
            }, () => {
                CreateEntity(id, r, order, type);
            });
        }
        private void HistoryRemoveEntity(uint id, RectangleF r, uint order, int type) {
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
        private void HistoryTypeEntity(uint id, int oldType, int newType) {
            _historyHandler.Add(() => {
                TypeEntity(id, oldType);
            }, () => {
                TypeEntity(id, newType);
            });
        }
        private void CreateEntity(uint id, RectangleF r, uint order, int type) {
            Entity e = new Entity(id, r, order, type);
            e.Leaf1 = _aabbTree.Add(e.Rect, e);
            _entities.Add(e.Id, e);
            if (_shouldAddNewToHover) _newEntitiesHover.Push(e.Id);

            _pathEditor.AddToPath((Rectangle)e.Inset, e.IsNegative);
        }
        private void RemoveEntity(uint id) {
            Entity e = _entities[id];
            e.Leaf1 = _aabbTree.Remove(e.Leaf1);
            _entities.Remove(e.Id);
            e.Leaf2 = _selectedEntities.Remove(e.Leaf2);

            _pathEditor.RemoveFromPath((Rectangle)e.Inset);
        }
        private void MoveEntity(uint id, Vector2 xy) {
            Entity e = _entities[id];

            RectangleF oldInset = e.Inset;

            var inset = e.Inset;
            inset.Position = xy;
            e.Inset = inset;
            _aabbTree.Update(e.Leaf1, e.Rect);
            if (e.Leaf2 != -1) _selectedEntities.Update(e.Leaf2, e.Rect);

            _pathEditor.UpdatePath((Rectangle)oldInset, (Rectangle)e.Inset, e.IsNegative);
        }
        private void ResizeEntity(uint id, Vector2 size) {
            Entity e = _entities[id];

            RectangleF oldInset = e.Inset;

            var inset = e.Inset;
            inset.Size = size;
            e.Inset = inset;
            _aabbTree.Update(e.Leaf1, e.Rect);
            if (e.Leaf2 != -1) _selectedEntities.Update(e.Leaf2, e.Rect);

            _pathEditor.UpdatePath((Rectangle)oldInset, (Rectangle)e.Inset, e.IsNegative);
        }
        private void OrderEntity(uint id, uint order) {
            Entity e = _entities[id];
            e.Order = order;
        }
        private void TypeEntity(uint id, int type) {
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
                    var inset = first.Inset;
                    addSelected = !inset.Contains(Camera.MouseWorld) && Utility.ExpandRect(new RectangleF(inset.X, inset.Y, inset.Width, inset.Height), _edit.HandleDistanceWorld).Contains(Camera.MouseWorld);
                }

                List<Entity> hoversUnderMouse;
                List<Entity> selectedAndHovered;
                if (addSelected) {
                    hoversUnderMouse = _aabbTree.Query(Camera.MouseWorld).Append(first!).OrderBy(e => e).ToList();
                    // This can only happen when there's only 1 selection so we can do a special case here.
                    selectedAndHovered = new List<Entity>{ first! };
                } else {
                    hoversUnderMouse = _aabbTree.Query(Camera.MouseWorld).OrderBy(e => e).ToList();
                    selectedAndHovered = _selectedEntities.Query(Camera.MouseWorld).OrderBy(e => e).ToList();
                }

                var hoverCount = hoversUnderMouse.Count();
                // Skip if there are no hovers.
                if (hoverCount > 0) {
                    int cycleReset = 0;
                    // If there's a selection, always give it priority over everything else.
                    if (selectedAndHovered.Count() > 0) {
                        cycleReset = hoverCount - 1 - hoversUnderMouse.IndexOf(selectedAndHovered.Last());
                    }

                    // If we aren't cycling, then select the selection at the top or element at the top.
                    if (_cycleMouse == null) {
                        _cycleIndex = cycleReset;
                    }
                    // If we're current cycling over the hovers, we reset when the mouse moves enough.
                    else if (Vector2.DistanceSquared(_cycleMouse.Value, Camera.MouseWorld) > Utility.ScreenArea(10)) {
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
                } else {
                    _cycleIndex = 0;
                    _cycleMouse = null;
                }
            }
        }

        private void ApplySelection(bool addModifier, bool removeModifier) {
            if (!addModifier && !removeModifier) {
                foreach (var e in _selectedEntities) e.Leaf2 = -1;
                _selectedEntities.Clear();
            }
            if (removeModifier) {
                foreach (var e in GetHovers()) {
                    e.Leaf2 = _selectedEntities.Remove(e.Leaf2);
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
                        e.Leaf2 = _selectedEntities.Add(e.Rect, e);
                    }
                }
            }

            _cycleIndex = 0;
            _cycleMouse = null;

            ComputedSelectionBounds();

            _selection.Rect = null;
        }
        private void ApplyEdit(bool isDone) {
            // If the edit rectangle isn't null, we have at least one selection.
            if (_edit.Rect != null) {
                using (IEnumerator<Entity> e = _selectedEntities.ToList().GetEnumerator()) {
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
                        if (newType != first.Type) {
                            HistoryTypeEntity(first.Id, first.Type, newType);
                        }
                    }

                    if (rect.X != newRect.X || rect.Y != newRect.Y || rect.Width != newRect.Width || rect.Height != newRect.Height) {
                        _edit.Rect = new RectangleF(newRect.X, newRect.Y, newRect.Width, newRect.Height);

                        isDone = true;
                    }
                    Sidebar.Pop();

                    if (!isDone) {
                        if (_editRectStartXY != (Vector2)_edit.Rect.Value.Position || _editRectStartSize != (Vector2)_edit.Rect.Value.Size) {
                            _editRectStartXY = _edit.Rect.Value.Position;
                            _editRectStartSize = _edit.Rect.Value.Size;
                            Vector2 offset = _editAnchor + _edit.Rect.Value.Position;

                            if (!isDone) {
                                RectangleF oldInset = first.Inset;

                                var inset = first.Inset;
                                inset.Position = first.Offset + offset;
                                first.Inset = inset;
                                if (_selectedEntities.Count() == 1) {
                                    inset.Size = _edit.Rect.Value.Size;
                                    first.Inset = inset;
                                }
                                _aabbTree.Update(first.Leaf1, first.Rect);
                                if (first.Leaf2 != -1) _selectedEntities.Update(first.Leaf2, first.Rect);

                                _pathEditor.UpdatePath((Rectangle)oldInset, (Rectangle)first.Inset, first.IsNegative);

                                while (e.MoveNext()) {
                                    var current = e.Current;
                                    oldInset = current.Inset;

                                    inset = current.Inset;
                                    inset.Position = current.Offset + offset;
                                    current.Inset = inset;
                                    _aabbTree.Update(current.Leaf1, current.Rect);
                                    if (current.Leaf2 != -1) _selectedEntities.Update(current.Leaf2, current.Rect);

                                    _pathEditor.UpdatePath((Rectangle)oldInset, (Rectangle)current.Inset, current.IsNegative);
                                }
                            }
                        }
                    } else if (_editRectInitialStartXY != (Vector2)_edit.Rect.Value.Position || _editRectInitialStartSize != (Vector2)_edit.Rect.Value.Size) {
                        _editRectStartXY = _edit.Rect.Value.Position;
                        _editRectStartSize = _edit.Rect.Value.Size;
                        Vector2 oldOffset = _editAnchor + _editRectInitialStartXY;
                        Vector2 newOffset = _editAnchor + _edit.Rect.Value.Position;

                        _historyHandler.AutoCommit = false;
                        HistoryMoveEntity(first.Id, first.Offset + oldOffset, first.Offset + newOffset);

                        if (_selectedEntities.Count() == 1) {
                            HistoryResizeEntity(first.Id, _editRectInitialStartSize, _edit.Rect.Value.Size);
                        }

                        while (e.MoveNext()) {
                            var current = e.Current;
                            HistoryMoveEntity(current.Id, current.Offset + oldOffset, current.Offset + newOffset);
                        }
                        _historyHandler.Commit();
                        _historyHandler.AutoCommit = true;

                        _editRectInitialStartXY = _edit.Rect.Value.Position;
                        _editRectInitialStartSize = _edit.Rect.Value.Size;
                    }
                }
            }
        }
        private void ComputedSelectionBounds() {
            if (_selectedEntities.Count() >= 1) {
                using (IEnumerator<Entity> e = _selectedEntities.GetEnumerator()) {
                    e.MoveNext();
                    var first = e.Current;

                    float x1 = first.Inset.X;
                    float x2 = first.Inset.X + first.Inset.Width;
                    float y1 = first.Inset.Y;
                    float y2 = first.Inset.Y + first.Inset.Height;

                    while (e.MoveNext()) {
                        var current = e.Current;
                        x1 = MathF.Min(current.Inset.X, x1);
                        x2 = MathF.Max(current.Inset.X + current.Inset.Width, x2);
                        y1 = MathF.Min(current.Inset.Y, y1);
                        y2 = MathF.Max(current.Inset.Y + current.Inset.Height, y2);

                        current.Offset = current.Inset.Position;
                    }

                    _edit.IsResizable = _selectedEntities.Count() == 1;
                    _editRectStartXY = new Vector2(x1, y1);
                    _editRectStartSize = new Vector2(x2 - x1, y2 - y1);
                    _edit.Rect = new RectangleF(_editRectStartXY, _editRectStartSize);
                    first.Offset = first.Inset.Position;
                    _editAnchor = -_editRectStartXY;

                    _editRectInitialStartXY = _editRectStartXY;
                    _editRectInitialStartSize = _editRectStartSize;
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

        private void ResetResize() {
            _historyHandler.AutoCommit = false;
            foreach (var e in _selectedEntities) {
                var bleeder = Assets.Bleeders[e.Type];
                HistoryResizeEntity(e.Id, e.Inset.Size, bleeder.Source.Size.ToVector2() * bleeder.Inset.Size);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;

            ComputedSelectionBounds();
        }
        private void Remove() {
            _edit.Rect = null;
            _hoveredEntity = null;
            var all = _selectedEntities.ToArray();
            _historyHandler.AutoCommit = false;
            foreach (var e in all) {
                HistoryRemoveEntity(e.Id, new RectangleF(e.Inset.Position, e.Inset.Size), e.Order, e.Type);
                e.Leaf2 = _selectedEntities.Remove(e.Leaf2);
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
                    var pos1 = current.Inset.Position;
                    _pasteBuffer.Enqueue(new EntityPaste(new RectangleF(0, 0, current.Inset.Width, current.Inset.Height), current.Type));

                    while (e.MoveNext()) {
                        current = e.Current;
                        var pos2 = current.Inset.Position;

                        _pasteBuffer.Enqueue(new EntityPaste(new RectangleF(pos2 - pos1, new Vector2(current.Inset.Width, current.Inset.Height)), current.Type));
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
            // FIXME: This can crash if there are no "bleeders".
            var bleeder = Assets.Bleeders[0];
            HistoryCreateEntity(GrabNextId(), new RectangleF(Camera.MouseWorld, bleeder.Source.Size.ToVector2() * bleeder.Inset.Size), GrabNextOrder(), 0);
            _shouldAddNewToHover = false;
        }
        private void CreateStuff() {
            _historyHandler.AutoCommit = false;
            for (int i = 0; i < 10000; i++) {
                var screenBounds = Camera.ViewRect;
                var origin = Camera.Origin;
                float minX = screenBounds.Left;
                float maxX = screenBounds.Right;
                float minY = screenBounds.Top;
                float maxY = screenBounds.Bottom;

                // FIXME: This can crash if there are no "bleeders".
                HistoryCreateEntity(GrabNextId(), new RectangleF(new Vector2(_random.NextSingle(minX, maxX), _random.NextSingle(minY, maxY)) - origin, new Vector2(_random.NextSingle(50, 200), _random.NextSingle(50, 200))), GrabNextOrder(), 0);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;
        }
        private void Paste(Vector2 anchor) {
            _shouldAddNewToHover = true;
            _historyHandler.AutoCommit = false;
            foreach (var e in _pasteBuffer) {
                HistoryCreateEntity(GrabNextId(), new RectangleF(anchor + e.Rect.Position, e.Rect.Size), GrabNextOrder(), e.Type);
            }
            _historyHandler.Commit();
            _historyHandler.AutoCommit = true;
            _shouldAddNewToHover = false;
        }

        private IEnumerable<Entity> GetHovers(bool withinCamera = false) {
            if (_edit.IsDragged) {
                yield break;
            } else if (_newEntitiesHover.Count > 0) {
                while (_newEntitiesHover.Count > 0) {
                    yield return _entities[_newEntitiesHover.Pop()];
                }
            } else if (_selection.Rect != null) {
                if (!withinCamera) {
                    var r = _selection.Rect.Value;
                    foreach (var e in _aabbTree.Query(new RectangleF(r.X, r.Y, r.Width, r.Height)))
                        yield return e;
                } else {
                    var r = _selection.Rect.Value.Intersection(Camera.ViewRect);
                    foreach (var e in _aabbTree.Query(new RectangleF(r.X, r.Y, r.Width, r.Height)))
                        yield return e;
                }
            } else if (_hoveredEntity != null) {
                yield return _hoveredEntity;
            }
            yield break;
        }

        World _world;
        AABBTree<Entity> _aabbTree;
        Dictionary<uint, Entity> _entities;

        PathEditor _pathEditor;

        Random _random = new Random();

        IMGUI _ui = null!;

        uint _lastId = 0;
        uint _order = 0;
        int _cycleIndex = 0;
        Vector2? _cycleMouse = Vector2.Zero;

        RectEdit _selection = null!;
        RectEdit _edit = null!;
        Vector2 _editAnchor = Vector2.Zero;
        Vector2 _editRectInitialStartXY = Vector2.Zero;
        Vector2 _editRectInitialStartSize = Vector2.Zero;
        Vector2 _editRectStartXY = Vector2.Zero;
        Vector2 _editRectStartSize = Vector2.Zero;
        bool _shouldAddNewToHover = false;
        Stack<uint> _newEntitiesHover = new Stack<uint>();

        HistoryHandler _historyHandler = null!;
        Entity? _hoveredEntity;
        AABBTree<Entity> _selectedEntities = null!;
        Queue<EntityPaste> _pasteBuffer = new Queue<EntityPaste>();
        uint _lastOrder = 0;

        bool _gridLock = false;
        float _gridSize = 100f;
        float _adaptiveGrid = 100f;
        float _gridWorld = 100f;
        Matrix _projection;
    }
}
