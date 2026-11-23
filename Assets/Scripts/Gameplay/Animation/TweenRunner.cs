using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using View;

namespace Gameplay.Animation
{
    public sealed class TweenRunner
    {
        private const float SHAKE_FREQUENCY_RAD = Mathf.PI * 14f;
        private const float SHAKE_VERTICAL_FREQUENCY_RATIO = 1.35f;
        private const float SHAKE_VERTICAL_AMPLITUDE_RATIO = 0.45f;

        private readonly List<TileAnimationCommand> _activeCommands;

        public TweenRunner(int capacity)
        {
            _activeCommands = new List<TileAnimationCommand>(capacity);
        }

        public bool HasActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _activeCommands.Count > 0;
        }

        public void Clear()
        {
            _activeCommands.Clear();
        }

        public void Add(in TileAnimationCommand command)
        {
            _activeCommands.Add(command);
        }

        public void Cancel(GemView view)
        {
            for (int i = _activeCommands.Count - 1; i >= 0; i--)
            {
                if (_activeCommands[i].View == view)
                {
                    int lastIndex = _activeCommands.Count - 1;
                    _activeCommands[i] = _activeCommands[lastIndex];
                    _activeCommands.RemoveAt(lastIndex);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = _activeCommands.Count - 1; i >= 0; i--)
            {
                TileAnimationCommand command = _activeCommands[i];
                command.Elapsed += deltaTime;

                float activeTime = command.Elapsed - command.StartDelay;
                if (activeTime < 0f)
                {
                    _activeCommands[i] = command;
                    continue;
                }

                if (command.TargetType == AnimationTargetType.Callback)
                {
                    int lastCallbackIndex = _activeCommands.Count - 1;
                    _activeCommands[i] = _activeCommands[lastCallbackIndex];
                    _activeCommands.RemoveAt(lastCallbackIndex);
                    command.Callback.Invoke(command.CallbackArg);
                    continue;
                }

                float t = command.Duration > 0f ? Mathf.Clamp01(activeTime / command.Duration) : 1f;
                ApplyCommand(command, t);

                if (t >= 1f)
                {
                    int lastIndex = _activeCommands.Count - 1;
                    _activeCommands[i] = _activeCommands[lastIndex];
                    _activeCommands.RemoveAt(lastIndex);
                    continue;
                }

                _activeCommands[i] = command;
            }
        }

        private static void ApplyCommand(TileAnimationCommand command, float t)
        {
            if (command.TargetType == AnimationTargetType.Delay)
            {
                return;
            }

            if (command.TargetType == AnimationTargetType.Position)
            {
                float easedT = EvaluateEase(t, command.Ease);
                command.View.SetPosition(Vector3.LerpUnclamped(command.StartValue, command.EndValue, easedT));
                return;
            }

            if (command.TargetType == AnimationTargetType.Scale)
            {
                command.View.SetScale(Vector3.Lerp(command.StartValue, command.EndValue, t));
                return;
            }

            if (command.TargetType == AnimationTargetType.Pulse)
            {
                if (t >= 1f)
                {
                    command.View.SetScale(Vector3.one);
                    return;
                }

                float scaleOffset = Mathf.Sin(t * Mathf.PI) * (command.EndValue.x - 1f);
                command.View.SetScale(Vector3.one * (1f + scaleOffset));
                return;
            }

            if (t >= 1f)
            {
                command.View.SetPosition(command.StartValue);
                return;
            }

            float envelope = 1f - t;
            float offsetX = Mathf.Sin(t * SHAKE_FREQUENCY_RAD) * envelope * command.EndValue.x;
            float offsetY = Mathf.Cos(t * SHAKE_FREQUENCY_RAD * SHAKE_VERTICAL_FREQUENCY_RATIO) * envelope * (command.EndValue.x * SHAKE_VERTICAL_AMPLITUDE_RATIO);
            Vector3 pos = command.StartValue;
            pos.x += offsetX;
            pos.y += offsetY;
            command.View.SetPosition(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EvaluateEase(float t, AnimationEase ease)
        {
            switch (ease)
            {
                case AnimationEase.QuadIn:
                    return t * t;
                case AnimationEase.QuadOut:
                    return t * (2f - t);
                case AnimationEase.BounceOut:
                    return EaseBounceOut(t);
                default:
                    return t;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EaseBounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
