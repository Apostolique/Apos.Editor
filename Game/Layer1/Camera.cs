using System;
using Apos.Input;
using Dcrew.Camera;
using Microsoft.Xna.Framework;

namespace GameProject {
    public static class Camera {
        public static void Setup() {
            _camera = new Dcrew.Camera.Camera(new Vector2(0f, 0f), 0f, new Vector2(1f));
        }

        public static Vector2 MouseWorld = Vector2.Zero;
        public static Matrix View => _camera.View();
        public static Rectangle WorldBounds => _camera.WorldBounds();
        public static float Angle => _camera.Angle;
        public static Vector2 Origin => _camera.Origin;
        public static float ScreenToWorldScale => 1f / _camera.ScreenToWorldScale();

        public static void UpdateInput() {
            int scrollDelta = InputHelper.NewMouse.ScrollWheelValue - InputHelper.OldMouse.ScrollWheelValue;
            if (scrollDelta != 0 && !Triggers.SelectionCycle.Held(false)) {
                Zoom = MathF.Min(MathF.Max(Zoom - scrollDelta * 0.001f, 0.2f), 4f);
            }

            if (Triggers.RotateLeft.Pressed()) {
                _camera.Angle += MathHelper.PiOver4;
            }
            if (Triggers.RotateRight.Pressed()) {
                _camera.Angle -= MathHelper.PiOver4;
            }

            MouseWorld = _camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

            if (Triggers.CameraDrag.Pressed()) {
                _dragAnchor = MouseWorld;
                _isDragging = true;
            }
            if (_isDragging && Triggers.CameraDrag.HeldOnly()) {
                _camera.XY += _dragAnchor - MouseWorld;
                MouseWorld = _dragAnchor;
            }
            if (_isDragging && Triggers.CameraDrag.Released()) {
                _isDragging = false;
            }
        }

        private static float Zoom {
            get => MathF.Log(_camera.ZFromScale(_camera.Scale.X, 0f) + 1);
            set {
                _camera.Scale = new Vector2(_camera.ScaleFromZ(MathF.Exp(value) - 1, 0f));
            }
        }
        private static Dcrew.Camera.Camera _camera = null!;
        private static Vector2 _dragAnchor = Vector2.Zero;
        private static bool _isDragging = false;
    }
}
