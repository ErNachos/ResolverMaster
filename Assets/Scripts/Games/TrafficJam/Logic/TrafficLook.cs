using System;
using System.Collections.Generic;
using ResolverMaster.Core.Puzzle;

namespace ResolverMaster.Games.TrafficJam
{
    /// <summary>
    /// TJ-002: how a level looks, from its seed (pure logic, the same level always looks the same): the drawing and the front of every
    /// vehicle and the drawing of every obstacle. Target cars use their own drawing (red or blue; worried while waiting, happy when they
    /// leave) and face their exit; the other vehicles get a car or truck drawing (color and expression) and a front along their line from
    /// the seed; obstacles one of the drawings the chapter theme allows (<see cref="ThemeVariants"/>, Core). Sprite names are the files of
    /// Assets/Art/TrafficJam (Tools/Art/make_trafficjam_art.py); the drawings have their front up, the board turns them.
    /// </summary>
    public sealed class TrafficLook
    {
        /// <summary><see cref="ArtVariants"/> groups (11 and 12 are the TJ-001 color and front choices, kept).</summary>
        public const int CarGroup = 11, FrontGroup = 12, TruckGroup = 13, ObstacleGroup = 14;

        public static readonly string[] CarNames = { "Car_Yellow", "Car_Lilac", "Car_Grey", "Car_Beige" };
        public static readonly string[] TruckNames = { "Truck_Grey", "Truck_Yellow", "Truck_Lilac" };
        public static readonly string[] ObstacleNames = { "Obstacle_Barrier", "Obstacle_Manhole", "Obstacle_Rock", "Obstacle_Trash" };

        /// <summary>Every drawing of the game (the validator and the tests check they all exist).</summary>
        public static IEnumerable<string> AllNames()
        {
            for (int t = 0; t < 2; t++)
            {
                yield return TargetName(t, false);
                yield return TargetName(t, true);
            }
            foreach (var n in CarNames) yield return n;
            foreach (var n in TruckNames) yield return n;
            foreach (var n in ObstacleNames) yield return n;
        }

        /// <summary>The drawing of target car <paramref name="target"/> (0 red, 1 blue): happy from the moment it starts to leave.</summary>
        public static string TargetName(int target, bool leaving) => (target == 1 ? "Target_Blue_" : "Target_Red_") + (leaving ? "Happy" : "Worried");

        /// <summary>Per vehicle: its drawing (target cars: the worried one), and its front (SquareTopology direction: 0 up .. 3 left).</summary>
        public readonly string[] Vehicles;
        public readonly int[] Front;
        /// <summary>Per vehicle: index in <see cref="CarNames"/> or <see cref="TruckNames"/> (-1 for target cars).</summary>
        public readonly int[] Variant;
        /// <summary>The obstacle cells in reading order, and the drawing of each.</summary>
        public readonly int[] ObstacleCells;
        public readonly string[] Obstacles;

        TrafficLook(int vehicles, int obstacles)
        {
            Vehicles = new string[vehicles];
            Front = new int[vehicles];
            Variant = new int[vehicles];
            ObstacleCells = new int[obstacles];
            Obstacles = new string[obstacles];
        }

        /// <param name="allowedObstacles">The obstacle drawings the theme allows (all of them in the Challenge).</param>
        public static TrafficLook For(TrafficLayout layout, int seed, IReadOnlyList<string> allowedObstacles)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (allowedObstacles == null || allowedObstacles.Count == 0) allowedObstacles = ObstacleNames;
            var look = new TrafficLook(layout.VehicleCount, layout.ObstacleCount);
            int others = 0, cars = 0, trucks = 0;
            for (int i = 0; i < layout.VehicleCount; i++)
            {
                var v = layout.Vehicles[i];
                if (v.IsTarget)
                {
                    look.Vehicles[i] = TargetName(v.Target, false);
                    look.Front[i] = v.ExitSide;
                    look.Variant[i] = -1;
                    continue;
                }
                bool truck = v.Kind == VehicleKind.Truck;
                int variant = truck ? ArtVariants.Pick(seed, TruckGroup, trucks++, TruckNames.Length) : ArtVariants.Pick(seed, CarGroup, cars++, CarNames.Length);
                look.Variant[i] = variant;
                look.Vehicles[i] = truck ? TruckNames[variant] : CarNames[variant];
                bool flip = ArtVariants.Pick(seed, FrontGroup, others++, 2) == 1;
                look.Front[i] = v.Horizontal ? (flip ? SquareTopology.Left : SquareTopology.Right) : (flip ? SquareTopology.Down : SquareTopology.Up);
            }
            int k = 0;
            for (int c = 0; c < layout.CellCount; c++)
            {
                if ((layout.Obstacles >> c & 1) == 0) continue;
                look.ObstacleCells[k] = c;
                look.Obstacles[k] = ArtVariants.Pick(seed, ObstacleGroup, k, allowedObstacles);
                k++;
            }
            return look;
        }
    }
}
