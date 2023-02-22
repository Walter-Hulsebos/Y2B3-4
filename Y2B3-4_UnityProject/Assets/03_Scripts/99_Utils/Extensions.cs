using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Utils
{
    public static class Extensions
    {
        public static Boolean TryFindObjectOfType<T>(out T result) where T : Component
        {
            result = Object.FindObjectOfType<T>();

            return (result != null);
        }
    }
}
