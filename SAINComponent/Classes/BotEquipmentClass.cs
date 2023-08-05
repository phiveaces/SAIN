﻿using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BotEquipmentClass : SAINBase, ISAINClass
    {
        static BotEquipmentClass()
        {
            _InventoryProp = AccessTools.Property(typeof(Player), "Inventory");
        }

        static readonly PropertyInfo _InventoryProp;

        public BotEquipmentClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            Inventory = Reflection.GetValue<InventoryClass>(Player, _InventoryProp);
            InventoryController = HelpersGClass.GetInventoryController(Player);
        }

        public void Update()
        {
            if (UpdateEquipmentTimer < Time.time)
            {
                UpdateEquipmentTimer = Time.time + 30f;

                HasEarPiece = Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem != null;
                CheckHelmet();
            }
        }

        public void Dispose()
        {
        }


        public InventoryControllerClass InventoryController { get; private set; }

        public InventoryClass Inventory { get; private set; }
        public Weapon CurrentWeapon => BotOwner.WeaponManager.CurrentWeapon;
        public Weapon SecondaryWeapon => BotOwner.WeaponManager.SecondWeaponInfo?.weapon;

        private readonly List<ArmorComponent> HelmetArmorComponents = new List<ArmorComponent>();

        public float InventorySpaceFilled { get; private set; }

        private void CheckHelmet()
        {
            HasHelmet = false;
            HasHeavyHelmet = false;
            HelmetArmorComponents.Clear();

            var headWear = Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            if (headWear != null)
            {
                headWear.GetItemComponentsInChildrenNonAlloc<ArmorComponent>(HelmetArmorComponents, true);
                if (HelmetArmorComponents.Count > 0)
                {
                    HasHelmet = true;
                    foreach (ArmorComponent armor in HelmetArmorComponents)
                    {
                        if (armor.Deaf == EDeafStrength.High)
                        {
                            HasHeavyHelmet = true;
                            break;
                        }
                    }
                }
            }
        }

        public bool HasEarPiece { get; private set; }
        public bool HasOptic { get; private set; }
        public bool HasHelmet { get; private set; }
        public bool HasHeavyHelmet { get; private set; }
        public bool HasArmor { get; private set; }
        public bool HasPrimary => PrimaryWeaponClass != WeaponClass.None;
        public bool HasSecondPrimary => SecondPrimaryWeaponClass != WeaponClass.None;
        public bool HasSideArm { get; private set; }
        public bool HasSuppressor { get; private set; }
        public WeaponClass PrimaryWeaponClass { get; private set; }
        public WeaponClass SecondPrimaryWeaponClass { get; private set; }
        public EquipmentClass Equipment => Inventory.Equipment;

        private float UpdateEquipmentTimer = 0f;
    }

    public enum WeaponClass
    {
        None,
        AssaultRifle,
        BattleRifle,
        SMG,
        PumpShotgun,
        AutoShotgun,
        MarksmanRifle,
        SniperRifle,
        Pistol,
    }
}