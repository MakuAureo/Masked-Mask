using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;

namespace MaskedMask;

internal class MaskedMaskConfig
{
    public bool lethalConfigLoaded;

    public readonly ConfigEntry<int> convertedMaskValue;

    public MaskedMaskConfig(ConfigFile cfg)
    {
        lethalConfigLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(MaskedMask.LethalConfigGUID);

        cfg.SaveOnConfigSet = false;

        convertedMaskValue = cfg.Bind(
                "General",
                "Converted Mask Value",
                40,
                new ConfigDescription("The value of masks spawned by a Masked converting a player", new AcceptableValueRange<int>(28, 51))
                );

        ClearOrphanedEntries(cfg); 
        cfg.Save(); 
        cfg.SaveOnConfigSet = true;
        
        if (lethalConfigLoaded)
        {
            AddLethalConfigItems();
            ConfigLethalConfigModEntry();
        }
    }

    private void ClearOrphanedEntries(ConfigFile cfg) 
    { 
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries"); 
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg); 
        orphanedEntries.Clear(); 
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void AddLethalConfigItems()
    {
        LethalConfig.ConfigItems.IntSliderConfigItem convertedMaskValueConfig = new(convertedMaskValue, new LethalConfig.ConfigItems.Options.IntSliderOptions { Min = 28, Max = 51, RequiresRestart = false });
        LethalConfig.LethalConfigManager.AddConfigItem(convertedMaskValueConfig);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void ConfigLethalConfigModEntry()
    {
        LethalConfig.LethalConfigManager.SetModDescription("Masked Mask configs");
    }
}
