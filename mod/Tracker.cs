using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Archipelago
{
    public enum TrackerMode
    {
        Disabled,
        Closest,
        Logical,
    }

    public class TrackerThread
    {
        public static string DepthString = "";
        public static bool ItemsRelevant = true;
        public static float BaseDepth = 600f;
        public static float LogicSwimDepth = BaseDepth;
        public static float TheoreticalSeamothDepth = BaseDepth;
        public static bool IncludeSeamoth = true;
        public static bool IncludePrawn = true;
        public static bool IncludeCyclops = true;
        public static float LogicVehicleDepth = 0;
        public static string LogicVehicle = "Vehicle";
        public static long creatureScanCutOff = 33999;

        public static bool InLogic(long locID)
        {
            // Gating items
            foreach (var logic in ArchipelagoData.LogicDict)
            {
                bool hasItem = KnownTech.Contains(logic.Key);
                if (!hasItem && logic.Value.Contains(locID))
                {
                    return false;
                }
            }
            // Depth
            return -(LogicVehicleDepth + LogicSwimDepth) < ArchipelagoData.Locations[locID].Position.y;
        }

        public static void PrimeDepthSystem()
        {
            DepthString    = APState.SwimRule;
            BaseDepth      = APState.SwimDepth;
            ItemsRelevant  = APState.ItemsRelevant;
            IncludeSeamoth = APState.SeamothState == APState.Inclusion.Included;
            IncludePrawn   = APState.PrawnState == APState.Inclusion.Included;
            IncludeCyclops = APState.CyclopsState == APState.Inclusion.Included;

            TheoreticalSeamothDepth = BaseDepth;
            if (IncludeSeamoth)
            {
                TheoreticalSeamothDepth = getTheoreticalLogicDepth(900f);
            }
        }

        public static float getTheoreticalLogicDepth(float candidateVehicleDepth)
        {
            float coreDepth = BaseDepth + candidateVehicleDepth;
            if (ItemsRelevant)
            {
                return coreDepth + 350f;
            }

            return coreDepth;
        }

        public static float GetLogicDepth()
        {
            float itemdepth = 0;

            if (! ItemsRelevant)
            {
                return itemdepth;
            }

            bool hasModStation = KnownTech.Contains(TechType.Workbench);

            if (KnownTech.Contains(TechType.Seaglide))
            {
                itemdepth += 200f;
                // Ultra High Capacity Tank
                if (hasModStation && KnownTech.Contains(TechType.HighCapacityTank))
                {
                    itemdepth += 150f;
                }
            }
            else if (hasModStation && KnownTech.Contains(TechType.UltraGlideFins))
            {
                itemdepth += 50f;
                if (KnownTech.Contains(TechType.HighCapacityTank))
                {
                    itemdepth += 100f;
                }
                else if (KnownTech.Contains(TechType.PlasteelTank))
                {
                    itemdepth += 25f;
                }
            }
            else if (hasModStation && KnownTech.Contains(TechType.HighCapacityTank))
            {
                itemdepth += 100f;
            }
            else if (hasModStation && KnownTech.Contains(TechType.PlasteelTank))
            {
                itemdepth += 25f;
            }

            return itemdepth;
        }

        public static int getSeamothDepth(bool has_mod, bool has_upg)
        {
            if (! (IncludeSeamoth && KnownTech.Contains(TechType.Seamoth)))
            {
                return 0;
            }

            int depth = 200;
            if (has_upg && KnownTech.Contains(TechType.VehicleHullModule1))
            {
                depth = 300;
                if (has_mod && KnownTech.Contains(TechType.VehicleHullModule2))
                {
                    depth = 500;
                    if (KnownTech.Contains(TechType.VehicleHullModule3))
                    {
                        depth = 900;
                    }
                }
            }

            return depth;
        }

        public static int getPrawnDepth(bool has_mod, bool has_upg)
        {
            if (! (IncludePrawn && KnownTech.Contains(TechType.Exosuit)))
            {
                return 0;
            }

            int depth = 900;
            if (has_upg && KnownTech.Contains(TechType.ExoHullModule1))
            {
                depth = 1300;
                if (has_mod && KnownTech.Contains(TechType.ExoHullModule2))
                {
                    depth = 1700;
                }
            }

            return depth;
        }

        public static int getCyclopsDepth(bool has_mod, bool has_upg)
        {
            if (! (IncludeCyclops && KnownTech.Contains(TechType.Cyclops)))
            {
                return 0;
            }

            int depth = 500;
            if (KnownTech.Contains(TechType.CyclopsHullModule1))
            {
                depth = 900;
                if (has_mod && KnownTech.Contains(TechType.CyclopsHullModule2))
                {
                    depth = 1300;
                    if (KnownTech.Contains(TechType.CyclopsHullModule3))
                    {
                        depth = 1700;
                    }
                }
            }

            return depth;
        }

        public static (string, float) GetVehicleDepth()
        {
            float maxDepth = 0f;
            string logicVehicleName = "Vehicle";

            if (!KnownTech.Contains(TechType.Constructor))
            {
                return (logicVehicleName, maxDepth);
            }

            bool hasModStation = KnownTech.Contains(TechType.Workbench);
            bool hasUpgradeConsole = KnownTech.Contains(TechType.BaseUpgradeConsole) &&
                                     KnownTech.Contains(TechType.BaseMoonpool);

            int candidateDepth = getSeamothDepth(hasModStation, hasUpgradeConsole);
            if (candidateDepth > maxDepth)
            {
                logicVehicleName = "Seamoth";
                maxDepth = candidateDepth;
            }

            candidateDepth = getPrawnDepth(hasModStation, hasUpgradeConsole);
            if (candidateDepth > maxDepth)
            {
                logicVehicleName = "Prawn";
                maxDepth = candidateDepth;
            }

            candidateDepth = getCyclopsDepth(hasModStation, hasUpgradeConsole);
            if (candidateDepth > maxDepth)
            {
                logicVehicleName = "Cyclops";
                maxDepth = candidateDepth;
            }

            return (logicVehicleName, maxDepth);
        }

        public static (string, float) GetAdvancedDepth()
        {
            float maxDepth = 0;
            string logicVehicleName = "Vehicle";

            // Since Uraninite spawns ~250, this bridges nuclear from no-items/easy to uraninite
            if (KnownTech.Contains(TechType.FarmingTray))
            {
                maxDepth += 200;
                logicVehicleName = "Advanced";
            }

            bool hasReactor = false;
            bool hasReactorCapableRoom = KnownTech.Contains(TechType.BaseRoom) || KnownTech.Contains(TechType.BaseLargeRoom);

            // Uraninite spawns at ~250m; don't assume any drops from AP
            if (BaseDepth + maxDepth >= 250 && hasReactorCapableRoom && KnownTech.Contains(TechType.BaseNuclearReactor))
            {
                hasReactor = true;
                logicVehicleName = "Advanced";
            }

            // Bio fuel is easy to come by at all depths
            if (hasReactorCapableRoom && KnownTech.Contains(TechType.BaseBioReactor))
            {
                hasReactor = true;
                logicVehicleName = "Advanced";
            }

            // Need the transmitter to get the electricity back to the base
            if (KnownTech.Contains(TechType.ThermalPlant) && KnownTech.Contains(TechType.PowerTransmitter))
            {
                hasReactor = true;
                logicVehicleName = "Advanced";
            }

            if (hasReactor)
            {
                maxDepth += 1500;
            }

            return (logicVehicleName, maxDepth);
        }

        public static void UpdateTrackedLocation()
        {
            float closestDist = 100000.0f;
            long closestID = -1;
            long trackingCount = 0;
            Vector3 playerPos = Player.main.gameObject.transform.position;

            foreach (var locID in APState.Session.Locations.AllMissingLocations)
            {
                // Check that it's a static location
                if (locID < creatureScanCutOff)
                {
                    trackingCount++;
                    // Skip locations not in logic
                    if (APState.TrackedMode == TrackerMode.Logical && !InLogic(locID))
                    {
                        continue;
                    }
                    float dist = Vector3.Distance(playerPos, ArchipelagoData.Locations[locID].Position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestID = locID;
                    }
                }
            }

            APState.TrackedLocationsCount = trackingCount;
            APState.TrackedDistance = closestDist;
            APState.TrackedLocation = closestID;
            if (closestID != -1)
            {
                APState.TrackedLocationName = APState.Session.Locations.GetLocationNameFromId(APState.TrackedLocation);
                Vector3 directionVector = ArchipelagoData.Locations[closestID].Position - Player.main.gameObject.transform.position;
                directionVector.Normalize();
                APState.TrackedAngle = Vector3.Angle(directionVector, Player.main.viewModelCamera.transform.forward);
            }
        }

        public static void UpdateTrackedFish()
        {
            long maxFish = 7;
            var remainingFish = new List<long>();
            foreach (var locID in APState.Session.Locations.AllMissingLocations)
            {
                // Check that it's a static location
                if (locID > creatureScanCutOff)
                {
                    remainingFish.Add(locID);
                }
            }

            APState.TrackedFishCount = remainingFish.Count;
            if (APState.TrackedFishCount == 0)
            {
                APState.TrackedFish = "";
                return;
            }

            remainingFish.Sort();
            var display_fish = new List<string>();
            for (int i = 0; i < Math.Min(APState.TrackedFishCount, maxFish); i++)
            {
                display_fish.Add(
                    APState.Session.Locations.GetLocationNameFromId(
                        remainingFish[i]).Replace(
                        " Scan", ""));
            }
            APState.TrackedFish = String.Join(", ", display_fish);
        }

        public static bool KnownTechLoaded()
        {
            try
            {
                // Doesn't matter what we check for here, we don't care about the results
                KnownTech.Contains(TechType.Seaglide);
            }
            catch (NullReferenceException)
            {
                return false;
            }

            return true;
        }

        public static void DoWork()
        {
            while (true)
            {
                Thread.Sleep(150);

                // These three variables are close, but there's still a race condition
                // so add a check for the KnownTech object as well
                if (APState.state != APState.State.InGame
                    || APState.Session == null
                    || Player.main == null
                    || !KnownTechLoaded())
                {
                    APState.TrackedFishCount = 0;
                    APState.TrackedLocationsCount = 0;
                    APState.TrackedLocation = -1;
                    continue;
                }

                if (APState.SwimRule != DepthString)
                {
                    PrimeDepthSystem();
                }

                LogicSwimDepth = BaseDepth + GetLogicDepth();

                bool seamothCanMakeIt = TheoreticalSeamothDepth > 1443;
                if (!seamothCanMakeIt && !IncludePrawn && !IncludeCyclops)
                {
                    (LogicVehicle, LogicVehicleDepth) = GetAdvancedDepth();
                }
                else
                {
                    (LogicVehicle, LogicVehicleDepth) = GetVehicleDepth();
                }

                UpdateTrackedLocation();
                UpdateTrackedFish();
            }
        }
    }
}
