using UnityEngine;
using MysticsRisky2Utils;
using RoR2;
using RoR2.Skills;
using UnityEngine.Networking;
using MysticsRisky2Utils.BaseAssetTypes;
using MysticsRisky2Utils.ContentManagement;
using R2API;
using RoR2.Projectile;
using RoR2.Navigation;
using RoR2.Networking;
using UnityEngine.Events;

namespace ExtraChallengeShrines.Interactables
{
    public class ShrineRock : BlankLoadableAsset
    {
        public static GameObject shrinePrefab;
        public static InteractableSpawnCard spawnCard;

        public static ConfigOptions.ConfigurableValue<float> bossCredits = ConfigOptions.ConfigurableValue.CreateFloat(
            ExtraChallengeShrinesPlugin.PluginGUID,
            ExtraChallengeShrinesPlugin.PluginName,
            ExtraChallengeShrinesPlugin.config,
            "Shrine of the Earth",
            "Boss Credits",
            300f,
            0f,
            100000f,
            "How many director credits to add when this shrine is first used?",
            useDefaultValueConfigEntry: ExtraChallengeShrinesPlugin.ignoreBalanceChanges
        );
        public static ConfigOptions.ConfigurableValue<float> bossCreditsPerStack = ConfigOptions.ConfigurableValue.CreateFloat(
            ExtraChallengeShrinesPlugin.PluginGUID,
            ExtraChallengeShrinesPlugin.PluginName,
            ExtraChallengeShrinesPlugin.config,
            "Shrine of the Earth",
            "Boss Credits Per Stack",
            600f,
            0f,
            100000f,
            "How many director credits to add for each time this shrine is used more than once?",
            useDefaultValueConfigEntry: ExtraChallengeShrinesPlugin.ignoreBalanceChanges
        );
        public static ConfigOptions.ConfigurableValue<int> extraDrops = ConfigOptions.ConfigurableValue.CreateInt(
            ExtraChallengeShrinesPlugin.PluginGUID,
            ExtraChallengeShrinesPlugin.PluginName,
            ExtraChallengeShrinesPlugin.config,
            "Shrine of the Earth",
            "Extra Drops",
            2,
            0,
            100,
            "How many extra items to drop for completing the TP event?",
            useDefaultValueConfigEntry: ExtraChallengeShrinesPlugin.ignoreBalanceChanges
        );
        public static ConfigOptions.ConfigurableValue<int> extraDropsPerStack = ConfigOptions.ConfigurableValue.CreateInt(
            ExtraChallengeShrinesPlugin.PluginGUID,
            ExtraChallengeShrinesPlugin.PluginName,
            ExtraChallengeShrinesPlugin.config,
            "Shrine of the Earth",
            "Extra Drops Per Stack",
            3,
            0,
            100,
            "How many extra items to drop for completing the TP event per additional use of this shrine?",
            useDefaultValueConfigEntry: ExtraChallengeShrinesPlugin.ignoreBalanceChanges
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
        }

        public override void OnLoad()
        {
            base.OnLoad();

            shrinePrefab = ExtraChallengeShrinesPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/ExtraChallengeShrines/ShrineRock/ShrineRock.prefab");

            foreach (Renderer renderer in shrinePrefab.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.shader.name == "Standard" && material.shader != HopooShaderToMaterial.Standard.shader)
                    {
                        HopooShaderToMaterial.Standard.Apply(material);
                        HopooShaderToMaterial.Standard.Dither(material);
                    }
                }
            }

            var existingSymbol = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Shrines/ShrineBoss").transform.Find("Symbol").gameObject;
            var symbol = shrinePrefab.transform.Find("Symbol").gameObject;
            symbol.GetComponent<MeshFilter>().mesh = Object.Instantiate(existingSymbol.GetComponent<MeshFilter>().mesh);
            Material symbolMaterial = Object.Instantiate(existingSymbol.GetComponent<MeshRenderer>().material);
            symbol.GetComponent<MeshRenderer>().material = symbolMaterial;
            symbolMaterial.SetTexture("_MainTex", ExtraChallengeShrinesPlugin.AssetBundle.LoadAsset<Texture>("Assets/Mods/ExtraChallengeShrines/ShrineRock/Symbol.png"));
            symbolMaterial.SetColor("_TintColor", new Color32(238, 255, 226, 255));

            ExtraChallengeShrinesTeleporterComponent.rockShrineIndicatorMaterial = symbolMaterial;

            var shrineBehaviour = shrinePrefab.AddComponent<ExtraChallengeShrinesShrineRockBehaviour>();
            shrineBehaviour.symbolTransform = symbol.transform;

            ExtraChallengeShrinesContent.Resources.networkedObjectPrefabs.Add(shrinePrefab);

            spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            spawnCard.name = "iscExtraChallengeShrines_ShrineRock";
            spawnCard.prefab = shrinePrefab;
            spawnCard.sendOverNetwork = true;
            spawnCard.hullSize = HullClassification.Golem;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.requiredFlags = NodeFlags.None;
            spawnCard.forbiddenFlags = NodeFlags.NoShrineSpawn;
            spawnCard.directorCreditCost = 20;
            spawnCard.occupyPosition = true;
            spawnCard.orientToFloor = true;
            spawnCard.slightlyRandomizeOrientation = true;
            spawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;

            ConfigOptions.ConfigurableValue.CreateInt(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Director Credit Cost",
                20,
                0,
                1000,
                onChanged: (x) => spawnCard.directorCreditCost = x
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Max Spawns Per Stage",
                -1,
                -1,
                1000,
                description: "-1 means no limit",
                onChanged: (x) => spawnCard.maxSpawnsPerStage = x
            );

            var directorCardRare = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = 1,
                spawnDistance = 0f,
                preventOverhead = false,
                minimumStageCompletions = 0,
                requiredUnlockableDef = null,
                forbiddenUnlockableDef = null
            };
            var directorCardCommon = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = 5,
                spawnDistance = 0f,
                preventOverhead = false,
                minimumStageCompletions = 0,
                requiredUnlockableDef = null,
                forbiddenUnlockableDef = null
            };

            var stageNames = ConfigOptions.ConfigurableValue.CreateString(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Stages",
                "dampcavesimple,wispgraveyard",
                restartRequired: true
            );
            foreach (var stageName in stageNames.Value.Split(','))
                BaseInteractable.AddDirectorCardTo(stageName, "Shrines", directorCardCommon);

            stageNames = ConfigOptions.ConfigurableValue.CreateString(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Stages (Rare)",
                "blackbeach,foggyswamp,golemplains,rootjungle,shipgraveyard,skymeadow,ancientloft,snowyforest",
                restartRequired: true
            );
            foreach (var stageName in stageNames.Value.Split(','))
                BaseInteractable.AddDirectorCardTo(stageName, "Shrines", directorCardRare);

            ConfigOptions.ConfigurableValue.CreateInt(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Selection Weight",
                5,
                0,
                1000,
                onChanged: (x) => {
                    directorCardCommon.selectionWeight = x;
                }
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Selection Weight (Rare)",
                1,
                0,
                1000,
                onChanged: (x) => {
                    directorCardRare.selectionWeight = x;
                }
            );

            ConfigOptions.ConfigurableValue.CreateInt(
                ExtraChallengeShrinesPlugin.PluginGUID,
                ExtraChallengeShrinesPlugin.PluginName,
                ExtraChallengeShrinesPlugin.config,
                "Shrine of the Earth",
                "Minimum Stage Completions",
                1,
                0,
                99,
                description: "Need to clear this many stages before it can spawn",
                onChanged: (x) => {
                    directorCardCommon.minimumStageCompletions = x;
                    directorCardRare.minimumStageCompletions = x;
                }
            );

            SceneDirector.onGenerateInteractableCardSelection += SceneDirector_onGenerateInteractableCardSelection;
        }

        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
        {
            if (RunArtifactManager.instance &&
                RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.singleMonsterTypeArtifactDef))
            {
                dccs.RemoveCardsThatFailFilter(x =>
                {
                    var prefab = x.spawnCard.prefab;
                    return !prefab.GetComponent<ExtraChallengeShrinesShrineRockBehaviour>();
                });
            }
        }
    }

    public class ExtraChallengeShrinesShrineRockBehaviour : NetworkBehaviour
    {
        public int maxPurchaseCount = 1;
        public float costMultiplierPerPurchase = 2f;
        public Transform symbolTransform;
        public PurchaseInteraction purchaseInteraction;
        public int purchaseCount;
        public float refreshTimer;
        public const float refreshDuration = 0.5f;
        public bool waitingForRefresh;

        public void Start()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            purchaseInteraction.onPurchase.AddListener((interactor) =>
            {
                purchaseInteraction.SetAvailable(false);
                AddShrineStack(interactor);
            });
        }

        public void FixedUpdate()
        {
            if (waitingForRefresh)
            {
                refreshTimer -= Time.fixedDeltaTime;
                if (refreshTimer <= 0f && purchaseCount < maxPurchaseCount)
                {
                    purchaseInteraction.SetAvailable(true);
                    purchaseInteraction.Networkcost = (int)(100f * (1f - Mathf.Pow(1f - (float)purchaseInteraction.cost / 100f, costMultiplierPerPurchase)));
                    waitingForRefresh = false;
                }
            }
        }

        [Server]
        public void AddShrineStack(Interactor interactor)
        {
            waitingForRefresh = true;
            if (TeleporterInteraction.instance && TeleporterInteraction.instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging)
            {
                var tpComponent = TeleporterInteraction.instance.GetComponent<ExtraChallengeShrinesTeleporterComponent>();
                if (!tpComponent) return;

                tpComponent.rockShrineStacks++;
                if (tpComponent.rockShrineStacks == 1)
                {
                    tpComponent.bossGroup.bonusRewardCount += ShrineRock.extraDrops;
                }
                else
                {
                    tpComponent.bossGroup.bonusRewardCount += ShrineRock.extraDropsPerStack;
                }
                tpComponent.ServerSendSyncShrineStacks();
            }
            CharacterBody component = interactor.GetComponent<CharacterBody>();
            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = component,
                baseToken = "EXTRACHALLENGESHRINES_SHRINE_ROCK_USE_MESSAGE"
            });
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = transform.position,
                rotation = Quaternion.identity,
                scale = 1f,
                color = new Color(0.7372549f, 0.905882359f, 0.945098042f)
            }, true);
            purchaseCount++;
            refreshTimer = 2f;
            if (purchaseCount >= maxPurchaseCount)
            {
                symbolTransform.gameObject.SetActive(false);
            }
        }

        public override int GetNetworkChannel()
        {
            return QosChannelIndex.defaultReliable.intVal;
        }
    }
}
