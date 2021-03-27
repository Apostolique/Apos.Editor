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
        public static Matrix ViewInvert => _camera.ViewInvert();
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
            _camera.Scale = new Vector2(InterpolateTowardsTarget(_camera.Scale.X, _targetZoom, 0.1f, 0.0001f));
            _camera.Angle = InterpolateTowardsTarget(_camera.Angle, _targetRotation, 0.1f, 0.0001f);
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
            get => MathF.Log(_camera.ZFromScale(_targetZoom, 0f) + 1);
            set {
                _targetZoom = _camera.ScaleFromZ(MathF.Exp(value) - 1, 0f);
            }
        }
        private static Dcrew.Camera.Camera _camera = null!;
        private static Vector2 _dragAnchor = Vector2.Zero;
        private static bool _isDragging = false;

        private static float _targetZoom = 1f;
        private static float _targetRotation = 0f;
    }
}
