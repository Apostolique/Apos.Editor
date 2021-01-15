using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public class RectEdit {
        public RectEdit() { }

        public RectangleF? Rect {
            get => _rect;
            set {
                _rect = value;
                _proxyRect = value;

                _validProxy = _proxyRect != null;
            }
        }
        public bool IsResizable {
            get;
            set;
        } = true;

        public bool UpdateInput(Vector2 mouseWorld, bool canCreate = true) {
            // TODO: Might be nice to do a minimum selection size threshold for it's area. (width * height > 900)

            // Use the current selection or create a new one.
            RectangleF r = _proxyRect ?? new RectangleF(mouseWorld.X, mouseWorld.Y, 0, 0);
            bool shouldCreate = false;

            float x1 = r.Left;
            float x2 = r.Right;
            float y1 = r.Top;
            float y2 = r.Bottom;

            // Drag start
            if (Triggers.SelectionDrag.Pressed(false)) {
                if (!_validProxy) {
                    shouldCreate = canCreate;

                    if (!shouldCreate) return false;
                }

                float dx = 0;
                float dy = 0;

                // Move selection
                if (r.Contains(mouseWorld)) {
                    dx = x1 - mouseWorld.X;
                    dy = y1 - mouseWorld.Y;
                    _dragHandle = DragHandle.Center;
                } else if (IsResizable) {
                    // Resize selection
                    if (y1 - _handleDistanceWorld <= mouseWorld.Y && mouseWorld.Y <= y2 + _handleDistanceWorld) {
                        float diff1 = x1 - mouseWorld.X;
                        float diff2 = mouseWorld.X - x2;

                        if (0 <= diff1 && diff1 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Left;
                            dx = x1 - mouseWorld.X;
                        } else if (0 <= diff2 && diff2 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Right;
                            dx = x2 - mouseWorld.X;
                        }
                    }
                    // Resize selection
                    if (x1 - _handleDistanceWorld <= mouseWorld.X && mouseWorld.X <= x2 + _handleDistanceWorld) {
                        float diff1 = y1 - mouseWorld.Y;
                        float diff2 = mouseWorld.Y - y2;

                        if (0 <= diff1 && diff1 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Top;
                            dy = y1 - mouseWorld.Y;
                        } else if (0 <= diff2 && diff2 <= _handleDistanceWorld) {
                            _dragHandle |= DragHandle.Bottom;
                            dy = y2 - mouseWorld.Y;
                        }
                    }

                    // We're outside the selection, create a new one.
                    if (canCreate && _dragHandle == DragHandle.None) {
                        r = new RectangleF(mouseWorld.X, mouseWorld.Y, 0, 0);
                        x1 = r.Left;
                        x2 = r.Right;
                        y1 = r.Top;
                        y2 = r.Bottom;

                        _dragHandle = DragHandle.Left | DragHandle.Top;
                        dx = x1 - mouseWorld.X;
                        dy = y1 - mouseWorld.Y;
                    }
                }

                if (_dragHandle != DragHandle.None) {
                    _dragDiff = new Vector2(dx, dy);
                    Triggers.SelectionDrag.Consume();
                }
            }
            // Drag ongoing
            if (_dragHandle != DragHandle.None && Triggers.SelectionDrag.HeldOnly()) {
                // Move selection
                if (_dragHandle.HasFlag(DragHandle.Center)) {
                    r.X = mouseWorld.X + _dragDiff.X;
                    r.Y = mouseWorld.Y + _dragDiff.Y;
                } else if (IsResizable) {
                    // Resize selection
                    float width = r.Width;
                    float height = r.Height;

                    // Do a first pass in case we're dragging a side and crossing over causing negative width or height.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        float left = mouseWorld.X + _dragDiff.X;
                        width = x2 - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        width = mouseWorld.X + _dragDiff.X - x1;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        float right = mouseWorld.Y + _dragDiff.Y;
                        height = y2 - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        height = mouseWorld.Y + _dragDiff.Y - y1;
                    }

                    // If we cross, preserve the static side.
                    if (width < 0) {
                        _dragHandle ^= DragHandle.Left | DragHandle.Right;
                        float temp = x1;
                        x1 = x2;
                        x2 = temp;
                    }
                    if (height < 0) {
                        _dragHandle ^= DragHandle.Top | DragHandle.Bottom;
                        float temp = y1;
                        y1 = y2;
                        y2 = temp;
                    }

                    // Compute the final rectangle.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        r.X = mouseWorld.X + _dragDiff.X;
                        r.Width = x2 - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        r.X = x1;
                        r.Width = mouseWorld.X + _dragDiff.X - x1;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        r.Y = mouseWorld.Y + _dragDiff.Y;
                        r.Height = y2 - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        r.Y = y1;
                        r.Height = mouseWorld.Y + _dragDiff.Y - y1;
                    }
                }
            }

            if (_dragHandle != DragHandle.None && (_proxyRect != null || shouldCreate)) {
                _proxyRect = r;

                if (r.Width * r.Height >= MathF.Pow(10 * Camera.ScreenToWorldScale, 2)) {
                    _validProxy = true;
                }
                if (_validProxy) {
                    _rect = _proxyRect;
                } else {
                    _rect = null;
                }
            }

            // Drag end
            if (_dragHandle != DragHandle.None && Triggers.SelectionDrag.Released()) {
                _dragHandle = DragHandle.None;
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch s) {
            if (Rect != null) {
                if (IsResizable) {
                    s.FillRectangle(Utility.ExpandRect(Rect.Value, _handleDistanceWorld), Color.White * 0.1f);
                    s.DrawRectangle(Utility.ExpandRect(Rect.Value, _handleDistanceWorld), Color.White * 0.1f, Camera.ScreenToWorldScale);
                }
                s.FillRectangle(Rect.Value, Color.White * 0.2f);
                s.DrawRectangle(Rect.Value, Color.White * 0.2f, Camera.ScreenToWorldScale);
            }
        }

        [Flags]
        enum DragHandle {
            None = 0,
            Left = 1 << 0,
            Top = 1 << 1,
            Right = 1 << 2,
            Bottom = 1 << 3,
            Center = 1 << 4,
        }
        DragHandle _dragHandle = DragHandle.None;
        float _handleDistance = 50f;
        float _handleDistanceWorld {
            get => _handleDistance * Camera.ScreenToWorldScale;
        }
        Vector2 _dragDiff = new Vector2();

        RectangleF? _rect;
        RectangleF? _proxyRect;
        bool _validProxy = false;
    }
}
