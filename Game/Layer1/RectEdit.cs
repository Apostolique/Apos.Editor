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
                _dragHandle = DragHandle.None;
            }
        }
        public bool IsResizable { get; set; } = true;
        public float HandleDistanceWorld => _handleDistance * Camera.ScreenToWorldScale;
        public bool IsDragged => _dragHandle != DragHandle.None;

        public bool UpdateInput(Vector2 mouseWorld, bool canCreate = true, Vector2? grid = null) {
            // Use the current rectangle or create a new one.
            RectangleF r = _proxyRect ?? new RectangleF(mouseWorld.X, mouseWorld.Y, 0, 0);
            bool shouldCreate = false;

            float left = r.Left;
            float right = r.Right;
            float top = r.Top;
            float bottom = r.Bottom;

            // Drag start
            if (Triggers.RectDrag.Pressed(false)) {
                if (!_validProxy) {
                    shouldCreate = canCreate;

                    if (!shouldCreate) return false;
                }

                float dx = 0;
                float dy = 0;

                // Move rectangle
                if (r.Contains(mouseWorld)) {
                    dx = left - mouseWorld.X;
                    dy = top - mouseWorld.Y;
                    _dragHandle = DragHandle.Center;
                } else if (IsResizable) {
                    // Resize rectangle
                    if (top - HandleDistanceWorld <= mouseWorld.Y && mouseWorld.Y <= bottom + HandleDistanceWorld) {
                        float dLeft = left - mouseWorld.X;
                        float dRight = mouseWorld.X - right;

                        if (0 <= dLeft && dLeft <= HandleDistanceWorld) {
                            _dragHandle |= DragHandle.Left;
                            dx = left - mouseWorld.X;
                        } else if (0 <= dRight && dRight <= HandleDistanceWorld) {
                            _dragHandle |= DragHandle.Right;
                            dx = right - mouseWorld.X;
                        }
                    }
                    // Resize rectangle
                    if (left - HandleDistanceWorld <= mouseWorld.X && mouseWorld.X <= right + HandleDistanceWorld) {
                        float dTop = top - mouseWorld.Y;
                        float dBottom = mouseWorld.Y - bottom;

                        if (0 <= dTop && dTop <= HandleDistanceWorld) {
                            _dragHandle |= DragHandle.Top;
                            dy = top - mouseWorld.Y;
                        } else if (0 <= dBottom && dBottom <= HandleDistanceWorld) {
                            _dragHandle |= DragHandle.Bottom;
                            dy = bottom - mouseWorld.Y;
                        }
                    }

                    // We're outside the rectangle, create a new one.
                    if (canCreate && _dragHandle == DragHandle.None) {
                        r = new RectangleF(mouseWorld.X, mouseWorld.Y, 0, 0);
                        left = r.Left;
                        right = r.Right;
                        top = r.Top;
                        bottom = r.Bottom;

                        _dragHandle = DragHandle.Left | DragHandle.Top;
                        dx = left - mouseWorld.X;
                        dy = top - mouseWorld.Y;
                    }
                }

                if (IsDragged) {
                    _dragDistance = new Vector2(dx, dy);
                    Triggers.RectDrag.Consume();
                }
            }
            // Drag ongoing
            if (IsDragged && Triggers.RectDrag.HeldOnly()) {
                // Move rectangle
                if (_dragHandle.HasFlag(DragHandle.Center)) {
                    r.X = mouseWorld.X + _dragDistance.X;
                    r.Y = mouseWorld.Y + _dragDistance.Y;
                } else if (IsResizable) {
                    // Resize rectangle
                    float width = r.Width;
                    float height = r.Height;

                    // Do a first pass in case we're dragging a side and crossing over causing negative width or height.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        width = right - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        width = mouseWorld.X + _dragDistance.X - left;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        height = bottom - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        height = mouseWorld.Y + _dragDistance.Y - top;
                    }

                    // If we cross, preserve the static side.
                    if (width < 0) {
                        _dragHandle ^= DragHandle.Left | DragHandle.Right;
                        float temp = left;
                        left = right;
                        right = temp;
                    }
                    if (height < 0) {
                        _dragHandle ^= DragHandle.Top | DragHandle.Bottom;
                        float temp = top;
                        top = bottom;
                        bottom = temp;
                    }

                    // Compute the final rectangle.
                    if (_dragHandle.HasFlag(DragHandle.Left)) {
                        r.X = mouseWorld.X + _dragDistance.X;
                        r.Width = right - r.X;
                    } else if (_dragHandle.HasFlag(DragHandle.Right)) {
                        r.X = left;
                        r.Width = mouseWorld.X + _dragDistance.X - left;
                    }
                    if (_dragHandle.HasFlag(DragHandle.Top)) {
                        r.Y = mouseWorld.Y + _dragDistance.Y;
                        r.Height = bottom - r.Y;
                    } else if (_dragHandle.HasFlag(DragHandle.Bottom)) {
                        r.Y = top;
                        r.Height = mouseWorld.Y + _dragDistance.Y - top;
                    }
                }
            }

            if (IsDragged && (_proxyRect != null || shouldCreate)) {
                _proxyRect = r;

                if (r.Width * r.Height >= Utility.ScreenArea(10)) {
                    _validProxy = true;
                }
                if (_validProxy) {
                    // TODO: Add a bias when resizing. We shouldn't snap to both sides.
                    float newLeft = _proxyRect.Value.X;
                    float newTop = _proxyRect.Value.Y;
                    float newRight = _proxyRect.Value.X + _proxyRect.Value.Width;
                    float newBottom = _proxyRect.Value.Y + _proxyRect.Value.Height;

                    float snapLeft = SnapX(newLeft, grid);
                    float snapTop = SnapY(newTop, grid);
                    float snapRight = SnapX(newRight, grid);
                    float snapBottom = SnapY(newBottom, grid);

                    float diffLeft = snapLeft - newLeft;
                    float diffTop = snapTop - newTop;
                    float diffRight = snapRight - newRight;
                    float diffBottom = snapBottom - newBottom;

                    float finalLeft = newLeft;
                    float finalTop = newTop;
                    float finalRight = newRight;
                    float finalBottom = newBottom;

                    if (MathF.Abs(diffLeft) < MathF.Abs(diffRight)) {
                        finalLeft = snapLeft;
                        finalRight += diffLeft;
                    } else {
                        finalRight = snapRight;
                        finalLeft += diffRight;
                    }
                    if (MathF.Abs(diffTop) < MathF.Abs(diffBottom)) {
                        finalTop = snapTop;
                        finalBottom += diffTop;
                    } else {
                        finalBottom = snapBottom;
                        finalTop += diffBottom;
                    }

                    _rect = new RectangleF(finalLeft, finalTop, finalRight - finalLeft, finalBottom - finalTop);
                } else {
                    _rect = null;
                }
            }

            // Drag end
            if (IsDragged && Triggers.RectDrag.Released()) {
                _dragHandle = DragHandle.None;
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch s) {
            if (Rect != null) {
                if (IsResizable) {
                    s.FillRectangle(Utility.ExpandRect(Rect.Value, HandleDistanceWorld), Color.White * 0.1f);
                    s.DrawRectangle(Utility.ExpandRect(Rect.Value, HandleDistanceWorld), Color.White * 0.1f, Camera.ScreenToWorldScale);
                }
                s.FillRectangle(Rect.Value, Color.White * 0.2f);
                s.DrawRectangle(Rect.Value, Color.White * 0.2f, Camera.ScreenToWorldScale);
            }
        }

        private float SnapX(float value, Vector2? grid) {
            if (grid != null) return MathF.Floor(value / grid.Value.X + 0.5f) * grid.Value.X;
            return value;
        }
        private float SnapY(float value, Vector2? grid) {
            if (grid != null) return MathF.Floor(value / grid.Value.Y + 0.5f) * grid.Value.Y;
            return value;
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
        Vector2 _dragDistance = new Vector2();

        RectangleF? _rect;
        public RectangleF? _proxyRect;
        bool _validProxy = false;
    }
}
