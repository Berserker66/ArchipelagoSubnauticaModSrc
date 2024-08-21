using System;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Archipelago
{
    //BepInEx Interface
    [BepInPlugin("Archipelago", "Archipelago", Version)]
    public class ArchipelagoPlugin : BaseUnityPlugin
    {
        public const string Version = "1.8.0";
        public static bool Zero;
        // Early Reflection to not fish for things later:
        public static Type SubnauticaEscapePod;
        public static readonly BindingFlags all;

        private void Awake()
        {
            var harmony = new Harmony("Archipelago");
            Logging.Initialize();
            ArchipelagoData.Init();
            APState.Logger = Logger;
            
            // launch tracker thread
            APState.TrackerProcessing = new Thread(TrackerThread.DoWork);
            APState.TrackerProcessing.IsBackground = true;
            APState.TrackerProcessing.Start();
            
            SubnauticaEscapePod = Type.GetType("EscapePod, Assembly-CSharp");
            if (SubnauticaEscapePod is null)
            {
                Zero = true;
            }
            Logging.Log("Archipelago: Below Zero: " + Zero, ingame:false);

            // Universal Patches
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // Manual Patching based on game 
            if (Zero)
            {

            }
            else
            {
                PatchSubnautica(harmony);
            }

            harmony.Patch(typeof(PDAEncyclopedia).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static),
                postfix: new HarmonyMethod(typeof(CustomPDA).GetMethod("Add")));
            
            Logging.Log($"Plugin Archipelago (" + Version + ") for Server (" 
                        + APState.AP_VERSION[0] + "." + APState.AP_VERSION[1] + "." + APState.AP_VERSION[2] + 
                        ") is loaded!", ingame:false);
        }

        private void PatchSubnautica(Harmony harmony)
        {
            harmony.Patch(typeof(EscapePod).GetMethod("StopIntroCinematic"),
                postfix: new HarmonyMethod(typeof(EscapePod_StopIntroCinematic_Patch).GetMethod("GameReady")));
            harmony.Patch(typeof(Rocket).GetMethod("AdvanceRocketStage"),
                prefix: new HarmonyMethod(typeof(Rocket_AdvanceRocketStage_Patch).GetMethod("AdvanceRocketStage")));
            harmony.Patch(typeof(RocketConstructor).GetMethod("StartRocketConstruction"),
                prefix: new HarmonyMethod(typeof(RocketConstructor_StartRocketConstruction_Patch).GetMethod("StartRocketConstruction")));
            harmony.Patch(typeof(LaunchRocket).GetMethod("SetLaunchStarted", BindingFlags.NonPublic | BindingFlags.Static),
                prefix: new HarmonyMethod(typeof(LaunchRocket_SetLaunchStarted_Patch).GetMethod("SetLaunchStarted")));
            harmony.Patch(typeof(Player).GetMethod("Awake"),
                postfix: new HarmonyMethod(typeof(Player_Start_Patch).GetMethod("Awake")));
        }
    }
}