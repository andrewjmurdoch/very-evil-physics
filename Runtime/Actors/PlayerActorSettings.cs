using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "PlayerActorSettings", menuName = "TKW/Player/PlayerActorSettings", order = 0)]
    public class PlayerActorSettings : ScriptableObject
    {
        public float COYOTE_TIME;
        public float CROUCH_TIME;
        public float CROUCH_FRICTION;
        public float JUMP_SPEED;
        public float JUMP_HORIZONTAL_FRACTION;
        public float JUMP_BANK_TIME;
        public float LONG_JUMP_START_TIME;
        public float LONG_JUMP_END_TIME;
        public float LONG_JUMP_SPEED;
        public float MOVEMENT_SPEED;
        public float MOVEMENT_MAX_SPEED;
        public float MOVEMENT_THRESHOLD;
        public float AIR_MOVEMENT_SPEED;
        public float AIR_MOVEMENT_MAX_SPEED;
        public float JUMP_PEAK_MOVEMENT_SPEED;
        public float JUMP_PEAK_MOVEMENT_MAX_SPEED;
        public float JUMP_PEAK_THRESHOLD;
        public float WALLPLANT_ENTRY_SPEED;
        public float WALLPLANT_SLIP_SPEED;
        public float WALLPLANT_TIME;
        public float WALLPLANT_EXIT_TIME;
        public float WALLPLANT_EXIT_THRESHOLD;
        public float WALLPLANT_JUMP_SPEED_HORIZONTAL;
        public float WALLPLANT_JUMP_SPEED_VERTICAL;
    }
}