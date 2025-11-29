using System;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Emissions;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(HydraProjectiles.HydraProjectilesMod), HydraProjectiles.ModHelperData.Name, HydraProjectiles.ModHelperData.Version, HydraProjectiles.ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace HydraProjectiles;

public class HydraProjectilesMod : BloonsTD6Mod
{
    private static readonly ModSettingBool Enabled = new(true)
    {
        displayName = "Enabled",
        button = true
    };

    private static readonly ModSettingHotkey ToggleHotkey = new(KeyCode.F9)
    {
        displayName = "Toggle Hotkey"
    };

    public override void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
    {
        if (!Enabled) return;
        ApplyHydraToTower(tower);
    }

    public override void OnTowerUpgraded(Tower tower, string upgradeName, TowerModel newBaseTowerModel)
    {
        if (!Enabled) return;
        ApplyHydraToTower(tower);
    }

    private static void ApplyHydraToTower(Tower tower)
    {
        if (tower.towerModel.baseId == TowerType.SpikeFactory) return;

        var towerModel = tower.towerModel.Duplicate();
        var modified = false;

        foreach (var attackModel in towerModel.GetAttackModels())
        {
            if (attackModel.name.Contains("Wall of") || attackModel.name.Contains("Caltrop")) continue;

            foreach (var weaponModel in attackModel.weapons)
            {
                if (weaponModel.projectile == null) continue;
                if (!weaponModel.projectile.HasBehavior<TravelStraitModel>()) continue;
                if (weaponModel.projectile.HasBehavior<CreateProjectileOnContactModel>()) continue;

                ApplyHydraEffect(weaponModel.projectile);
                modified = true;
            }
        }

        if (modified) tower.UpdateRootModel(towerModel);
    }

    private static void ApplyHydraEffect(ProjectileModel projectile)
    {
        projectile.pierce = 5;

        var childProjectile = CreateChildProjectile(projectile, 0.5f, 2);
        var grandchildProjectile = CreateChildProjectile(projectile, 0.25f, 1);

        AddSeekingBehavior(childProjectile);
        AddSeekingBehavior(grandchildProjectile);

        AddSpawnOnContact(childProjectile, grandchildProjectile, 2);
        AddSpawnOnContact(projectile, childProjectile, 2);
    }

    private static ProjectileModel CreateChildProjectile(ProjectileModel parent, float damageMultiplier, int pierce)
    {
        var child = parent.Duplicate();
        child.id = parent.id + $"_HydraChild_{damageMultiplier}";
        child.pierce = pierce;

        if (child.displayModel != null)
            child.displayModel.ignoreRotation = false;

        child.RemoveBehavior<CreateProjectileOnContactModel>();

        if (child.HasBehavior(out DamageModel damageModel))
            damageModel.damage = MathF.Max(1f, damageModel.damage * damageMultiplier);

        return child;
    }

    private static void AddSeekingBehavior(ProjectileModel projectile)
    {
        if (projectile.HasBehavior<TrackTargetModel>()) return;

        projectile.AddBehavior(new TrackTargetModel("", 999f, true, true, 360f, true, 720f, true, false, false));

        if (projectile.HasBehavior(out TravelStraitModel travel))
        {
            travel.Speed *= 0.5f;
            travel.Lifespan *= 2f;
        }
    }

    private static void AddSpawnOnContact(ProjectileModel parent, ProjectileModel child, int count)
    {
        parent.AddBehavior(new CreateProjectileOnContactModel(
            "", child, new ArcEmissionModel("", count, 0, 180, null, false, false), true, false, false
        ));
    }

    private static void ApplyToAllTowers()
    {
        if (InGame.instance?.bridge == null) return;

        foreach (var tower in InGame.instance.GetTowers())
            ApplyHydraToTower(tower);
    }

    private static void RemoveFromAllTowers()
    {
        if (InGame.instance?.bridge == null) return;

        var gameModel = InGame.instance.GetGameModel();
        foreach (var tower in InGame.instance.GetTowers())
        {
            var originalModel = gameModel.GetTower(
                tower.towerModel.baseId,
                tower.towerModel.tiers[0],
                tower.towerModel.tiers[1],
                tower.towerModel.tiers[2]
            );
            if (originalModel != null)
                tower.UpdateRootModel(originalModel);
        }
    }

    public override void OnUpdate()
    {
        if (!ToggleHotkey.JustPressed()) return;

        Enabled.SetValue(!Enabled);

        if (Enabled)
            ApplyToAllTowers();
        else
            RemoveFromAllTowers();

        ModHelper.Msg<HydraProjectilesMod>($"Hydra Projectiles {(Enabled ? "Enabled" : "Disabled")}");
    }
}
