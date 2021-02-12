using System;
using Apos.Input;

namespace GameProject {
    class FPSCounter {
        public int FramesPerSecond {
            get;
            private set;
        } = 0;
        public double UpdatePerSecond {
            get;
            private set;
        } = 0;
        public double TimePerFrame {
            get;
            private set;
        } = 0;
        public double TimePerUpdate {
            get;
            private set;
        } = 0;
        public int DroppedFrames {
            get;
            set;
        } = 0;

        public void Update(double elapsedTime) {
            _updateCounter++;

            _totalTime += elapsedTime;
            timer += elapsedTime;
            if (timer <= _oneSecond) {
                return;
            } else if (timer > _oneSecond * 60) {
                // This fixes a case where the game stops being update for a long time.
                // For example when the computer is in sleep mode.
                timer = _oneSecond * 60;
                return;
            }

            UpdatePerSecond = _updateCounter;
            FramesPerSecond = _framesCounter;
            _updateCounter = 0;
            _framesCounter = 0;
            timer -= _oneSecond;

            TimePerUpdate = Math.Truncate(1000d / UpdatePerSecond * 10000) / 10000;
            if (FramesPerSecond > 0) {
                TimePerFrame = Math.Truncate(1000d / FramesPerSecond * 10000) / 10000;
            }

            if (FramesPerSecond < 60 && _totalTime > 3000 && InputHelper.IsActive) {
                DroppedFrames += 60 - FramesPerSecond;
            }
        }
        public void Draw() {
            _framesCounter++;
        }

        private int _oneSecond = 1000;
        private double timer = 0;
        private int _framesCounter = 0;
        private double _updateCounter = 0;
        private double _totalTime = 0;
    }
}
