using System;
using Apos.Input;
using A = Apos.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace GameProject {
    public static class Camera {
        public static void Setup(GraphicsDevice graphicsDevice, GameWindow window) {
            _camera = new A.Camera(new A.DefaultViewport(graphicsDevice, window));
        }

        public static Vector2 MouseWorld = Vector2.Zero;
        public static Matrix View => _camera.View;
        public static Matrix ViewInvert => _camera.ViewInvert;
        public static RectangleF ViewRect => _camera.ViewRect;
        public static float Rotation => _camera.Rotation;
        public static Vector2 Origin => _camera.VirtualViewport.Origin;
        public static float ScreenToWorldScale => _camera.ScreenToWorldScale();

        public static Matrix GetView(float z) => _camera.GetView(z);

        public static float FocalLength {
            get => _camera.FocalLength;
            set {
                _camera.FocalLength = value;
            }
        }
        public static float Z {
            get => _camera.Z;
            set {
                _camera.Z = value;
            }
        }

        public static void UpdateInput() {
            if (MouseCondition.Scrolled() && !Triggers.SelectionCycle.Held(false)) {
                Zoom = MathF.Min(MathF.Max(Zoom - MouseCondition.ScrollDelta * 0.001f, 0.2f), 4f);
            }

            if (Triggers.RotateLeft.Pressed()) {
                _targetRotation += MathHelper.PiOver4;
            }
            if (Triggers.RotateRight.Pressed()) {
                _targetRotation -= MathHelper.PiOver4;
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

        public static void Update() {
            _camera.Z = _camera.ScaleToZ(InterpolateTowardsTarget(_camera.ZToScale(_camera.Z, 0f), _targetZoom, 0.1f, 0.0001f), 0f);
            _camera.Rotation = InterpolateTowardsTarget(_camera.Rotation, _targetRotation, 0.1f, 0.0001f);
        }

        /// <summary>
        /// Poor man's tweening function.
        /// If the result is stored in the `from` value, it will create a nice interpolation over multiple frames.
        /// </summary>
        /// <param name="from">The value to start from.</param>
        /// <param name="target">The value to reach.</param>
        /// <param name="speed">A value between 0f and 1f.</param>
        /// <param name="snapNear">When the difference between the target and the result is smaller than this value, the target will be returned.</param>
        private static float InterpolateTowardsTarget(float from, float target, float speed, float snapNear) {
            float result = MathHelper.Lerp(from, target, speed);

            if (from < target) {
                result = MathHelper.Clamp(result, from, target);
            } else {
                result = MathHelper.Clamp(result, target, from);
            }

            if (MathF.Abs(target - result) < snapNear) {
                return target;
            } else {
                return result;
            }
        }

        private static float Zoom {
            get => MathF.Log(_camera.ScaleToZ(_targetZoom, 0f) + 1);
            set {
                _targetZoom = _camera.ZToScale(MathF.Exp(value) - 1, 0f);
            }
        }
        private static A.Camera _camera = null!;
        private static Vector2 _dragAnchor = Vector2.Zero;
        private static bool _isDragging = false;

        private static float _targetZoom = 1f;
        private static float _targetRotation = 0f;
    }
}
