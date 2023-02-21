using UnityEngine;
using Unity.Mathematics;

using JetBrains.Annotations;

using F32x2 = Unity.Mathematics.float2;
using F32x3 = Unity.Mathematics.float3;

namespace Game.Shared
{
    [PublicAPI]
    public abstract class Component : MonoBehaviour
    {
        public F32x3 WorldPos
        {
            get => transform.position;
            set => transform.position = value;
        }
        public F32x3 LocalPos
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }
        
        public quaternion Rot
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        public F32x3 WorldScale
        {
            get => transform.lossyScale;
        }
        public F32x3 LocalScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public F32x3 Backward => -transform.forward;
        public F32x3 Forward  => transform.forward;
        public F32x3 Left     => -transform.right;
        public F32x3 Right    => transform.right;
        public F32x3 Down     => -transform.up;
        public F32x3 Up       => transform.up;


    }
}
