using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using UnityEngine;
using FloatFunc = GClass755<float>;

namespace SAIN.SAINComponent.Classes
{
    public class BotWeightManagement : BotBase, IBotClass
    {
        public BotWeightManagement(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            if (GlobalSettingsClass.Instance.General.BotWeightEffects)
            {
                getSlots();
                Person.Player.InventoryControllerClass.Inventory.TotalWeight = new FloatFunc(new Func<float>(this.getBotTotalWeight));
                Person.Player.Physical.EncumberDisabled = false;
            }
        }

        private void getSlots()
        {
            _slots.Clear();
            foreach (var slot in _botEquipmentSlots)
            {
                _slots.Add(Player.Equipment.GetSlot(slot));
            }
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private float getBotTotalWeight()
        {
            float result = Player.Equipment.method_10(_slots);
            _slots.Clear();
            // Logger.LogWarning(result);
            return result;
        }

        private readonly List<Slot> _slots = new List<Slot>();

        public static readonly EquipmentSlot[] _botEquipmentSlots = new EquipmentSlot[]
        {
            EquipmentSlot.Backpack,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.ArmorVest,
            EquipmentSlot.Eyewear,
            EquipmentSlot.FaceCover,
            EquipmentSlot.Headwear,
            EquipmentSlot.Earpiece,
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
            EquipmentSlot.Pockets,
        };
    }
}
