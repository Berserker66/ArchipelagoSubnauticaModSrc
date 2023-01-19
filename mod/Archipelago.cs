using HarmonyLib;
using System;
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


namespace Archipelago
{
    public class ArchipelagoUI : MonoBehaviour
    {
#if DEBUG
        public static string mouse_target_desc = "";
        private bool show_warps = false;
        private bool show_items = false;
        private float copied_fade = 0.0f;

        public static Dictionary<string, Vector3> WRECKS = new Dictionary<string, Vector3>
        {
            { "Blood Kelp Trench 1", new Vector3(-1201, -324, -396) },
            { "Bulb Zone 1", new Vector3(929, -198, 593) },
            { "Bulb Zone 2", new Vector3(1309, -215, 570) },
            { "Dunes 1", new Vector3(-1448, -332, 723) },
            { "Dunes 2", new Vector3(-1632, -334, 83) },
            { "Dunes 3", new Vector3(-1210, -217, 7) },
            { "Grand Reef 1", new Vector3(-290, -222, -773) },
            { "Grand Reef 2", new Vector3(-865, -430, -1390) },
            { "Grassy Plateaus 1", new Vector3(-15, -96, -624) },
            { "Grassy Plateaus 2", new Vector3(-390, -120, 648) },
            { "Grassy Plateaus 3", new Vector3(286, -72, 444) },
            { "Grassy Plateaus 4", new Vector3(-635, -50, -2) },
            { "Grassy Plateaus 5", new Vector3(-432, -90, -268) },
            { "Kelp Forest 1", new Vector3(-320, -57, 252) },
            { "Kelp Forest 2", new Vector3(65, -25, 385) },
            { "Mountains 1", new Vector3(701, -346, 1224) },
            { "Mountains 2", new Vector3(1057, -254, 1359) },
            { "Northwestern Mushroom Forest", new Vector3(-645, -120, 773) },
            { "Safe Shallows 1", new Vector3(-40, -14, -400) },
            { "Safe Shallows 2", new Vector3(366, -6, -203) },
            { "Sea Treader's Path", new Vector3(-1131, -166, -729) },
            { "Sparse Reef", new Vector3(-787, -208, -713) },
            { "Underwater Islands", new Vector3(-102, -179, 860) }
        };
        void Update()
        {
            if (mouse_target_desc != "")
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
                {
                    Debug.Log("INSPECT GAME OBJECT: " + mouse_target_desc);
                    string id = mouse_target_desc.Split(new char[] { ':' })[0];
                    GUIUtility.systemCopyBuffer = id;
                    copied_fade = 1.0f;
                }
            }
            copied_fade -= Time.deltaTime;
        }
#endif

        void OnGUI()
        {
#if DEBUG
            GUI.Box(new Rect(0, 0, Screen.width, 120), "");
#endif
            string ap_ver = "Archipelago v" + APState.AP_VERSION[0] + "." + APState.AP_VERSION[1] + "." + APState.AP_VERSION[2];
            if (APState.Session != null)
            {
                GUI.Label(new Rect(16, 16, 300, 20), ap_ver + " Status: Connected");
            }
            else
            {
                GUI.Label(new Rect(16, 16, 300, 20), ap_ver + " Status: Not Connected");
            }

            if ((APState.Session == null || !APState.Authenticated) && APState.state == APState.State.Menu)
            {
                GUI.Label(new Rect(16, 36, 150, 20), "Host: ");
                GUI.Label(new Rect(16, 56, 150, 20), "PlayerName: ");
                GUI.Label(new Rect(16, 76, 150, 20), "Password: ");

                bool submit = false;
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                {
                    submit = true;
                }

                APState.ServerData.host_name = GUI.TextField(new Rect(150 + 16 + 8, 36, 150, 20), 
                    APState.ServerData.host_name);
                APState.ServerData.slot_name = GUI.TextField(new Rect(150 + 16 + 8, 56, 150, 20), 
                    APState.ServerData.slot_name);
                APState.ServerData.password = GUI.TextField(new Rect(150 + 16 + 8, 76, 150, 20), 
                    APState.ServerData.password);

                if (submit && Event.current.type == EventType.KeyDown)
                {
                    // The text fields have not consumed the event, which means they were not focused.
                    submit = false;
                }

                if ((GUI.Button(new Rect(16, 96, 100, 20), "Connect") || submit) && APState.ServerData.Valid)
                {
                    APState.Connect();
                }
            }
            else if (APState.state == APState.State.InGame && APState.Session != null && Player.main != null)
            {
                
                if (APState.TrackedLocation != -1 && APState.TrackedMode != TrackerMode.Disabled)
                {
                    string text = "Locations left: " + APState.TrackedLocationsCount;
                    if (APState.TrackedLocation != -1)
                    {
                        text += ". Closest is " + (long)APState.TrackedDistance + " m away, named " +
                                APState.TrackedLocationName;
                    }
                    GUI.Label(new Rect(16, 36, 1000, 20), text);
                    
                    // TODO: find a way to display this
                    //GUI.Label(new Rect(16, 56, 1000, 20), 
                    //    APState.TrackedAngle.ToString());
                }

                if (APState.TrackedFishCount > 0 && APState.TrackedMode != TrackerMode.Disabled)
                {
                    GUI.Label(new Rect(16, 56, 1000, 22), 
                        "Fish left: "+APState.TrackedFishCount + ". Such as: "+APState.TrackedFish);
                }

                if (EscapePod.main != null &&
                    (EscapePod.main.transform.position - Player.main.transform.position).magnitude < 10f)
                {
                    GUI.Label(new Rect(16, 76, 1000, 22), 
                        "Goal: "+APState.Goal);
                }
            }

#if DEBUG
            GUI.Label(new Rect(16, 16 + 20, Screen.width - 32, 50), ((copied_fade > 0.0f) ? "Copied!" : "Target: ") + mouse_target_desc);

            if (APState.state != APState.State.Menu)
            {
                if (GUI.Button(new Rect(16, 16 + 25 + 8 + 25 + 8, 150, 25), "Activate Cheats"))
                {
                    DevConsole.SendConsoleCommand("nodamage");
                    DevConsole.SendConsoleCommand("oxygen");
                    DevConsole.SendConsoleCommand("item seaglide");
                    DevConsole.SendConsoleCommand("item battery 10");
                    DevConsole.SendConsoleCommand("fog");
                    DevConsole.SendConsoleCommand("speed 3");
                }
                if (GUI.Button(new Rect(16 + 150 + 8, 16 + 25 + 8 + 25 + 8, 150, 25), "Warp to Locations"))
                {
                    show_warps = !show_warps;
                    if (show_warps) show_items = false;
                }
                if (GUI.Button(new Rect(16 + 150 + 8 + 150 + 8, 16 + 25 + 8 + 25 + 8, 150, 25), "Items"))
                {
                    show_items = !show_items;
                    if (show_items) show_warps = false;
                }

                if (show_warps)
                {
                    int i = 0;
                    int j = 125;
                    foreach (var kv in WRECKS)
                    {
                        if (GUI.Button(new Rect(16 + i, j, 200, 25), kv.Key.ToString()))
                        {
                            string target = ((int)kv.Value.x).ToString() + " " +
                                            ((int)kv.Value.y).ToString() + " " +
                                            ((int)kv.Value.z + 50).ToString();
                            DevConsole.SendConsoleCommand("warp " + target);
                        }
                        j += 30;
                        if (j + 30 >= Screen.height)
                        {
                            j = 125;
                            i += 200 + 16;
                        }
                    }
                }

                if (show_items)
                {
                    int i = 0;
                    int j = 125;
                    foreach (var kv in APState.ITEM_CODE_TO_TECHTYPE)
                    {
                        if (GUI.Button(new Rect(16 + i, j, 200, 25), kv.Value.ToString()))
                        {
                            APState.unlock(kv.Value);
                        }
                        j += 30;
                        if (j + 30 >= Screen.height)
                        {
                            j = 125;
                            i += 200 + 16;
                        }
                    }
                }
            }
#endif
        }

        private void Start()
        {
            RegisterCmds();
        }

        public void RegisterCmds()
        {
            DevConsole.RegisterConsoleCommand(this, "say", false, false);
            DevConsole.RegisterConsoleCommand(this, "silent", false, false);
            DevConsole.RegisterConsoleCommand(this, "tracker", false, false);
            DevConsole.RegisterConsoleCommand(this, "deathlink", false, false);
            DevConsole.RegisterConsoleCommand(this, "apdebug", false, false);
        }

        private void OnConsoleCommand_say(NotificationCenter.Notification n)
        {
            string text = "";

            for (var i = 0; i < n.data.Count; i++)
            {
                text += (string)n.data[i];
                if (i < n.data.Count - 1) text += " ";
            }
            // Cannot type the '!' character in subnautica console, will use / instead and replace them
            text = text.Replace('/', '!');
            
            if (APState.Session != null && APState.Authenticated)
            {
                var packet = new SayPacket();
                packet.Text = text;
                APState.Session.Socket.SendPacket(packet);
            }
            else
            {
                Debug.Log("Can only 'say' while connected to Archipelago.");
                ErrorMessage.AddMessage("Can only 'say' while connected to Archipelago.");
            }
        }
        private void OnConsoleCommand_silent(NotificationCenter.Notification n)
        {
            APState.Silent = !APState.Silent;
            
            if (APState.Silent)
            {
                Debug.Log("Muted Archipelago chat.");
                ErrorMessage.AddMessage("Muted Archipelago chat.");
            }
            else
            {
                Debug.Log("Enabled Archipelago chat.");
                ErrorMessage.AddMessage("Enabled Archipelago chat.");
            }
        }
        private void OnConsoleCommand_tracker(NotificationCenter.Notification n)
        {
            switch (APState.TrackedMode)
            {
                case TrackerMode.Disabled:
                    APState.TrackedMode = TrackerMode.Closest;
                    Debug.Log("Tracking Locations by proximity.");
                    ErrorMessage.AddMessage("Tracking Locations by proximity.");
                    break;
                case TrackerMode.Closest:
                    APState.TrackedMode = TrackerMode.Logical;
                    Debug.Log("Tracking Locations by proximity, additionally filtering by " +
                              "Laser Cutter, Radiation Suit and Propulsion Cannon.");
                    ErrorMessage.AddMessage("Tracking Locations by proximity, additionally filtering by " +
                                            "Laser Cutter, Radiation Suit and Propulsion Cannon.");
                    break;
                case TrackerMode.Logical:
                    APState.TrackedMode = TrackerMode.Disabled;
                    Debug.Log("Location tracking disabled.");
                    ErrorMessage.AddMessage("Location tracking disabled.");
                    break;
            }
        }
        private void OnConsoleCommand_deathlink(NotificationCenter.Notification n)
        {
            APState.ServerData.death_link = !APState.ServerData.death_link;
            APState.set_deathlink();
            
            if (APState.ServerData.death_link)
            {
                Debug.Log("Enabled DeathLink.");
                ErrorMessage.AddMessage("Enabled DeathLink.");
            }
            else
            {
                Debug.Log("Disabled DeathLink.");
                ErrorMessage.AddMessage("Disabled DeathLink.");
            }
        }

        private void OnConsoleCommand_apdebug(NotificationCenter.Notification n)
        {
            //var loc = APState.TrackedLocation;
            //var loc_data = APState.LOCATIONS[loc];
            //DevConsole.SendConsoleCommand("warp "+(int)loc_data.Position.x+" "+(int)loc_data.Position.y+" "+(int)loc_data.Position.z);
            
            //Debug.LogError("Analysis:");
            //string json = JsonConvert.SerializeObject(Player.main.pdaData.analysisTech);
            //Debug.LogError(json);
        }
    }

    // Remove scannable fragments as they spawn, we will unlock them from Databoxes, PDAs and Terminals.
    [HarmonyPatch(typeof(ResourceTracker))]
    [HarmonyPatch("Start")]
    internal class ResourceTracker_Start_Patch
    {
        [HarmonyPostfix]
        public static void RemoveFragment(ResourceTracker __instance, TechType ___techType)
        {

            if (___techType == TechType.Fragment)
            {
                var techTag = __instance.GetComponent<TechTag>();
                if (techTag != null)
                {
                    if (APState.TechFragmentsToDestroy.Contains(techTag.type))
                    {
                        UnityEngine.Object.Destroy(__instance.gameObject);
                    }
                }
                else
                {
                    UnityEngine.Object.Destroy(__instance.gameObject); // No techtag, so it's just "fragment", remove it...
                }
            }
            else if (APState.TechFragmentsToDestroy.Contains(___techType)) // Not fragment, but could be one of the others
            {
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(PDAScanner))]
    [HarmonyPatch("UpdateTarget")]
    internal class PDAScanner_UpdateTarget_Patch
    {
        [HarmonyPostfix]
        public static void MakeUnscanable()
        {
            if (PDAScanner.scanTarget.gameObject)
            {
                var tech_tag = PDAScanner.scanTarget.gameObject.GetComponent<TechTag>();
                if (tech_tag != null)
                {
                    if (APState.TechFragmentsToDestroy.Contains(tech_tag.type))
                    {
                        PDAScanner.scanTarget.Invalidate();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BlueprintHandTarget))]
    [HarmonyPatch("Start")]
    internal class BlueprintHandTarget_Start_Patch
    {
        // Using TechType.None gives 2 titanium we don't want that
        [HarmonyPrefix] 
        public static void ReplaceDataboxContent(BlueprintHandTarget __instance)
        {
            // needs to be a unique not taken ID
            __instance.unlockTechType = (TechType)__instance.transform.position.x+100000;
        }
    }

    [HarmonyPatch(typeof(DataboxSpawner))]
    [HarmonyPatch("Start")]
    internal class DataboxSpawner_Start_Patch
    {
        
        [HarmonyPrefix]
        public static bool AlwaysSpawn(DataboxSpawner __instance, ref IEnumerator __result)
        {
            __result = AddressablesUtility.InstantiateAsync(__instance.databoxPrefabReference.RuntimeKey as string, 
                __instance.transform.parent, __instance.transform.localPosition, __instance.transform.localRotation);
            return false;
        }
    }

    // Once databox clicked, send it to Archipelago
    [HarmonyPatch(typeof(BlueprintHandTarget))]
    [HarmonyPatch("UnlockBlueprint")]
    internal class BlueprintHandTarget_UnlockBlueprint_Patch
    {
        [HarmonyPrefix]
        public static void OpenDatabox(BlueprintHandTarget __instance)
        {
            if (!__instance.used)
            {
                APState.CheckLocation(__instance.gameObject.transform.position);
            }
        }
    }

    // Once PDA clicked, send it to Archipelago.
    [HarmonyPatch(typeof(StoryHandTarget))]
    [HarmonyPatch("OnHandClick")]
    internal class StoryHandTarget_OnHandClick_Patch
    {
        [HarmonyPrefix]
        public static bool PickupPDA(StoryHandTarget __instance)
        {
            if (APState.CheckLocation(__instance.gameObject.transform.position))
            {
                var generic_console = __instance.gameObject.GetComponent<GenericConsole>();
                if (generic_console != null)
                {
                    // Change its color
                    generic_console.gotUsed = true;

                    var UpdateState_method = typeof(GenericConsole).GetMethod("UpdateState", BindingFlags.NonPublic | BindingFlags.Instance);
                    UpdateState_method.Invoke(generic_console, new object[] { });

                    return false; // Don't let the item in the console be given. (Like neptune blueprint)
                }
            }
            return true;
        }
    }

    // There are 3 pickupable modules in the game
    [HarmonyPatch(typeof(Pickupable))]
    [HarmonyPatch("OnHandClick")]
    internal class Pickupable_OnHandClick_Patch
    {
        [HarmonyPrefix]
        public static bool PickModule(Pickupable __instance)
        {
            if (APState.CheckLocation(__instance.gameObject.transform.position))
            {
                var tech_tag = __instance.gameObject.GetComponent<TechTag>();
                if (tech_tag != null)
                {
                    if (tech_tag.type == TechType.VehicleHullModule1 ||
                        tech_tag.type == TechType.VehicleStorageModule ||
                        tech_tag.type == TechType.PowerUpgradeModule)
                    {
                        // Don't let the module in the console be given
                        UnityEngine.Object.Destroy(__instance.gameObject);
                        return false;
                    }
                }
            }
            return true;
        }
    }

#if DEBUG
    [HarmonyPatch(typeof(KnownTech))]
    [HarmonyPatch("Initialize")]
    internal class PrintCascadeTechs
    {
        [HarmonyPostfix]
        public static void PrintCascade(List<KnownTech.AnalysisTech> ___analysisTech)
        {
            foreach (KnownTech.AnalysisTech tech in ___analysisTech)
            { 
                Debug.LogError(tech.techType + " -> " + JsonConvert.SerializeObject(tech.unlockTechTypes));
            }
        }
    }
#endif
    
    [HarmonyPatch(typeof(MainGameController))]
    [HarmonyPatch("LoadInitialInventoryAsync")]
    internal class MainGameController_LoadInitialInventoryAsync_Patch
    {
        [HarmonyPostfix]
        public static void GameReady()
        {
            // Make sure the say command is registered
            APState.ArchipelagoUI.RegisterCmds();
        }
    }
    
    [HarmonyPatch(typeof(SaveLoadManager.GameInfo))]
    [HarmonyPatch("SaveIntoCurrentSlot")]
    internal class GameInfo_SaveIntoCurrentSlot_Patch
    {
        [HarmonyPostfix]
        public static void SaveIntoCurrentSlot(SaveLoadManager.GameInfo info)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(APState.ServerData));
            Platform.IO.File.WriteAllBytes(Platform.IO.Path.Combine(SaveLoadManager.GetTemporarySavePath(), 
                "archipelago.json"), bytes);
        }
    }
    
    [HarmonyPatch(typeof(SaveLoadManager))]
    [HarmonyPatch("SetCurrentSlot")]
    internal class SaveLoadManager_SetCurrentSlot_Patch
    {
        [HarmonyPostfix]
        public static void LoadArchipelagoState(string _currentSlot)
        {
            var storage = PlatformUtils.main.GetServices().GetUserStorage() as UserStoragePC;
            var rawPath = storage.GetType().GetField("savePath",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(storage);
            var path = Platform.IO.Path.Combine((string)rawPath, _currentSlot);

            path = Platform.IO.Path.Combine(path, "archipelago.json");
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    APState.ServerData = JsonConvert.DeserializeObject<APData>(reader.ReadToEnd());
                    
                    if (APState.Connect() && APState.ServerData.@checked != null)
                    {
                        APState.Session.Locations.CompleteLocationChecks(APState.ServerData.@checked.ToArray());
                    }
                    else
                    {
                        ErrorMessage.AddError("Null Checked");
                    }
                }
            }
            // compat handling, remove later
            else if (APState.archipelago_indexes.ContainsKey(_currentSlot))
            {
                APState.ServerData.index = APState.archipelago_indexes[_currentSlot];
            }
            else
            {
                APState.ServerData.index = 0;
            }
        }
    }

    [HarmonyPatch(typeof(MainGameController))]
    [HarmonyPatch("OnDestroy")]
    internal class MainGameController_OnDestroy_Patch
    {
        [HarmonyPostfix]
        public static void GameClosing()
        {
            APState.state = APState.State.Menu;
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager))]
    [HarmonyPatch("RegisterSaveGame")]
    internal class SaveLoadManager_RegisterSaveGame_Patch
    {
        [HarmonyPrefix]
        public static void RegisterSaveGame(string slotName, UserStorageUtils.LoadOperation loadOperation)
        {
            if (loadOperation.GetSuccessful())
            {
                byte[] jsonData = null;
                if (loadOperation.files.TryGetValue("gameinfo.json", out jsonData))
                {
                    try
                    {
                        var json_string = Encoding.UTF8.GetString(jsonData);
                        var splits = json_string.Split(new char[] { ',' });
                        var last = splits[splits.Length - 1];
                        splits = last.Split(new char[] { ':' });
                        var name = splits[0];
                        name = name.Substring(1, name.Length - 2);
                        splits = splits[1].Split(new char[] { '}' });
                        var value = splits[0];

                        if (name == "archipelago_item_index")
                        {
                            var index = int.Parse(value);
                            APState.archipelago_indexes[slotName] = index;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("archipelago_item_index error: " + e.Message);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MainGameController))]
    [HarmonyPatch("Update")]
    internal class MainGameController_Update_Patch
    {
        private static bool IsSafeToUnlock()
        {
            if (APState.unlock_dequeue_timeout > 0.0f)
            {
                return false;
            }

            if (APState.state != APState.State.InGame)
            {
                return false;
            }

            if (LaunchRocket.isLaunching || (EscapePod.main != null && EscapePod.main.IsPlayingIntroCinematic()))
            {
                return false;
            }

            if (PlayerCinematicController.cinematicModeCount > 0 && Time.time - PlayerCinematicController.cinematicActivityExpireTime <= 30f)
            {
                return false;
            }

            return !SaveLoadManager.main.isSaving;
        }

        [HarmonyPostfix]
        public static void DequeueUnlocks()
        {
            const int DEQUEUE_COUNT = 2;
            const float DEQUEUE_TIME = 3.0f;

            if (APState.unlock_dequeue_timeout > 0.0f) APState.unlock_dequeue_timeout -= Time.deltaTime;
            if (APState.message_dequeue_timeout > 0.0f) APState.message_dequeue_timeout -= Time.deltaTime;

            // Print messages
            if (APState.message_dequeue_timeout <= 0.0f)
            {
                // We only do x at a time. To not crowd the on screen log/events too fast
                List<string> to_process = new List<string>();
                while (to_process.Count < DEQUEUE_COUNT && APState.message_queue.Count > 0)
                {
                    to_process.Add(APState.message_queue[0]);
                    APState.message_queue.RemoveAt(0);
                }
                foreach (var message in to_process)
                {
                    ErrorMessage.AddMessage(message);
                }
                APState.message_dequeue_timeout = DEQUEUE_TIME;
            }

            // Do unlocks
            if (IsSafeToUnlock())
            {
                if (APState.ServerData.index < APState.Session.Items.AllItemsReceived.Count)
                {
                    APState.Unlock(APState.ITEM_CODE_TO_TECHTYPE[
                        APState.Session.Items.AllItemsReceived[Convert.ToInt32(APState.ServerData.index)].Item
                    ]);
                    APState.ServerData.index++;
                    // We only do x at a time. To not crowd the on screen log/events too fast
                    APState.unlock_dequeue_timeout = DEQUEUE_TIME;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuController))]
    [HarmonyPatch("Start")]
    internal class MainMenuController_Start_Patch
    {
        [HarmonyPostfix]
        public static void CreateArchipelagoUI()
        {
            // Create a game object that will be responsible to drawing the IMGUI in the Menu.
            var guiGameobject = new GameObject();
            APState.ArchipelagoUI = guiGameobject.AddComponent<ArchipelagoUI>();
            GameObject.DontDestroyOnLoad(guiGameobject);
            var storage = PlatformUtils.main.GetServices().GetUserStorage() as UserStoragePC;
            var rawPath = storage.GetType().GetField("savePath",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(storage);
            var lastConnectInfo = APLastConnectInfo.LoadFromFile(rawPath + "/archipelago_last_connection.json");
            if (lastConnectInfo != null)
            {
                APState.ServerData.FillFromLastConnect(lastConnectInfo);
            }
        }
    }

#if DEBUG
    [HarmonyPatch(typeof(GUIHand))]
    [HarmonyPatch("OnUpdate")]
    internal class GUIHand_OnUpdate_Patch
    {
        [HarmonyPostfix]
        public static void OnUpdate(GUIHand __instance)
        {
            var active_target = __instance.GetActiveTarget();
            if (active_target)
                ArchipelagoUI.mouse_target_desc = APState.InspectGameObject(active_target.gameObject);
            else if (PDAScanner.scanTarget.gameObject)
                ArchipelagoUI.mouse_target_desc = APState.InspectGameObject(PDAScanner.scanTarget.gameObject);
            else
                ArchipelagoUI.mouse_target_desc = "";
        }
    }
#endif

    //[HarmonyPatch(typeof(LeakingRadiation))]
    //[HarmonyPatch("Start")]
    //internal class LeakingRadiation_StopIntroCinematic_Patch
    //{
    //    [HarmonyPostfix]
    //    public static void PrintRad(LeakingRadiation __instance)
    //    {
    //        ErrorMessage.AddError("Radiation max: " + __instance.kMaxRadius + " at " + __instance.gameObject.transform.position.ToString());
    //    }
    //}

    // Ship start already exploded
    [HarmonyPatch(typeof(EscapePod))]
    [HarmonyPatch("StopIntroCinematic")]
    internal class EscapePod_StopIntroCinematic_Patch
    {
        [HarmonyPostfix]
        public static void GameReady(EscapePod __instance)
        {
            DevConsole.SendConsoleCommand("explodeship");
            APState.ServerData.index = 0; // New game detected
        }
    }

    // Advance rocket stage, but don't add to known tech the next stage! We'll find them in the world
    [HarmonyPatch(typeof(Rocket))]
    [HarmonyPatch("AdvanceRocketStage")]
    internal class Rocket_AdvanceRocketStage_Patch
    {
        [HarmonyPrefix]
        static public bool AdvanceRocketStage(Rocket __instance)
        {
            __instance.currentRocketStage++;
            if (__instance.currentRocketStage == 5)
            {
                var isFinishedMember = typeof(Rocket).GetField("isFinished", BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.Instance);
                isFinishedMember.SetValue(__instance, true);

                var IsAnyRocketReadyMember = typeof(Rocket).GetProperty("IsAnyRocketReady", BindingFlags.Static);
                IsAnyRocketReadyMember.SetValue(null, true);
            }
            //KnownTech.Add(__instance.GetCurrentStageTech(), true); // This is the part we don't want

            return false;
        }
    }

    [HarmonyPatch(typeof(RocketConstructor))]
    [HarmonyPatch("StartRocketConstruction")]
    internal class RocketConstructor_StartRocketConstruction_Patch
    {
        [HarmonyPrefix]
        public static bool StartRocketConstruction(RocketConstructor __instance)
        {
            TechType currentStageTech = __instance.rocket.GetCurrentStageTech();
            if (!KnownTech.Contains(currentStageTech))
            {
                return false;
            }

            return true;
        }
    }

    // Prevent aurora explosion story event to give a radiationsuit...
    [HarmonyPatch(typeof(Story.UnlockBlueprintData))]
    [HarmonyPatch("Trigger")]
    internal class UnlockBlueprintData_Trigger_Patch
    {
        [HarmonyPrefix]
        public static bool PreventRadiationSuitUnlock(Story.UnlockBlueprintData __instance)
        {
            if (__instance.techType == TechType.RadiationSuit)
            {
                return false;
            }
            return true;
        }
    }

    // When launching the rocket, send goal achieved to archipelago
    [HarmonyPatch(typeof(LaunchRocket))]
    [HarmonyPatch("SetLaunchStarted")]
    internal class LaunchRocket_SetLaunchStarted_Patch
    {
        [HarmonyPrefix]
        public static void SetLaunchStarted()
        {
            APState.send_completion();
        }
    }
    [HarmonyPatch(typeof(StoryGoalCustomEventHandler))]
    [HarmonyPatch("NotifyGoalComplete")]
    internal class StoryGoalCustomEventHandler_NotifyGoalComplete_Patch
    {
        [HarmonyPrefix]
        public static void NotifyGoalComplete(string key)
        {
            if (key == APState.GoalEvent)
            {
                APState.send_completion();
            }
        }
    }
    [HarmonyPatch(typeof(PDAEncyclopedia), "Add", typeof(string), typeof(PDAEncyclopedia.Entry), typeof(bool))]
    internal class CustomPDA
    {
        [HarmonyPostfix]
        public static void Add(string key, PDAEncyclopedia.Entry entry)
        {
            if (APState.Encyclopdia.TryGetValue(key, out var id))
            {
                APState.SendLocID(id);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnKill")]
    internal class CustomPlayerKill
    {
        [HarmonyPostfix]
        public static void PlayerDeath(DamageType damageType)
        {
            if (!APState.DeathLinkKilling)
            {
                if (APState.ServerData.death_link)
                {
                    APState.DeathLinkService.SendDeathLink(new DeathLink(APState.ServerData.slot_name));
                }
            }
            APState.DeathLinkKilling = false;
        }
    }
}
