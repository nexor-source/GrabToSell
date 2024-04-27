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
using System.Runtime.ExceptionServices;

namespace GrabToSell
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class GrabToSell : BaseUnityPlugin
    {
        private const string modGUID = "nexor.GrabToSell";
        private const string modName = "GrabToSell";
        private const string modVersion = "0.0.5";

        private readonly Harmony harmony = new Harmony(modGUID);

        public ConfigEntry<string> switchKey;
        public ConfigEntry<string> white_list_name;
        public static GrabToSell Instance;
        public static BepInEx.Logging.ManualLogSource Logger;
        public bool hold_sell_mode = false;
        public string[] white_list = null;


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

            // 初始化配置项
            white_list_name = Config.Bind<string>("Grab To Sell Config",
                                            "the item list which will not trigger GrabToSell i.e. an item whitelist, (不会触发 GrabToSell 的物品列表,也就是物品白名单)",
                                            "easter egg, kitchen knife, shotgun",
                                            "Names are separated by commas. 名字间用逗号隔开");

            Logger = base.Logger;
            harmony.PatchAll();
            Logger.LogInfo("GrabToSell 0.0.5 loaded.");
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
            GrabToSell.Instance.white_list = GrabToSell.Instance.white_list_name.Value.Split(',');
            for (int i = 0;i<GrabToSell.Instance.white_list.Length;i++)
            {
                GrabToSell.Instance.white_list[i] = GrabToSell.Instance.white_list[i].ToLower().Trim();
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
                if (GrabToSell.Instance.hold_sell_mode) HUDManager.Instance.DisplayTip("Warning", "Grab To Sell mode ON!");
                else HUDManager.Instance.DisplayTip("Warning", "Grab To Sell mode OFF!");
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
                    if (UnityEngine.Object.FindObjectOfType<DepositItemsDesk>() != null && you.currentlyHeldObjectServer != null && you.currentlyHeldObjectServer.itemProperties.isScrap)
                    {

                        // GrabToSell.Logger.LogInfo("name : " + you.currentlyHeldObjectServer.itemProperties.itemName);

                        // 如果包含不该卖出去的物品的名字，就不卖
                        if (GrabToSell.Instance.white_list.Contains(you.currentlyHeldObjectServer.itemProperties.itemName.ToLower())) return;

                        

                        // GrabToSell.Logger.LogInfo("you.currentlyHeldObjectServer.itemProperties.isScrap: " + you.currentlyHeldObjectServer.itemProperties.isScrap);
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