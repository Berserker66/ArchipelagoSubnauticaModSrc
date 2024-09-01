using System;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Archipelago.MultiClient.Net.Packets;
using System.Text;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;
using File = System.IO.File;
using Object = UnityEngine.Object;

namespace Archipelago
{
    //This class handles features that involve spawning in objects to alter progression.
    public class APSpawnHandler : MonoBehaviour
    {
        public bool SpawnsComplete = false;

        //This method attempts to locate an APSpawnHandler component attached to a player.
        //If a player exists but does not have an APSpawnHandler, a new one will be created.
        //If a player does not exist/has not loaded, returns null.
        public static APSpawnHandler FindSpawnHandler()
        {
            Player IdentifiedPlayer = (Player)UnityEngine.Object.FindObjectOfType(typeof(Player));
            if (IdentifiedPlayer != null)
            {
                if (!IdentifiedPlayer.gameObject.TryGetComponent<APSpawnHandler>(out APSpawnHandler SpawnMaker))
                {
                    SpawnMaker = IdentifiedPlayer.gameObject.AddComponent<APSpawnHandler>();
                }
                return SpawnMaker;
            }
            return null;
        }

        //Confirms that a valid player exists, that a valid AP connection exists, and that spawning has not
        //already been completed. If each condition is true, triggers spawn routine.
        public static void CheckConditionsForSpawn()
        {
            APSpawnHandler spawnHandler = FindSpawnHandler();
            if ((spawnHandler != null) && (APState.Session != null))
            {
                if (!spawnHandler.SpawnsComplete)
                {
                    spawnHandler.SpawnAllBlockers();
                }
            }
        }

        //Spawn all blocking items called for by the AP connection.
        public void SpawnAllBlockers()
        {
            // Load blockers.json
            List<BlockerDataSet> BlockerList = ArchipelagoData.ReadJSON<List<BlockerDataSet>>("blockers");

            if (APState.PropulsionCannonLogic == "strict_requirement")
            {
                SpawnBlockersInRegion(BlockerList, "AuroraUpper");
            }
            SpawnsComplete = true;
        }

        //This function sweeps the blocker list for any blockers matching the chosen region and spawns them.
        public static void SpawnBlockersInRegion(List<BlockerDataSet> BlockerList, String region)
        {
            foreach (BlockerDataSet blocker in BlockerList)
            {
                UWE.CoroutineHost.StartCoroutine(SpawnNewGameObject(blocker.position, blocker.rotation,
                TechType.StarshipCargoCrate, blocker.region));
            }
        }

        //This function generates a new game object, assigns it a position and rotation, and attaches an APSpawnRule to them.
        //Currently, it can only spawn items with valid TechTypes.
        public static IEnumerator SpawnNewGameObject(Vector3 spawnPosition, Vector3 eulerRotation, TechType objectTechType,
            String region)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(objectTechType);
            yield return task;
            GameObject gameObjectPrefab = task.GetResult();
            GameObject gameObject = GameObject.Instantiate(gameObjectPrefab);
            gameObject.transform.position = spawnPosition;
            Quaternion spawnRotation = new Quaternion();
            spawnRotation.eulerAngles = eulerRotation;
            gameObject.transform.rotation = spawnRotation;
            APSpawnRule objectRule = gameObject.AddComponent<APSpawnRule>();
            objectRule.region = region;
        }
    }

    //This class attaches as a component of spawned items, in order to track and manage useful information about them.
    //Not currently relevant, but potentially useful going forward.
    internal class APSpawnRule : MonoBehaviour
    {
        public string region;
    }

    //This class allows for deserialization of blocker data from the relevant json.
    //It is used to spawn in blocking objects.
    public class BlockerDataSet (Vector3 position, Vector3 rotation, String region)
    {
        public Vector3 position = position;
        public Vector3 rotation = rotation;
        public String region = region;
    }



}