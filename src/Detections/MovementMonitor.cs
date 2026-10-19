using AntiCheat.Core;
using UnityEngine;

namespace AntiCheat.Detections
{
    /// <summary>
    /// Samples remote player positions on the server and checks teleport, speed, fly and noclip.
    /// </summary>
    internal static class MovementMonitor
    {
        private const float SampleInterval = 0.5f;

        private static float nextSample;

        public static void Tick()
        {
            var now = Time.time;
            if (now < nextSample)
            {
                return;
            }
            nextSample = now + SampleInterval;

            var world = ServerContext.World;
            if (world == null)
            {
                return;
            }

            var players = world.Players.list;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player != null && player.isEntityRemote)
                {
                    Check(world, player, now);
                }
            }
        }

        private static void Check(World world, EntityPlayer player, float now)
        {
            var tracker = PlayerTracker.Get(player.entityId);

            if (!player.Spawned || player.IsDead() || player.AttachedToEntity != null || tracker.InGrace ||
                ServerContext.IsExempt(player.entityId) || ServerContext.HasExemptBuff(player))
            {
                tracker.ResetMovement();
                return;
            }

            var position = player.position;
            if (!tracker.HasPosition)
            {
                tracker.SetPosition(position, now);
                return;
            }

            var settings = Settings.Current;
            var delta = position - tracker.LastPosition;
            var distance = delta.magnitude;

            var teleport = settings.Get(DetectionNames.Teleport);
            if (teleport.Enabled && distance > teleport.GetFloat("maxDistance", 150f) * settings.SpeedMultiplier)
            {
                DetectionService.Report(player.entityId, DetectionNames.Teleport,
                    $"moved {distance:F0}m in {now - tracker.LastSampleTime:F1}s from {Format(tracker.LastPosition)} to {Format(position)}");
                tracker.ResetMovement();
                tracker.SetPosition(position, now);
                return;
            }

            CheckSpeed(player, tracker, settings, delta, now);
            CheckFly(world, player, tracker, settings, position, now);
            CheckNoClip(world, player, tracker, settings, position);

            tracker.SetPosition(position, now);
        }

        private static void CheckSpeed(EntityPlayer player, PlayerTracker tracker, Settings settings, Vector3 delta, float now)
        {
            var speed = settings.Get(DetectionNames.Speed);
            if (!speed.Enabled)
            {
                return;
            }

            var window = Mathf.Max(1f, speed.GetFloat("windowSeconds", 3f));
            var horizontal = new Vector2(delta.x, delta.z).magnitude;
            var total = tracker.AddHorizontalSample(now, horizontal, window, out var covered);
            if (covered < window * 0.8f)
            {
                return;
            }

            var average = total / covered;
            var limit = speed.GetFloat("maxSpeed", 20f) * settings.SpeedMultiplier;
            if (average > limit)
            {
                DetectionService.Report(player.entityId, DetectionNames.Speed,
                    $"average {average:F1} m/s over {covered:F1}s (limit {limit:F1})");
            }
        }

        private static void CheckFly(World world, EntityPlayer player, PlayerTracker tracker, Settings settings, Vector3 position, float now)
        {
            var fly = settings.Get(DetectionNames.Fly);
            if (!fly.Enabled)
            {
                return;
            }

            if (player.onGround || player.IsInWater() || player.IsInElevator() || HasBlocksNearby(world, position))
            {
                tracker.AirborneSince = -1f;
                return;
            }

            if (tracker.AirborneSince < 0f)
            {
                tracker.AirborneSince = now;
                tracker.AirborneStartY = position.y;
                return;
            }

            var airTime = now - tracker.AirborneSince;
            var maxAir = fly.GetFloat("maxAirSeconds", 6f);
            if (airTime > maxAir && position.y > tracker.AirborneStartY - 2f)
            {
                DetectionService.Report(player.entityId, DetectionNames.Fly,
                    $"airborne {airTime:F1}s with no support at {Format(position)}");
                tracker.AirborneSince = now;
                tracker.AirborneStartY = position.y;
            }
        }

        private static void CheckNoClip(World world, EntityPlayer player, PlayerTracker tracker, Settings settings, Vector3 position)
        {
            var noClip = settings.Get(DetectionNames.NoClip);
            if (!noClip.Enabled)
            {
                return;
            }

            var feet = new Vector3i(position + new Vector3(0f, 0.1f, 0f));
            var head = feet + Vector3i.up;
            if (IsSolid(world, feet) && IsSolid(world, head))
            {
                tracker.SolidChecks++;
                var required = Mathf.Max(1, noClip.GetInt("minChecks", 4));
                if (tracker.SolidChecks >= required)
                {
                    DetectionService.Report(player.entityId, DetectionNames.NoClip,
                        $"inside solid blocks for {tracker.SolidChecks} checks at {Format(position)}");
                    tracker.SolidChecks = 0;
                }
            }
            else
            {
                tracker.SolidChecks = 0;
            }
        }

        private static bool IsSolid(World world, Vector3i position)
        {
            var block = world.GetBlock(position);
            if (block.isair)
            {
                return false;
            }

            var type = block.Block;
            return type != null && type.IsCollideMovement && type.shape != null && type.shape.IsSolidCube;
        }

        private static bool HasBlocksNearby(World world, Vector3 position)
        {
            var feet = new Vector3i(position);
            for (var dy = -3; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (!world.GetBlock(new Vector3i(feet.x + dx, feet.y + dy, feet.z + dz)).isair)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static string Format(Vector3 v) => $"({v.x:F0}, {v.y:F0}, {v.z:F0})";
    }
}
