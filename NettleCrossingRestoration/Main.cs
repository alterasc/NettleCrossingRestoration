using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Actions;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Localization;
using System;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace NettleCrossingRestoration
{
    static class Main
    {
        public static bool Enabled;
        public static UnityModManager.ModEntry ModEntry;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            ModEntry = modEntry;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            harmony.PatchAll();
            return true;
        }
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (GUILayout.RepeatButton("Unlock Kingdom project (use if you're installing this mid playthrough)", GUILayout.ExpandWidth(false)))
            {
                if (Game.Instance == null || Game.Instance.Player == null) return;
                var player = Game.Instance.Player;
                if (!KingdomState.Founded) return;
                var proj = ResourcesLibrary.TryGetBlueprint<BlueprintKingdomProject>("42e3f6af-ce48-474b-b435-5d14ebf12c9c");
                var shrike = ResourcesLibrary.TryGetBlueprint<BlueprintRegion>("caacbcf9f6d6561459f526e584ded703");

                RegionState rs = player.Kingdom.Regions.First(x => x.Blueprint == shrike);
                KingdomTimelineManager kingdomTimelineManager = new();
                if (KingdomState.Instance.EventHistory.Any((KingdomEventHistoryEntry e) => e.Event == proj) || KingdomState.Instance.ActiveEvents.Any((KingdomEvent e) => e.EventBlueprint == proj))
                {
                    return;
                }
                kingdomTimelineManager.StartEventInRegion(proj, rs, 0).CheckTriggerOnStart = false;
            }

        }

    }

    [HarmonyPatch(typeof(LibraryScriptableObject), nameof(LibraryScriptableObject.LoadDictionary))]
    static class LibraryScriptableObject_LoadDictionary_Patch
    {
        static bool Run = false;
        static void Postfix(LibraryScriptableObject __instance)
        {
            if (Run) return;
            Run = true;
            var proj = ScriptableObject.CreateInstance<BlueprintKingdomProject>();
            var guid = "42e3f6af-ce48-474b-b435-5d14ebf12c9c";
            proj.m_AssetGuid = guid;
            proj.ProjectType = KingdomProjectType.Economy;
            proj.ProjectStartBPCost = 50;
            proj.ResolutionTime = 10;
            proj.SkipRoll = true;
            proj.AutoResolveResult = EventResult.MarginType.Success;
            proj.name = "NettleCrossingRestoration";
            proj.LocalizedName = CreateString("5b4555f1-6b87-450e-a0a4-a9a71062f347", "Restoration of Nettle's Crossing");
            proj.LocalizedDescription = CreateString("acc2ce5a-2a95-454b-ac0f-132858eb1fbe",
                "With Davik Nettle laid to rest it is possible to restore his bridge over the Shrike River.");

            var kTC21OlegSecondBuildingRoad = ResourcesLibrary.TryGetBlueprint<BlueprintKingdomProject>("fd3e919b6ce941744b2821c120a832eb");
            var riverCrossingAllowed = ResourcesLibrary.TryGetBlueprint<BlueprintUnlockableFlag>("b9fa316b554eec14cb5f082dcb6485ee");
            proj.Solutions = kTC21OlegSecondBuildingRoad.Solutions;
            proj.Components = new BlueprintComponent[] {
                new EventFinalResults()
                {
                    Results = new EventResult[]
                    {
                        new EventResult()
                        {
                            Margin = EventResult.MarginType.Success,
                            Condition = new ConditionsChecker(),
                            Actions = new ActionList()
                            {
                                Actions = new GameAction[]
                                {
                                    new UnlockFlag(){
                                        flag = riverCrossingAllowed
                                    }
                                }
                            },
                            StatChanges = new KingdomStats.Changes()
                            {
                                m_Changes = new int[10]{0,0,0,2,0,0,0,0,0,0}
                            },
                            SuccessCount = 1,
                            LocalizedDescription = CreateString("876f67d4-48c0-4021-ab01-40b0420ad782", "You've restored Nettle's Crossing")
                        }
                    }
                }
            };
            __instance.AddAsset(proj);


            var nettleTalkAfterStagLordDeath = ResourcesLibrary.TryGetBlueprint<BlueprintCue>("281aa076dae1eee41901f5d041879c36");
            var newAction = new Conditional()
            {
                name = "Give research into NettleCrossingRestoration if player already has barony",
                ConditionsChecker = new ConditionsChecker()
                {
                    Conditions = new Condition[]
                    {
                        new Kingmaker.Kingdom.Conditions.KingdomExists()
                    }
                },
                IfTrue = new ActionList()
                {
                    Actions = new GameAction[]
                    {
                        new KingdomActionStartEvent()
                        {
                            Event = proj
                        }
                    }
                },
                IfFalse = new ActionList()
            };
            nettleTalkAfterStagLordDeath.OnShow.Actions = nettleTalkAfterStagLordDeath.OnShow.Actions.Add(newAction).ToArray();

            var onFirstKingdomSetup = ResourcesLibrary.TryGetBlueprint<BlueprintCue>("61fd0dbd69f5c354995a559f79888c6f");
            var newAction2 = new Conditional()
            {
                name = "NettleCrossing Restoration should be started",
                ConditionsChecker = new ConditionsChecker()
                {
                    Conditions = new Condition[]
                    {
                        new FlagUnlocked()
                        {
                            ConditionFlag = ResourcesLibrary.TryGetBlueprint<BlueprintUnlockableFlag>("6901d3010176c7643bb15cd3e870fc65")
                        }
                    }
                },
                IfTrue = new ActionList()
                {
                    Actions = new GameAction[]
                    {
                        new KingdomActionStartEvent()
                        {
                            Event = proj
                        }
                    }
                },
                IfFalse = new ActionList()
            };
            onFirstKingdomSetup.OnStop.Actions = onFirstKingdomSetup.OnStop.Actions.Add(newAction2).ToArray();
        }

        public static void AddAsset(this LibraryScriptableObject library, BlueprintScriptableObject blueprint)
        {
            var guid = blueprint.m_AssetGuid;
            if (library.BlueprintsByAssetId.TryGetValue(guid, out BlueprintScriptableObject existing))
            {
                throw new Exception($"Duplicate AssetId for {blueprint.name}, existing entry ID: {guid}, name: {existing.name}, type: {existing.GetType().Name}");
            }
            library.m_AllBlueprints.Add(blueprint);
            library.BlueprintsByAssetId[guid] = blueprint;
        }

        public static LocalizedString CreateString(string key, string value)
        {
            var strings = LocalizationManager.CurrentPack.Strings;
            strings[key] = value;
            var localized = new LocalizedString
            {
                m_Key = key
            };
            return localized;
        }
    }

}
