using UnityEngine;

namespace Script.Player
{
    public abstract class PlayerObject : MonoBehaviour
    {
        public abstract int GetLastDirection();
        public abstract bool GetLocked();
    }
}
