using SAIN.Plugin;
using SAIN.Preset;
using System;

namespace SAIN.SAINComponent
{
    public abstract class PresetUpdaterBase
    {
        protected virtual void SubscribeToPreset(Action<SAINPresetClass> func)
        {
            if (func != null)
            {
                subscribed = true;
                _func = func;
                PresetHandler.OnPresetUpdated += func;
            }
        }

        protected virtual void UnSubscribeToPreset()
        {
            if (subscribed && _func != null)
            {
                subscribed = false;
                PresetHandler.OnPresetUpdated -= _func;
            }
        }

        protected bool subscribed;
        private Action<SAINPresetClass> _func;
    }

    // this purely exists to avoid rewriting code 100 times
    public class PresetAutoUpdater
    {
        public void Subscribe(Action<SAINPresetClass> func)
        {
            if (func != null)
            {
                Subscribed = true;
                _func = func;
                PresetHandler.OnPresetUpdated += func;
            }
        }

        public void UnSubscribe()
        {
            if (Subscribed && _func != null)
            {
                Subscribed = false;
                PresetHandler.OnPresetUpdated -= _func;
            }
        }

        public bool Subscribed { get; private set; }

        private Action<SAINPresetClass> _func;
    }
}