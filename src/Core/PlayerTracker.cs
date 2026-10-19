using System.Collections.Generic;
using UnityEngine;

namespace AntiCheatExtension.Core
{
    public sealed class PlayerTracker
    {
        private const float GraceSeconds = 10f;

        private static readonly Dictionary<int, PlayerTracker> Trackers = new Dictionary<int, PlayerTracker>();
        private static readonly object Sync = new object();

        private readonly Queue<KeyValuePair<float, float>> horizontalSamples = new Queue<KeyValuePair<float, float>>();

        private PlayerTracker(int entityId)
        {
            EntityId = entityId;
        }

        public int EntityId { get; }

        public bool HasPosition { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public float LastSampleTime { get; private set; }
        public float GraceUntil { get; private set; }

        public float AirborneSince { get; set; } = -1f;
        public float AirborneStartY { get; set; }
        public int SolidChecks { get; set; }
        public int LastLevel { get; set; } = -1;

        public bool InGrace => Time.time < GraceUntil;

        public static PlayerTracker Get(int entityId)
        {
            lock (Sync)
            {
                if (!Trackers.TryGetValue(entityId, out var tracker))
                {
                    tracker = new PlayerTracker(entityId);
                    Trackers[entityId] = tracker;
                }
                return tracker;
            }
        }

        public static void Remove(int entityId)
        {
            lock (Sync)
            {
                Trackers.Remove(entityId);
            }
        }

        public void StartGrace()
        {
            GraceUntil = Time.time + GraceSeconds;
            ResetMovement();
        }

        public void ResetMovement()
        {
            HasPosition = false;
            AirborneSince = -1f;
            SolidChecks = 0;
            horizontalSamples.Clear();
        }

        public void SetPosition(Vector3 position, float time)
        {
            LastPosition = position;
            LastSampleTime = time;
            HasPosition = true;
        }

        public float AddHorizontalSample(float time, float distance, float windowSeconds, out float coveredSeconds)
        {
            horizontalSamples.Enqueue(new KeyValuePair<float, float>(time, distance));
            while (horizontalSamples.Count > 0 && time - horizontalSamples.Peek().Key > windowSeconds)
            {
                horizontalSamples.Dequeue();
            }

            var total = 0f;
            var oldest = time;
            foreach (var sample in horizontalSamples)
            {
                total += sample.Value;
                oldest = Mathf.Min(oldest, sample.Key);
            }

            coveredSeconds = time - oldest + (time - LastSampleTime);
            return total;
        }
    }
}
