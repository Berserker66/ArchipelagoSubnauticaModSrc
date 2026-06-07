using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Archipelago
{

    public static class APState
    {
        public struct Location
        {
            public long ID;
            public Vector3 Position;
        }

        public enum State
        {
            Menu,
            InGame
        }

        public static Dictionary<string, string> GoalMapping = new Dictionary<string, string>()
        {
            { "free", "Goal_Disable_Gun" },
            { "drive", "AuroraRadiationFixed" },
            { "infected", "Infection_Progress4" },
        };
        
        public static int[] AP_VERSION = new int[] { 0, 6, 6 };
        public static APConnectInfo ServerConnectInfo = new APConnectInfo();
        
        private static DeathLinkService _deathLinkService = null;
        public static DeathLinkService DeathLinkService
        {
            get => _deathLinkService;
            set
            {
                if (_deathLinkService != null)
                {
                    _deathLinkService.OnDeathLinkReceived -= DeathLinkReceived;
                }
                _deathLinkService = value;
                if (_deathLinkService != null)
                {
                    _deathLinkService.OnDeathLinkReceived += DeathLinkReceived;
                }
            }
        }
        
        public static bool DeathLinkKilling = false; // indicates player is currently getting DeathLinked
        public static Dictionary<string, int> archipelago_indexes = new ();
        public static float unlock_dequeue_timeout = 0.0f;
        public static ConcurrentQueue<string> message_queue = new ();
        public static float message_dequeue_timeout = 0.0f;
        public static State state = State.Menu;
        public static bool Authenticated;
        public static string Goal = "launch";
        public static string GoalEvent = "";
        public static string SwimRule = "";
        public static bool EmptyTanks = true;
        public static bool FreeSamples;
        public static bool Silent = false;
        public static Thread TrackerProcessing;
        public static long TrackedLocationsCount = 0;
        public static long TrackedFishCount = 0;
        public static string TrackedFish = "";
        public static long TrackedLocation = -1;
        public static Vector3 TrackedPos = new Vector3();
        public static string TrackedLocationName;
        public static float TrackedDistance;
        public static float TrackedAngle;

        private static ArchipelagoSession _session;
        public static ArchipelagoSession Session
        {
            get => _session;
            set
            {
                if (_session != null)
                {
                    if (_session.MessageLog != null)
                    {
                        _session.MessageLog.OnMessageReceived -= Session_MessageReceived;
                    }
                    if (_session.Socket != null)
                    {
                        _session.Socket.ErrorReceived -= Session_ErrorReceived;
                        _session.Socket.SocketClosed -= Session_SocketClosed;
                    }
                }
                _session = value;
                if (_session != null)
                {
                    _session.MessageLog.OnMessageReceived += Session_MessageReceived;
                    _session.Socket.ErrorReceived += Session_ErrorReceived;
                    _session.Socket.SocketClosed += Session_SocketClosed;
                }
            }
        }
        
        public static ArchipelagoUI ArchipelagoUI = null;
        
        public static HashSet<TechType> tech_fragments = new HashSet<TechType>
        {
            // scannable
            TechType.SeamothFragment,
            TechType.StasisRifleFragment,
            TechType.ExosuitFragment,
            TechType.TransfuserFragment,
            TechType.TerraformerFragment,
            TechType.ReinforceHullFragment,
            TechType.WorkbenchFragment,
            TechType.PropulsionCannonFragment,
            TechType.BioreactorFragment,
            TechType.ThermalPlantFragment,
            TechType.NuclearReactorFragment,
            TechType.MoonpoolFragment,
            TechType.CyclopsHullFragment,
            TechType.CyclopsBridgeFragment,
            TechType.CyclopsEngineFragment,
            TechType.CyclopsDockingBayFragment,
            TechType.SeaglideFragment,
            TechType.ConstructorFragment,
            TechType.SolarPanelFragment,
            TechType.PowerTransmitterFragment,
            TechType.BaseUpgradeConsoleFragment,
            TechType.BaseObservatoryFragment,
            TechType.BaseWaterParkFragment,
            TechType.RadioFragment,
            TechType.BaseBulkheadFragment,
            TechType.BatteryChargerFragment,
            TechType.PowerCellChargerFragment,
            TechType.ScannerRoomFragment,
            TechType.SpecimenAnalyzerFragment,
            TechType.FarmingTrayFragment,
            TechType.SignFragment,
            TechType.PictureFrameFragment,
            TechType.BenchFragment,
            TechType.PlanterPotFragment,
            TechType.PlanterBoxFragment,
            TechType.PlanterShelfFragment,
            TechType.AquariumFragment,
            TechType.BuilderFragment,
            TechType.LEDLightFragment,
            TechType.TechlightFragment,
            TechType.SpotlightFragment,
            TechType.BaseMapRoomFragment,
            TechType.BaseBioReactorFragment,
            TechType.BaseNuclearReactorFragment,
            TechType.LaserCutterFragment,
            TechType.BeaconFragment,
            TechType.GravSphereFragment,
            TechType.ExosuitDrillArmFragment,
            TechType.ExosuitPropulsionArmFragment,
            TechType.ExosuitGrapplingArmFragment,
            TechType.ExosuitTorpedoArmFragment,
            TechType.ExosuitClawArmFragment,
            // non-destructive scanning
            TechType.BaseRoom,
            TechType.FarmingTray,
            TechType.BaseBulkhead,
            TechType.BasePlanter,
            TechType.Spotlight,
            TechType.BaseObservatory,
            TechType.PlanterBox,
            TechType.BaseWaterPark,
            TechType.StarshipDesk,
            TechType.StarshipChair,
            TechType.StarshipChair3,
            TechType.LabCounter,
            TechType.NarrowBed,
            TechType.Bed1,
            TechType.Bed2,
            TechType.CoffeeVendingMachine,
            TechType.Trashcans,
            TechType.Techlight,
            TechType.BarTable,
            TechType.VendingMachine,
            TechType.SingleWallShelf,
            TechType.WallShelves,
            TechType.Bench,
            TechType.PlanterPot,
            TechType.PlanterShelf,
            TechType.PlanterPot2,
            TechType.PlanterPot3,
            TechType.LabTrashcan,
            TechType.BaseFiltrationMachine
        };
        
        public static TrackerMode TrackedMode = TrackerMode.Logical;
        public static HashSet<TechType> TechFragmentsToDestroy = new HashSet<TechType>();

#if DEBUG
        public static string InspectGameObject(GameObject gameObject)
        {
            string msg = gameObject.transform.position.ToString().Trim() + ": ";

            var tech_tag = gameObject.GetComponent<TechTag>();
            if (tech_tag != null)
            {
                msg += "(" + tech_tag.type.ToString() + ")";
            }

            Component[] components = gameObject.GetComponents(typeof(Component));
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var component_name = components[i].ToString().Split('(').GetLast();
                component_name = component_name.Substring(0, component_name.Length - 1);

                msg += component_name;

                if (component_name == "ResourceTracker")
                {
                    var techTypeMember = typeof(ResourceTracker).GetField("techType", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                    var techType = (TechType)techTypeMember.GetValue(component);
                    msg += $"({techType.ToString()},{((ResourceTracker)component).overrideTechType.ToString()})";
                }

                msg += ", ";
            }

            return msg;
        }
#endif

        public static bool Connect()
        {
            if (Authenticated)
            {
                return true;
            }

            Disconnect();

            if (ServerConnectInfo.host_name is null || ServerConnectInfo.host_name.Length == 0)
            {
                return false;
            }
            
            // Start the archipelago session.
            ServerConnectInfo.host_name = ServerConnectInfo.host_name.Trim();
            // If host_name is all digits, assume it's an archipelago.gg port
            if (ServerConnectInfo.host_name.All(char.IsDigit))
            {
                ServerConnectInfo.host_name = $"wss://archipelago.gg:{ServerConnectInfo.host_name}";
            }
            Session = ArchipelagoSessionFactory.CreateSession(ServerConnectInfo.host_name);

            HashSet<TechType> vanillaTech = new HashSet<TechType>();
            LoginResult loginResult;
            try
            {
                loginResult = Session.TryConnectAndLogin(
                    "Subnautica",
                    ServerConnectInfo.slot_name,
                    ItemsHandlingFlags.AllItems,
                    new Version(AP_VERSION[0], AP_VERSION[1], AP_VERSION[2]),
                    null,
                    "",
                    ServerConnectInfo.password);
            }
            catch (Exception e)
            {
                Authenticated = false;
                Logging.LogError("Connection Error: " + e.Message);
                Disconnect();
                return false;
            }

            if (loginResult is LoginSuccessful loginSuccess)
            {
                var storage = PlatformUtils.main.GetServices().GetUserStorage() as UserStoragePC;
                var rawPath = storage?.GetType().GetField("savePath",
                        BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(storage);
                if (rawPath != null)
                {
                    ServerConnectInfo.GetAsLastConnect().WriteToFile(rawPath + "/archipelago_last_connection.json");
                }
                else
                {
                    Logging.LogError("Could not write most recent connect info to file.");
                }
                
                Authenticated = true;
                state = State.InGame;
                if (loginSuccess.SlotData.TryGetValue("swim_rule", out var swim_rule))
                {
                    SwimRule = (string)swim_rule;
                }
                if (loginSuccess.SlotData.TryGetValue("free_samples", out var free_samples))
                {
                    FreeSamples = Convert.ToInt32(free_samples) > 0;
                }
                if (loginSuccess.SlotData.TryGetValue("empty_tanks", out var empty_tanks))
                {
                    EmptyTanks = Convert.ToInt32(empty_tanks) > 0;
                }
                Goal = (string)loginSuccess.SlotData["goal"];
                GoalMapping.TryGetValue(Goal, out GoalEvent);
                if (loginSuccess.SlotData["vanilla_tech"] is JArray temp)
                {
                    foreach (var tech in temp)
                    {
                        vanillaTech.Add((TechType)Enum.Parse(typeof(TechType), tech.ToString()));
                    }
                }
                    
                
                Logging.Log("SlotData: " + JsonConvert.SerializeObject(loginSuccess.SlotData), ingame:false);
                ServerConnectInfo.death_link = Convert.ToInt32(loginSuccess.SlotData["death_link"]) > 0;
                set_deathlink();

                if (ServerConnectInfo.@checked.Count > 0)
                {
                    Task.Run(() => { Session.Locations.CompleteLocationChecksAsync(ServerConnectInfo.@checked.ToArray()); }).ConfigureAwait(false);
                }
            }
            else if (loginResult is LoginFailure loginFailure)
            {
                Authenticated = false;
                Logging.LogError("Connection Error: " + String.Join("\n", loginFailure.Errors));
                Disconnect();
            }
            // all fragments
            TechFragmentsToDestroy = new HashSet<TechType>(APState.tech_fragments);
            // remove vanilla so it's scannable
            TechFragmentsToDestroy.ExceptWith(vanillaTech);
            Logging.LogDebug("Preventing scanning of: " + string.Join(", ", TechFragmentsToDestroy));
            Logging.LogDebug("Allowing scanning of: " + string.Join(", ", vanillaTech));
            return loginResult.Successful;
        }
        
        static void Session_SocketClosed(string reason)
        {
            Logging.LogError("Connection to Archipelago lost: " + reason);
            Disconnect();
        }
        static void Session_MessageReceived(LogMessage message)
        {
            if (!Silent)
            {
                message_queue.Enqueue(message.ToString());
            }
        }
        static void Session_ErrorReceived(Exception e, string message)
        {
            Logging.LogError(message);
            if (e != null) Logging.LogError(e.ToString());
            Disconnect();
        }

        public static void Disconnect()
        {
            Authenticated = false;
            state = State.Menu;

            if (Session != null)
            {
                if (Session.Socket != null && Session.Socket.Connected)
                {
                    var socket = Session.Socket;
                    Task.Run(() =>
                    {
                        try
                        {
                            socket.DisconnectAsync();
                        }
                        catch
                        {
                            // ignore
                        }
                    });
                }

                Session = null;
            }
            DeathLinkService = null;
        }
        public static void DeathLinkReceived(DeathLink deathLink)
        {
            Logging.LogDebug("Received DeathLink");
            Logging.MainThreadActions.Enqueue(() => {
                if (!(bool) (UnityEngine.Object) Player.main.liveMixin)
                    return;
                DeathLinkKilling = true;
                Player.main.liveMixin.Kill();
                message_queue.Enqueue(deathLink.Cause);
            });
        }

        public static bool CheckLocation(Vector3 position)
        {
            long closest_id = -1;
            float closestDist = 100000.0f;
            foreach (var location in ArchipelagoData.Locations)
            {
                var dist = Vector3.Distance(location.Value.Position, position);
                if (dist < closestDist && dist < 1.0f)
                {
                    closestDist = dist;
                    closest_id = location.Key;
                }
            }

            if (closest_id != -1)
            {
                SendLocID(closest_id);
                return true;
            }
#if DEBUG
            ErrorMessage.AddError("Tried to check unregistered Location at: " + position);
            Debug.LogError("Tried to check unregistered Location at: " + position);
            foreach (var location in ArchipelagoData.Locations)
            {
                var dist = Vector3.Distance(location.Value.Position, position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest_id = location.Key;
                }
            }
            ErrorMessage.AddError("Could it be Location ID " + closest_id + " with a distance of "+closestDist + "?");
            Debug.LogError("Could it be Location ID " + closest_id + " with a distance of "+closestDist + "?");
#endif
            return false;
        }

        public static void SendLocID(long id)
        {
            if (ServerConnectInfo.@checked.Add(id))
            {
                if (Session == null || !Authenticated)
                {
                    return;
                }

                Task.Run(() => {Session.Locations.CompleteLocationChecksAsync(
                    ServerConnectInfo.@checked.Except(Session.Locations.AllLocationsChecked).ToArray()); }).ConfigureAwait(false);
            }
        }

        public static void Resync()
        {
            if (Session == null || !Authenticated) return;
            
            if (ServerConnectInfo.@checked.Count > 0)
            {
                Logging.LogDebug("Running Location resync with " + ServerConnectInfo.@checked.Count + " locations.");
                Task.Run(() => { Session.Locations.CompleteLocationChecksAsync(ServerConnectInfo.@checked.ToArray()); }).ConfigureAwait(false);
            }

            Logging.LogDebug("Running Item resync with " + Session.Items.AllItemsReceived.Count + " items.");
            var done = new HashSet<long>();
            for (int i = 0; i < Session.Items.AllItemsReceived.Count; i++)
            {
                var itemID = Session.Items.AllItemsReceived[i].ItemId;
                if (ArchipelagoData.ItemCodeToItemType[itemID] == ArchipelagoItemType.Resource || !done.Contains(itemID))
                {
                    Unlock(itemID, i);
                    done.Add(itemID);
                }
            }
        }
        
        public static void Unlock(long apItemID, long index)
        {
            if (ArchipelagoData.GroupItems.TryGetValue(apItemID, out var groupUnlock))
            {
                foreach (var subUnlock in groupUnlock)
                {
                    Unlock(subUnlock, index);
                }
                return;
            }
            
            ArchipelagoData.ItemCodeToTechType.TryGetValue(apItemID, out var techType);
            if (ArchipelagoData.ItemCodeToItemType[apItemID] == ArchipelagoItemType.Resource)
            {
                HashSet<long> set;
                if (!ServerConnectInfo.resources_granted.TryGetValue(apItemID, out set))
                {
                    set = new();
                    ServerConnectInfo.resources_granted[apItemID] = set;
                }
                if (set.Contains(index))
                {
                    // already given resources
                    return;
                }

                set.Add(index);
                for (int i = 0; i < set.Count; i++)
                {
                    Inventory.main.StartCoroutine(PickUp(techType));
                }

                return;
            }

            if (techType == TechType.None || KnownTech.Contains(techType))
            {
                // Unknown item ID or already known technology.
                return;
            }

            if (PDAScanner.IsFragment(techType))
            {
                PDAScanner.EntryData entryData = PDAScanner.GetEntryData(techType);

                PDAScanner.Entry entry;
                if (!PDAScanner.GetPartialEntryByKey(techType, out entry))
                {
                    MethodInfo methodAdd = typeof(PDAScanner).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(TechType), typeof(int) }, null);
                    entry = (PDAScanner.Entry)methodAdd.Invoke(null, new object[] { techType, 0 });
                }

                if (entry != null)
                {
                    int newCount = Session.Items.AllItemsReceived.Count(networkItem => networkItem.ItemId == apItemID);
                    if (newCount == entry.unlocked)
                    {
                        return;
                    }
                    entry.unlocked = newCount;

                    if (entry.unlocked >= entryData.totalFragments)
                    {
                        List<PDAScanner.Entry> partial = (List<PDAScanner.Entry>)(typeof(PDAScanner).GetField("partial", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                        HashSet<TechType> complete = (HashSet<TechType>)(typeof(PDAScanner).GetField("complete", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                        partial.Remove(entry);
                        complete.Add(entry.techType);

                        MethodInfo methodNotifyRemove = typeof(PDAScanner).GetMethod("NotifyRemove", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(PDAScanner.Entry) }, null);
                        methodNotifyRemove.Invoke(null, new object[] { entry });

                        MethodInfo methodUnlock = typeof(PDAScanner).GetMethod("Unlock", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(PDAScanner.EntryData), typeof(bool), typeof(bool), typeof(bool) }, null);
                        methodUnlock.Invoke(null, new object[] { entryData, true, false, true });
                        if (FreeSamples)
                        {
                            GiveItem(entryData.blueprint);
                        }
                    }
                    else
                    {
                        int totalFragments = entryData.totalFragments;
                        if (totalFragments > 1)
                        {
                            float num2 = entry.unlocked / (float)totalFragments;
                            float arg = Mathf.RoundToInt(num2 * 100f);
                            ErrorMessage.AddError(Language.main.GetFormat(
                                "ScannerInstanceScanned", Language.main.Get(entry.techType.AsString()), 
                                arg, entry.unlocked, totalFragments));
                        }

                        MethodInfo methodNotifyProgress = typeof(PDAScanner).GetMethod(
                            "NotifyProgress", BindingFlags.NonPublic | BindingFlags.Static, null,
                            new Type[] { typeof(PDAScanner.Entry) }, null);
                        methodNotifyProgress.Invoke(null, new object[] { entry });
                    }
                }
            }
            else
            {
                // Blueprint
                if (ArchipelagoPlugin.Zero)
                {
                    typeof(KnownTech).GetMethod("Add", BindingFlags.Public | BindingFlags.Static).Invoke(
                        null,
                        new object[] {techType, true, true});
                }
                else
                {
                    typeof(KnownTech).GetMethod("Add", BindingFlags.Public | BindingFlags.Static).Invoke(
                        null,
                        new object[] {techType, true});
                }

                if (FreeSamples)
                {
                    GiveItem(techType);
                }
            }
        }

        private static IEnumerator PickUp(TechType techType)
        {
            TaskResult<GameObject> prefabResult = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(techType, prefabResult, false);
                
            GameObject gameObject = prefabResult.Get();
            if (gameObject == null)
            {
                Logging.LogError("Object " + techType + " failed to be created.", ingame:false);
                yield break;
            }
            // in case it can't be picked up, dump it right in front of player for obvious problem.
            gameObject.transform.position = MainCamera.camera.transform.position + 
                                            MainCamera.camera.transform.forward * 3f;
            // This seems to fill batteries to default values when crafted.
            CrafterLogic.NotifyCraftEnd(gameObject, techType);
            
            Pickupable pickupable = gameObject.GetComponent<Pickupable>();
            if (pickupable == null)
            {
                Logging.LogError("Object " + techType + " was not pickupable.", ingame:false);
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                Inventory.main.ForcePickup(pickupable);
            }
        }
        
        private static IEnumerator GiveItemAsync(TechType techType, bool giveLinked = false, bool filterCategory = true)
        {
            if (CraftData.GetBuilderIndex(techType, out TechGroup group, out TechCategory category, out int index))
            {
                if (filterCategory && (
                        category == TechCategory.Cyclops ||
                        category == TechCategory.BaseRoom ||
                        category == TechCategory.BasePiece ||
                        category == TechCategory.Misc ||
                        category == TechCategory.Constructor ||
                        category == TechCategory.BaseWall ||
                        category == TechCategory.ExteriorLight ||
                        category == TechCategory.ExteriorModule ||
                        category == TechCategory.ExteriorOther ||
                        category == TechCategory.InteriorModule ||
                        category == TechCategory.InteriorPiece ||
                        category == TechCategory.InteriorRoom
                    ))
                {
                    yield break;
                }

                yield return PickUp(techType);
                
                if (!giveLinked)
                {
                    yield break;
                }
                var techtypes = TechData.GetLinkedItems(techType);
                if (techtypes != null)
                {
                    foreach (var linkedItem in techtypes)
                    {
                        yield return PickUp(linkedItem);
                    }
                }
            }
        }

        public static void GiveItem(TechType techType)
        {
            Inventory.main.StartCoroutine(GiveItemAsync(techType, giveLinked: true));
        }
        
        public static void set_deathlink()
        {
            if (Session == null)
            {
                return;
            }

            if (DeathLinkService == null)
            {
                DeathLinkService = Session.CreateDeathLinkService();
            }
            
            if (ServerConnectInfo.death_link)
            {
                DeathLinkService.EnableDeathLink();
            }
            else
            {
                DeathLinkService.DisableDeathLink();
            }
        }
        
        public static void send_completion()
        {
            if (Session == null)
            {
                return;
            }

            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            Session.Socket.SendPacket(statusUpdatePacket);
        }
    }
}