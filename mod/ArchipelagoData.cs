using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Archipelago;

public enum ArchipelagoItemType
{
    Technology = 1,
    Resource = 2,
    Group = 3,
}

// Data coming Archipelago Export
public static class ArchipelagoData
{
    public static bool Initialized;
    public static Dictionary<string, long> Encyclopdia;
    public static Dictionary<TechType, List<long>> LogicDict;
    public static Dictionary<long, TechType> ItemCodeToTechType = new ();
    public static Dictionary<long, APState.Location> Locations = new ();
    public static Dictionary<long, List<long>> GroupItems = new ();
    public static Dictionary<long, ArchipelagoItemType> ItemCodeToItemType = new();

    public static T ReadJSON<T>(string filename)
    {
        try
        {
            var reader = File.OpenText(BepInEx.Paths.PluginPath+"/Archipelago/" + filename + ".json");
            var content = reader.ReadToEnd();
            reader.Close();
            var data = JsonConvert.DeserializeObject<T>(content);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError("Could not read " + filename + ".json\n" + e);
            return default;
        }
    }

    public static void Init()
    {
        if (Initialized)
        {
            return;
        }
        
        // Load items.json
        var itemsData = ReadJSON<Dictionary<long, string>>("items");
        foreach (var itemJson in itemsData)
        {
            // not all tech types exist in both games
            var success = Enum.TryParse(itemJson.Value, out TechType tech);
            if (success)
            {
                ItemCodeToTechType[itemJson.Key] = tech;
            }
        }
        
        // Load group_items.json
        GroupItems = ReadJSON<Dictionary<long, List<long>>>("group_items");

        // Load locations.json
        var locationsData = ReadJSON<Dictionary<long, Dictionary<string, float>>>("locations");
        foreach (var locationJson in locationsData)
        {
            APState.Location location = new APState.Location();
            location.ID = locationJson.Key;
            var vec = locationJson.Value;
            location.Position = new Vector3(
                vec["x"],
                vec["y"],
                vec["z"]
            );
            Locations.Add(location.ID, location);
        }

        // Load encyclopedia.json
        Encyclopdia = ReadJSON<Dictionary<string, long>>("encyclopedia");
        
        // Load logic.json
        LogicDict = ReadJSON<Dictionary<TechType, List<long>>>("logic");
        
        // Load item_types.json
        var itemTypesData = ReadJSON<Dictionary<ArchipelagoItemType, List<long>>>("item_types");

        foreach (var type in itemTypesData)
        {
            foreach (var itemCode in type.Value)
            {
                ItemCodeToItemType[itemCode] = type.Key;
            }
        }

        Debug.LogError("ItemCodeToItemType " + JsonConvert.SerializeObject(ItemCodeToItemType));
        Initialized = true;
    }

    //Alters the location data stored in LogicDict to conform to selected choices
    public static void UpdateLogicData()
    {
        if (APState.PropulsionCannonLogic == "no_requirement")
        {
            var LogicPatch = ReadJSON<Dictionary<int, List<long>>>("logic_revisions");
            foreach (var logic_change in LogicPatch)
            {
                //Logic patches 1 and 2 remove propulsion cannon requirement from a location
                if (logic_change.Key == 1 || logic_change.Key == 2) {
                    foreach (var loc in logic_change.Value)
                    {
                        ArchipelagoData.LogicDict[TechType.PropulsionCannon].Remove(loc);
                    }
                }
                //Logic patch 2 also adds laser cutter requirement
                if (logic_change.Key == 2)
                {
                    foreach (var loc in logic_change.Value)
                    {
                        ArchipelagoData.LogicDict[TechType.LaserCutter].Add(loc);
                    }
                }
            }
        }
    }
}
