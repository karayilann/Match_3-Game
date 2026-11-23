using System;
using UnityEngine;
using View;

namespace Gameplay.Animation
{
    public enum AnimationTargetType : byte
    {
        Position,
        Scale,
        Shake,
        Pulse,
        Delay,
        Callback
    }

    public enum AnimationEase : byte
    {
        Linear,
        QuadIn,
        QuadOut,
        BounceOut
    }

    public struct TileAnimationCommand
    {
        public GemView View;
        public AnimationTargetType TargetType;
        public AnimationEase Ease;
        public Vector3 StartValue;
        public Vector3 EndValue;
        public float Duration;
        public float Elapsed;
        public float StartDelay;
        public Action<int> Callback;
        public int CallbackArg;
    }
}
