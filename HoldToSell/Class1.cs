using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

using System;
using System.Security.Cryptography;

namespace GrabToSell
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class GrabToSell : BaseUnityPlugin
    {
        private const string modGUID = "nexor.GrabToSell";
        private const string modName = "GrabToSell";
        private const string modVersion = "0.0.1";

        private readonly Harmony harmony = new Harmony(modGUID);

        public ConfigEntry<string> switchKey;
        public static GrabToSell Instance;
        public static BepInEx.Logging.ManualLogSource Logger;
        public bool hold_sell_mode = false;


        // 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // 初始化配置项
            switchKey = Config.Bind<string>("Grab To Sell Config",
                                            "Grab To Sell mode switch key(切换快速售卖模式的按键)",
                                            "F11",
                                            "Grab To Sell mode only works on company. 快速售卖模式仅在公司有用");



            Logger = base.Logger;
            harmony.PatchAll();
            Logger.LogInfo("GrabToSell 0.0.1 loaded.");
        }
    }
    [HarmonyPatch(typeof(HUDManager), "Update")]
    internal class HUDPatch
    {

        private static Key switchKey;

        static HUDPatch()
        {

            // 将配置文件中的字符串表示形式转换为 Key
            if (!(System.Enum.TryParse(GrabToSell.Instance.switchKey.Value, true, out switchKey)))
            {
                // 如果解析失败，说明用户提供了无效的按键字符串
                // Debug.LogError("Invalid teleport key: [" + TeleportYourself.Instance.teleportKey.Value+"]");
                // Debug.LogError("Using default teleport key [V]");
                switchKey = Key.F11;
            }

        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            // 判断是否是公司地形
            if (RoundManager.Instance.currentLevel.sceneName != "CompanyBuilding") return;

            // 如果摁下开关键，则切换模式
            if (Keyboard.current[switchKey].wasPressedThisFrame)
            {
                GrabToSell.Instance.hold_sell_mode = !GrabToSell.Instance.hold_sell_mode;
                if (GrabToSell.Instance.hold_sell_mode) HUDManager.Instance.DisplayTip("Warning", "Hold To Sell mode ON!");
                else HUDManager.Instance.DisplayTip("Warning", "Hold To Sell mode OFF!");
            }

            // 如果模式已启动
            if (GrabToSell.Instance.hold_sell_mode)
            {
                
                PlayerControllerB you = StartOfRound.Instance.localPlayerController;
                if (you.isGrabbingObjectAnimation || you.isTypingChat || you.inSpecialInteractAnimation)
                {
                    return;
                }
                if (you.activatingItem)
                {
                    return;
                }

                if (you.isHoldingObject && you.currentlyHeldObjectServer != null)
                {
                    if (UnityEngine.Object.FindObjectOfType<DepositItemsDesk>() != null && you.currentlyHeldObjectServer != null)
                    {
                        DepositItemsDesk depositItemsDesk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();

                        depositItemsDesk.PlaceItemOnCounter(you);
                        // depositItemsDesk.SellItemsOnServer();
                        // GrabToSell.Logger.LogInfo("GrabToSell triggered");
                        return;

                    }

                }
                
            }

        }

    }

}