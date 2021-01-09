using System;
using Apos.Input;
using Dcrew.Camera;
using Microsoft.Xna.Framework;

namespace GameProject {
    public class CameraManager {
        public CameraManager() {
            Camera = new Camera(new Vector2(0f, 0f), 0f, new Vector2(1f));
        }

        public Camera Camera;
        public Vector2 MouseWorld = Vector2.Zero;
        public Matrix View => Camera.View();

        public void UpdateInput() {
            int scrollDelta = InputHelper.NewMouse.ScrollWheelValue - InputHelper.OldMouse.ScrollWheelValue;
            if (scrollDelta != 0) {
                Zoom = MathF.Max(Zoom - scrollDelta * 0.0005f, 0.1f);
            }

            if (Triggers.RotateLeft.Pressed()) {
                Camera.Angle += MathHelper.PiOver4;
            }
            if (Triggers.RotateRight.Pressed()) {
                Camera.Angle -= MathHelper.PiOver4;
            }

            MouseWorld = Camera.ScreenToWorld(InputHelper.NewMouse.X, InputHelper.NewMouse.Y);

            if (Triggers.CameraDrag.Pressed()) {
                _dragAnchor = MouseWorld;
                _isDragging = true;
            }
            if (_isDragging && Triggers.CameraDrag.HeldOnly()) {
                Camera.XY += _dragAnchor - MouseWorld;
                MouseWorld = _dragAnchor;
            }
            if (_isDragging && Triggers.CameraDrag.Released()) {
                _isDragging = false;
            }
        }

        float Zoom {
            get => MathF.Sqrt(Camera.ZFromScale(Camera.Scale.X, 0f));
            set {
                Camera.Scale = new Vector2(Camera.ScaleFromZ(value * value, 0f));
            }
        }
        Vector2 _dragAnchor = Vector2.Zero;
        bool _isDragging = false;
    }
}
