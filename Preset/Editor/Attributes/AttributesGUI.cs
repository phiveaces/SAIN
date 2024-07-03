using EFT;
using EFT.UI;
using Interpolation;
using SAIN.Editor;
using SAIN.Editor.Util;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GearStealthValues;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static CW2.Animations.PhysicsSimulator.UnityValueDevice;
using static SAIN.Editor.SAINLayout;

namespace SAIN.Attributes
{
    public class AttributesGUI
    {
        public static AttributesInfoClass GetAttributeInfo(MemberInfo member)
        {
            string name = member.Name + member.DeclaringType.Name;
            AddAttributesToDictionary(name, member);
            if (_attributeClasses.TryGetValue(name, out var value))
            {
                return value;
            }
            return null;
        }

        private static void AddAttributesToDictionary(string name, MemberInfo member)
        {
            if (!_attributeClasses.ContainsKey(name) && !_failedAdds.Contains(name))
            {
                var attributes = new AttributesInfoClass(member);
                if (attributes.ValueType != null)
                {
                    _attributeClasses.Add(name, attributes);
                }
                else
                {
                    _failedAdds.Add(name);
                }
            }
        }

        public static object EditValue(ref object value, object settingsObject, AttributesInfoClass attributes, out bool wasEdited, GUIEntryConfig entryConfig = null)
        {
            wasEdited = false;
            if (value != null && attributes != null && !attributes.DoNotShowGUI) {
                entryConfig = entryConfig ?? _defaultEntryConfig;
                if (_floatBoolIntTypes.Contains(attributes.ValueType))
                    value = EditFloatBoolInt(ref value, settingsObject, attributes, entryConfig, out wasEdited);
                else
                    value = FindListTypeAndEdit(ref value, settingsObject, attributes, out wasEdited, entryConfig);
            }

            if (wasEdited)
                ConfigEditingTracker.Add(attributes.Name, value);

            return value;
        }

        public static object FindListTypeAndEdit(ref object value, object settingsObject, AttributesInfoClass attributes, out bool wasEdited, GUIEntryConfig entryConfig = null)
        {
            wasEdited = false;

            if (!ExpandableList(attributes, entryConfig.EntryHeight + 5))
                return value;

            if (value is Dictionary<ECaliber, float>)
                EditFloatDictionary<ECaliber>(value, attributes, out wasEdited);

            if (value is Dictionary<EWeaponClass, float>)
                EditFloatDictionary<EWeaponClass>(value, attributes, out wasEdited);

            if (value is Dictionary<ESoundDispersionType, DispersionValues>)
                EditDispersionDictionary(value as Dictionary<ESoundDispersionType, DispersionValues>, settingsObject, attributes, out wasEdited);

            if (value is Dictionary<EPersonality, bool>)
                EditBoolDictionary<EPersonality>( value, attributes, out wasEdited);

            if (value is List<WildSpawnType>)
                ModifyLists.AddOrRemove(value as List<WildSpawnType>, out wasEdited);

            if (value is List<BotType>)
                ModifyLists.AddOrRemove(value as List<BotType>, out wasEdited);

            if (value is List<Brain>)
                ModifyLists.AddOrRemove(value as List<Brain>, out wasEdited);

            return value;
        }

        private static void CreateLabelStyle()
        {
            if (_labelStyle == null)
            {
                GUIStyle boxstyle = GetStyle(Style.box);
                _labelStyle = new GUIStyle(GetStyle(Style.label))
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = boxstyle.margin,
                    padding = boxstyle.padding
                };
            }
        }

        public static object EditFloatBoolInt(ref object value, object settingsObject, AttributesInfoClass attributes, GUIEntryConfig entryConfig, out bool wasEdited, bool showLabel = true, bool beginHoriz = true)
        {
            if (beginHoriz)
            {
                if (attributes.Advanced)
                {
                    BeginHorizontal(25f);
                    Box("Advanced",
                        _labelStyle,
                        Width(70f),
                        Height(entryConfig.EntryHeight));
                }
                else
                {
                    BeginHorizontal(100f);
                }
            }

            if (showLabel)
            {
                CreateLabelStyle();

                Box(new GUIContent(
                    attributes.Name,
                    attributes.Description),
                    _labelStyle,
                    Height(entryConfig.EntryHeight)
                    );
            }

            bool showResult = false;
            object originalValue = value;

            if (attributes.ValueType == typeof(bool))
            {
                showResult = true;
                value = Toggle((bool)value, (bool)value ? "On" : "Off", EUISoundType.MenuCheckBox, entryConfig.Toggle);
            }
            else if (attributes.ValueType == typeof(float) || attributes.ValueType == typeof(int))
            {
                showResult = true;
                float flValue = BuilderClass.CreateSlider((float)value, attributes.Min, attributes.Max, entryConfig.Toggle);
                if (attributes.ValueType == typeof(int))
                {
                    value = Mathf.RoundToInt(flValue);
                }
                else
                {
                    value = flValue.Round(attributes.Rounding);
                }
            }

            if (showResult && value != null)
            {
                string dirtyString = TextField(value.ToString(), null, entryConfig.Result);
                value = BuilderClass.CleanString(dirtyString, value);
                if (attributes.ValueType != typeof(bool))
                {
                    value = attributes.Clamp(value);
                }

                var defaultValue = attributes.GetDefault(settingsObject);
                if (defaultValue != null)
                {
                    if (Button("Reset", "Reset To Default Value", EUISoundType.ButtonClick, entryConfig.Reset))
                        value = defaultValue;
                }
                else
                {
                    Box(" ", "No Default Value is assigned to this option.", entryConfig.Reset);
                }
            }

            if (beginHoriz)
                EndHorizontal(100f);

            wasEdited = originalValue.ToString() != value.ToString();
            return value;
        }

        public static void EditAllStealthValues(GearStealthValuesClass stealthClass)
        {
            BeginVertical(5f);

            var possibleTypes = EnumValues.GetEnum<EEquipmentType>();
            int count = possibleTypes.Length;
            var values = stealthClass.ItemStealthValues;
            var defaults = stealthClass.Defaults;

            for (int i = 0; i < count; i++)
            {
                var type = possibleTypes[i];
                if (values.TryGetValue(type, out var list))
                {
                    if (!ExpandableList(type.ToString(), string.Empty, _defaultEntryConfig.EntryHeight + 5))
                    {
                        continue;
                    }
                    editStealthValueList(list, defaults);
                }
            }

            EndVertical(5f);
        }

        private static void editStealthValueList(List<ItemStealthValue> list, List<ItemStealthValue> defaults)
        {
            if (list.Count == 0) return;
            BeginVertical(10f);
            for (int i = 0; i < list.Count; i++)
            {
                ItemStealthValue value = list[i];
                ItemStealthValue defaultValue = getDefault(value, defaults);
                editStealthValue(value, defaultValue);
            }
            EndVertical(10f);
        }

        private static ItemStealthValue getDefault(ItemStealthValue value, List<ItemStealthValue> defaults)
        {
            if (!defaults.Contains(value)) 
                return null;

            foreach (ItemStealthValue value2 in defaults)
            {
                if (value2.Name == value.Name) 
                    return value2;
            }
            return null;
        }

        private static void editStealthValue(ItemStealthValue stealthValue, ItemStealthValue defaultValue)
        {
            BeginHorizontal(150);
            string name = stealthValue.Name;
            string description = $"The Stealth Value for {name}";
            float fvalue = stealthValue.StealthValue;
            float min = 0.1f;
            float max = 2;

            fvalue = slider(name, description, fvalue, min, max).Round(100f);
            if (defaultValue != null && 
                resetButton())
            {
                fvalue = defaultValue.StealthValue;
            }

            if (fvalue != stealthValue.StealthValue)
            {
                stealthValue.StealthValue = fvalue;
                ConfigEditingTracker.Add(name, fvalue);
            }
            EndHorizontal(150);
        }

        private static bool ExpandableList(AttributesInfoClass attributes, float height)
        {
            return ExpandableList(attributes.Name, attributes.Description, height);
        }

        private static bool ExpandableList(string name, string description, float height)
        {
            BeginHorizontal(100f);

            if (!_listOpen.ContainsKey(name))
            {
                _listOpen.Add(name, false);
            }
            bool isOpen = _listOpen[name];
            isOpen = BuilderClass.ExpandableMenu(name, isOpen, description, height);
            _listOpen[name] = isOpen;

            EndHorizontal(100f);
            return isOpen;
        }

        public static void EditBoolDictionary<T>(object dictValue, AttributesInfoClass attributes, out bool edited) where T : Enum
        {
            edited = false;

            BeginVertical(5f);

            var defaultDictionary = attributes.DefaultDictionary as Dictionary<T, bool>;
            var dictionary = dictValue as Dictionary<T, bool>;
            List<T> list = dictionary.Keys.ToList();

            CreateLabelStyle();

            for (int i = 0; i < list.Count; i++)
            {
                BeginHorizontal(150f);

                var item = list[i];
                var name = item.ToString();
                Box(new GUIContent(name), _labelStyle, Height(_defaultEntryConfig.EntryHeight));
                if (Toggle(dictionary[item], dictionary[item] ? "On" : "Off", EUISoundType.MenuCheckBox, _defaultEntryConfig.Toggle))
                {
                    // Option was selected, set all other values to false, other than the 1 selected
                    for (int j = 0; j < list.Count; j++)
                    {
                        var item2 = list[j];
                        bool selected = item2.ToString() == name;

                        // Set all other options to false
                        if (!selected &&
                            dictionary[item2] != false)
                        {
                            dictionary[item2] = false;
                            edited = true;
                        }

                        // Set the selected option to true
                        if (selected &&
                            dictionary[item2] != true)
                        {
                            dictionary[item2] = true;
                            edited = true;
                        }
                    }
                }
                // Option was set to true, but is now set to false
                else if (dictionary[item] != false)
                {
                    // Option deselected
                    dictionary[item] = false;
                    edited = true;
                }
                EndHorizontal(150f);
            }

            list.Clear();
            EndVertical(5f);
        }

        public static void EditDispersionDictionary(Dictionary<ESoundDispersionType, DispersionValues> dictionary, object settingsObject, AttributesInfoClass attributes, out bool wasEdited)
        {
            BeginVertical(5f);

            var defaultDictionary = attributes.GetDefault(settingsObject) as Dictionary<ESoundDispersionType, DispersionValues>;
            ESoundDispersionType[] array = EnumValues.GetEnum<ESoundDispersionType>();
            wasEdited = false;

            for (int i = 0; i < array.Length; i++)
            {
                var soundType = array[i];
                if (!dictionary.TryGetValue(soundType, out DispersionValues values))
                {
                    continue;
                }
                editDispStruct(values, soundType, defaultDictionary, out bool newEdit);
                if (newEdit)
                    wasEdited = true;
            }
            EndVertical(5f);
        }

        private static void editDispStruct(DispersionValues values, ESoundDispersionType soundType, Dictionary<ESoundDispersionType, DispersionValues> defaultDictionary, out bool wasEdited)
        {
            wasEdited = false;

            BeginVertical(2f);
            BeginHorizontal(150);

            Box(new GUIContent(soundType.ToString()), _labelStyle, Height(_defaultEntryConfig.EntryHeight));

            EndHorizontal(150);
            BeginHorizontal(200f);

            string name = nameof(values.DistanceModifier);
            string description = "How much to randomize the distance that a bot thinks a sound originated from.";
            float fvalue = values.DistanceModifier;
            float min = 0f;
            float max = 20f;

            fvalue = slider(name, description, fvalue, min, max).Round(100f);
            if (resetButton())
                fvalue = defaultDictionary[soundType].DistanceModifier;

            if (fvalue != values.DistanceModifier)
            {
                values.DistanceModifier = fvalue;
                wasEdited = true;
            }

            EndHorizontal(200f);
            BeginHorizontal(200f);

            name = nameof(values.MinAngle);
            description = "";
            fvalue = values.MinAngle;
            min = 0f;
            max = 180;

            fvalue = slider(name, description, fvalue, min, max).Round(100f);
            if (resetButton())
                fvalue = defaultDictionary[soundType].MinAngle;

            if (fvalue != values.MinAngle)
            {
                values.MinAngle = fvalue;
                wasEdited = true;
            }

            EndHorizontal(200f);
            BeginHorizontal(200f);

            name = nameof(values.MaxAngle);
            description = "";
            fvalue = values.MaxAngle;
            min = 0f;
            max = 180;

            fvalue = slider(name, description, fvalue, min, max).Round(100f);
            if (resetButton())
                fvalue = defaultDictionary[soundType].MaxAngle;

            if (fvalue != values.MaxAngle)
            {
                values.MaxAngle = fvalue;
                wasEdited = true;
            }

            EndHorizontal(200f);
            BeginHorizontal(200f);

            name = nameof(values.VerticalModifier);
            description = "";
            fvalue = values.VerticalModifier;
            min = 0f;
            max = 0.5f;

            fvalue = slider(name, description, fvalue, min, max).Round(100f);
            if (resetButton())
                fvalue = defaultDictionary[soundType].VerticalModifier;

            if (fvalue != values.VerticalModifier)
            {
                values.VerticalModifier = fvalue;
                wasEdited = true;
            }

            EndHorizontal(200f);

            EndVertical(2f);
        }

        private static bool resetButton()
        {
            return Button("Reset", EUISoundType.ButtonClick, _defaultEntryConfig.Reset);
        }

        private static float slider(string name, string description, float value, float min, float max)
        {
            Box(new GUIContent(name, description), _labelStyle, Height(_defaultEntryConfig.EntryHeight));
            value = BuilderClass.CreateSlider(value, min, max, _defaultEntryConfig.Toggle).Round(100f);
            Box(value.ToString(), _defaultEntryConfig.Result);
            return value;
        }

        public static void EditFloatDictionary<T>(object dictValue, AttributesInfoClass attributes, out bool wasEdited) where T : Enum
        {
            BeginVertical(5f);

            float min = attributes.Min;
            float max = attributes.Max;
            float rounding = attributes.Rounding;

            var defaultDictionary = attributes.DefaultDictionary as Dictionary<T, float>;
            var dictionary = dictValue as Dictionary<T, float>;

            T[] array = EnumValues.GetEnum<T>();
            if (array != null && array.Length > 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    //Logger.LogInfo(array[i]);
                }
            }
            List<T> list = new List<T>();
            foreach (var entry in dictionary)
            {
                if (entry.Key.ToString() == "Default")
                {
                    continue;
                }
                list.Add(entry.Key);
            }

            CreateLabelStyle();

            wasEdited = false;
            for (int i = 0; i < list.Count; i++)
            {
                BeginHorizontal(150f);

                var item = list[i];
                float originalValue = dictionary[item];
                float floatValue = originalValue;

                Box(new GUIContent(item.ToString()), _labelStyle, Height(_defaultEntryConfig.EntryHeight));
                floatValue = BuilderClass.CreateSlider(floatValue, min, max, _defaultEntryConfig.Toggle).Round(rounding);
                Box(floatValue.ToString(), _defaultEntryConfig.Result);

                if (resetButton())
                    floatValue = defaultDictionary[item];

                if (floatValue != originalValue)
                {
                    wasEdited = true;
                    dictionary[item] = floatValue;
                }
                EndHorizontal(150f);
            }
            list.Clear();
            EndVertical(5f);
        }

        public static void EditAllValuesInObj(object obj, out bool wasEdited, string search = null)
        {
            wasEdited = false;
            BeginVertical(5f);

            var fields = obj.GetType().GetFields();
            foreach (var field in fields)
            {
                var attributes = GetAttributeInfo(field);
                if (SkipForSearch(attributes, search) || attributes.Advanced)
                {
                    continue;
                }
                object value = field.GetValue(obj);
                object newValue = EditValue(ref value, obj, attributes, out bool newEdit);
                if (newEdit)
                {
                    field.SetValue(obj, newValue);
                    wasEdited = true;
                }
            }
            foreach (var field in obj.GetType().GetFields())
            {
                var attributes = GetAttributeInfo(field);
                if (SkipForSearch(attributes, search) || !attributes.Advanced)
                {
                    continue;
                }
                object value = field.GetValue(obj);
                object newValue = EditValue(ref value, obj, attributes, out bool newEdit);
                if (newEdit)
                {
                    field.SetValue(obj, newValue);
                    wasEdited = true;
                }
            }

            EndVertical(5f);
        }

        public static void EditAllValuesInObj(Category category, object categoryObject, out bool wasEdited, string search = null)
        {
            BeginVertical(5);

            wasEdited = false;
            foreach (var fieldAtt in category.FieldAttributesList)
            {
                if (SkipForSearch(fieldAtt, search) || fieldAtt.Advanced)
                {
                    continue;
                }
                object value = fieldAtt.GetValue(categoryObject);
                object newValue = EditValue(ref value, categoryObject, fieldAtt, out bool newEdit);
                if (newEdit)
                {
                    fieldAtt.SetValue(categoryObject, newValue);
                    wasEdited = true;
                }
            }

            foreach (var fieldAtt in category.FieldAttributesList)
            {
                if (SkipForSearch(fieldAtt, search) || !fieldAtt.Advanced)
                {
                    continue;
                }
                object value = fieldAtt.GetValue(categoryObject);
                object newValue = EditValue(ref value, categoryObject, fieldAtt, out bool newEdit);
                if (newEdit)
                {
                    fieldAtt.SetValue(categoryObject, newValue);
                    wasEdited = true;
                }
            }

            EndVertical(5);
        }

        public static bool SkipForSearch(AttributesInfoClass attributes, string search)
        {
            return !string.IsNullOrEmpty(search) &&
                (attributes.Name.ToLower().Contains(search) == false &&
                attributes.Description?.ToLower().Contains(search) == false);
        }

        private static readonly Dictionary<string, bool> _listOpen = new Dictionary<string, bool>();
        private static readonly List<string> _failedAdds = new List<string>();
        private static readonly Type[] _floatBoolIntTypes =
        {
            typeof(bool),
            typeof(float),
            typeof(int),
        };
        private static readonly GUIEntryConfig _defaultEntryConfig = new GUIEntryConfig();
        private static GUIStyle _labelStyle;
        private static readonly Dictionary<string, AttributesInfoClass> _attributeClasses = new Dictionary<string, AttributesInfoClass>();
    }
}