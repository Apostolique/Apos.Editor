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
                _targetExp = MathHelper.Clamp(_targetExp - MouseCondition.ScrollDelta * _expDistance, _maxExp, _minExp);
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
            _camera.Z = _camera.ScaleToZ(ExpToScale(Interpolate(ScaleToExp(_camera.ZToScale(_camera.Z, 0f)), _targetExp, _speed, _snapDistance)), 0f);
            _camera.Rotation = Interpolate(_camera.Rotation, _targetRotation, _speed, _snapDistance);
        }

        /// <summary>
        /// Poor man's tweening function.
        /// If the result is stored in the `from` value, it will create a nice interpolation over multiple frames.
        /// </summary>
        /// <param name="from">The value to start from.</param>
        /// <param name="target">The value to reach.</param>
        /// <param name="speed">A value between 0f and 1f.</param>
        /// <param name="snapNear">When the difference between the target and the result is smaller than this value, the target will be returned.</param>
        private static float Interpolate(float from, float target, float speed, float snapNear) {
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
        private static float ScaleToExp(float scale) {
            return -MathF.Log(scale);
        }
        private static float ExpToScale(float exp) {
            return MathF.Exp(-exp);
        }

        private static A.Camera _camera = null!;
        private static Vector2 _dragAnchor = Vector2.Zero;
        private static bool _isDragging = false;

        private static float _targetExp = 0f;
        private static float _targetRotation = 0f;

        private static float _speed = 0.08f;
        private static float _snapDistance = 0.001f;

        private static float _expDistance = 0.002f;
        private static float _maxExp = -2f;
        private static float _minExp = 4f;
    }
}
