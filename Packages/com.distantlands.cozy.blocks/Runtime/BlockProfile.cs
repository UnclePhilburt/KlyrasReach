using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DistantLands.Cozy;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy.Data
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Distant Lands/Cozy/Block Schedule", order = 361)]
    public class BlockProfile : CozyProfile
    {

        [System.Flags]
        public enum TimeBlocks { dawn = 1, morning = 2, day = 4, afternoon = 8, evening = 16, twilight = 32, night = 64 }

        public TimeBlocks timeBlocks;

        [Tooltip("Transitions from 4am to 5:30am")]
        [Blocks]
        public List<BlocksBlendable> dawn;
        [Tooltip("Transitions from 5:30am to 7am")]
        [Blocks]
        public List<BlocksBlendable> morning;
        [Tooltip("Transitions from 9am to 10am")]
        [Blocks]
        public List<BlocksBlendable> day;
        [Tooltip("Transitions from 1pm to 2pm")]
        [Blocks]
        public List<BlocksBlendable> afternoon;
        [Tooltip("Transitions from 4pm to 6pm")]
        [Blocks]
        public List<BlocksBlendable> evening;
        [Tooltip("Transitions from 8pm to 9pm")]
        [Blocks]
        public List<BlocksBlendable> twilight;
        [Tooltip("Transitions from 9pm to 10pm")]
        [Blocks]
        public List<BlocksBlendable> night;

    }
}