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
        public List<GameObject> AuroraBlockers = new List<GameObject>();
        public bool SpawnsComplete = false;

        //Add a handler to the Player object if not present, then spawn necessary items
        //Will only do so if a Player object has been instantiated and a valid AP connection is present
        public static void AddHandlerAndSpawnBarriers()
        {
            Player IdentifiedPlayer = (Player)UnityEngine.Object.FindObjectOfType(typeof(Player));
            if ((IdentifiedPlayer != null) && (APState.Session != null))
            {
                if (!IdentifiedPlayer.gameObject.TryGetComponent<APSpawnHandler>(out APSpawnHandler SpawnMaker))
                {
                    SpawnMaker = IdentifiedPlayer.gameObject.AddComponent<APSpawnHandler>();
                }
                SpawnMaker.AttemptSpawns();
            }
        }

        //Attempt to spawn blockades, but only do so if it has not already been done
        public void AttemptSpawns()
        {
            if (!SpawnsComplete) {
                if (APState.PropulsionCannonLogic == "strictrequirement")
                {
                    APSpawnHandler.SpawnAuroraBlockers(this.AuroraBlockers);
                }
                SpawnsComplete = true;
            }
        }

        //This function spawns a barricade on the upper Aurora entrance.
        //Does not spawn if the associated list has already been added to.
        public static void SpawnAuroraBlockers(List<GameObject> AuroraBlockers)
        {
            Vector3 blockRotation = new Vector3(0.0f, 232.0f, 0.0f);
            Vector3 blockA1SpawnPoint = new Vector3(997.75f, 36.70f, 109.0f);
            Vector3 blockB1SpawnPoint = new Vector3(998.4f, 36.7f, 108.0f);
            Vector3 blockA2SpawnPoint = new Vector3(997.75f, 37.80f, 109.0f);
            Vector3 blockB2SpawnPoint = new Vector3(998.4f, 37.8f, 108.0f);
            Vector3 blockA3SpawnPoint = new Vector3(997.75f, 38.90f, 109.0f);
            Vector3 blockB3SpawnPoint = new Vector3(998.4f, 38.9f, 108.0f);
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockA1SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockB1SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockA2SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockB2SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockA3SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
            UWE.CoroutineHost.StartCoroutine(ListNewGameObject(blockB3SpawnPoint, blockRotation,
                TechType.StarshipCargoCrate, AuroraBlockers));
        }

        //This function generates a new game object, assigns it a position and rotation, and adds them to designated list.
        //Currently, it can only spawn items with valid TechTypes.
       public static IEnumerator ListNewGameObject(Vector3 spawnPosition, Vector3 eulerRotation, TechType objectTechType,
            List<GameObject> objectList)
        {
            CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(objectTechType);
            yield return task;
            GameObject gameObjectPrefab = task.GetResult();
            GameObject gameObject = GameObject.Instantiate(gameObjectPrefab);
            gameObject.transform.position = spawnPosition;
            Quaternion spawnRotation = new Quaternion();
            spawnRotation.eulerAngles = eulerRotation;
            gameObject.transform.rotation = spawnRotation;
            objectList.Add(gameObject);
        }
    }

}