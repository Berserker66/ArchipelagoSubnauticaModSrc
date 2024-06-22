using System;
using System.Collections.Generic;
using System.IO;
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
        public static float LogicVehicleDepth = 0;
        public static string LogicVehicle = "Vehicle";
        public static bool InLogic(long locID)
        {
            // Gating items
            foreach (var logic in ArchipelagoData.LogicDict)
            {
                bool hasItem;
                try
                {
                    hasItem = KnownTech.Contains(logic.Key);
                }
                catch (NullReferenceException)
                {
                    hasItem = false;
                }
                
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

            DepthString = APState.SwimRule;
            if (DepthString.Length == 0)
            {
                // assume most permissive logic if none given
                DepthString = "items_hard";
            }

            var nameParts = DepthString.Split('_');

            ItemsRelevant = nameParts.Length > 1;
            switch (nameParts.GetLast())
            {
                case "easy":
                {
                    BaseDepth = 200f;
                    break;
                }
                case "normal":
                {
                    BaseDepth = 400f;
                    break;
                }
                case "hard":
                {
                    BaseDepth = 600f;
                    break;
                }
            }
        }

        public static void UpdateLogicDepth()
        {
            float swimdepth = BaseDepth;
            bool hasModStation;
            try
            {
                hasModStation = KnownTech.Contains(TechType.Workbench);
            }
            catch (NullReferenceException)
            {
                return;
            }

            if (KnownTech.Contains(TechType.Seaglide))
            {
                swimdepth += 200f;
                // Ultra High Capacity Tank
                if (hasModStation && KnownTech.Contains(TechType.HighCapacityTank))
                {
                    swimdepth += 150f;
                }
            }
            else if (hasModStation && KnownTech.Contains(TechType.UltraGlideFins))
            {
                swimdepth += 50f;
                if (KnownTech.Contains(TechType.HighCapacityTank))
                {
                    swimdepth += 100f;
                }
                else if (KnownTech.Contains(TechType.PlasteelTank))
                {
                    swimdepth += 25f;
                }
            }
            else if (hasModStation && KnownTech.Contains(TechType.HighCapacityTank))
            {
                swimdepth += 100f;
            }
            else if (hasModStation && KnownTech.Contains(TechType.PlasteelTank))
            {
                swimdepth += 25f;
            }

            LogicSwimDepth = swimdepth;
        }

        public static void UpdateVehicleDepth()
        {
            bool hasBay;
            float maxDepth = 0f;
            string logicVehicleName = "Vehicle";
            
            try
            {
                hasBay = KnownTech.Contains(TechType.Constructor);
            }
            catch (Exception)
            {
                return;
            }

            if (!hasBay)
            {
                LogicVehicleDepth = 0;
                LogicVehicle = logicVehicleName;
                return;
            }
            
            bool hasModStation = KnownTech.Contains(TechType.Workbench);
            bool hasUpgradeConsole = KnownTech.Contains(TechType.BaseUpgradeConsole) && 
                                     KnownTech.Contains(TechType.BaseMoonpool);
            float oldDepth = maxDepth;
            
            if (KnownTech.Contains(TechType.Seamoth))
            {
                maxDepth = Math.Max(maxDepth, 200f);
                if (hasUpgradeConsole && KnownTech.Contains(TechType.VehicleHullModule1))
                {
                    maxDepth = Math.Max(maxDepth, 300f);
                    if (hasModStation && KnownTech.Contains(TechType.VehicleHullModule2))
                    {
                        maxDepth = Math.Max(maxDepth, 500f);
                        if (KnownTech.Contains(TechType.VehicleHullModule3))
                        {
                            maxDepth = Math.Max(maxDepth, 900f);
                        }
                    }
                }
                if (Math.Abs(oldDepth - maxDepth) > 1)
                {
                    logicVehicleName = "Seamoth";
                }
            }
            oldDepth = maxDepth;
            if (KnownTech.Contains(TechType.Exosuit))
            {
                maxDepth = Math.Max(maxDepth, 900f);
                if (hasUpgradeConsole && KnownTech.Contains(TechType.ExoHullModule1))
                {
                    maxDepth = Math.Max(maxDepth, 1300f);
                    if (hasModStation && KnownTech.Contains(TechType.ExoHullModule2))
                    {
                        maxDepth = Math.Max(maxDepth, 1700f);
                    }
                }
                if (Math.Abs(oldDepth - maxDepth) > 1)
                {
                    logicVehicleName = "Prawn Suit";
                }
            }
            oldDepth = maxDepth;
            if (KnownTech.Contains(TechType.Cyclops))
            {
                maxDepth = Math.Max(maxDepth, 500f);
                if (KnownTech.Contains(TechType.CyclopsHullModule1))
                {
                    maxDepth = Math.Max(maxDepth, 900f);
                    if (hasModStation && KnownTech.Contains(TechType.CyclopsHullModule2))
                    {
                        maxDepth = Math.Max(maxDepth, 1300f);
                        if (KnownTech.Contains(TechType.CyclopsHullModule3))
                        {
                            maxDepth = Math.Max(maxDepth, 1700f);
                        }
                    }
                }
                if (Math.Abs(oldDepth - maxDepth) > 1)
                {
                    logicVehicleName = "Cyclops";
                }
            }

            LogicVehicle = logicVehicleName;
            LogicVehicleDepth = maxDepth;
        }
        
        // Debug.Log doesn't want to work for me in the thread, despite documentation saying it is threadsafe.
        // So this is the solution for now, and probably ever.
        public static void Log(string text)
        {
            using (StreamWriter sw = File.AppendText("TrackerThread.txt"))
            {
                sw.WriteLine(text);
            }
        }
        
        public static void DoWork()
        {
            float closestDist;
            float dist;
            long closestID;
            long scanCutOff = 33999;
            long maxFish = 7;


            Vector3 playerPos;
            
            while (true)
            {

                if (APState.SwimRule != DepthString)
                {
                    PrimeDepthSystem();
                }

                if (ItemsRelevant)
                {
                    UpdateLogicDepth();
                }
                else
                {
                    LogicSwimDepth = BaseDepth;
                }
                
                UpdateVehicleDepth();
                // locations
                long trackingCount = 0;
                if (APState.state == APState.State.InGame && APState.Session != null && Player.main != null)
                {
                    playerPos = Player.main.gameObject.transform.position;
                    
                    closestDist = 100000.0f;
                    closestID = -1;
                    foreach (var locID in APState.Session.Locations.AllMissingLocations)
                    {
                        // Check that it's a static location
                        if (locID < scanCutOff)
                        {
                            trackingCount++;
                            // Skip locations not in logic
                            if (APState.TrackedMode == TrackerMode.Logical && !InLogic(locID))
                            {
                                continue;
                            }
                            dist = Vector3.Distance(playerPos, ArchipelagoData.Locations[locID].Position);
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
                        APState.TrackedDepth =
                            Convert.ToInt32(ArchipelagoData.Locations[closestID].Position.y);
                        APState.TrackedLocationName =
                            APState.Session.Locations.GetLocationNameFromId(APState.TrackedLocation);
                        Vector3 directionVector = ArchipelagoData.Locations[closestID].Position - 
                                                  Player.main.gameObject.transform.position;
                        directionVector.Normalize();
                        APState.TrackedAngle = Vector3.Angle(directionVector, 
                            Player.main.viewModelCamera.transform.forward);
                    }
                }
                else
                {
                    APState.TrackedLocationsCount = 0;
                    APState.TrackedLocation = -1;
                }
                // fish
                if (APState.Session != null)
                {

                    var remainingFish = new List<long>();

                    foreach (var locID in APState.Session.Locations.AllMissingLocations)
                    {
                        // Check that it's a static location
                        if (locID > scanCutOff)
                        {
                            remainingFish.Add(locID);
                        }
                    }

                    APState.TrackedFishCount = remainingFish.Count;
                    if (APState.TrackedFishCount != 0)
                    {
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
                    else
                    {
                        APState.TrackedFish = "";
                    }
                }
                else
                {
                    APState.TrackedFishCount = 0;
                }

                Thread.Sleep(150);
            }
        }
    }
}
