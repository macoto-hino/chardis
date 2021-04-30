using System;
using Vintagestory.API.Common;

namespace Chardis
{
    public class ModConfig
    {
        private const string Filename = "ChardisModConfig.json";

        public string UpgradeItemCode = "game:gear-temporal";
        public int BaseSlots = 64;
        public int SlotsPerUpgrade = 32;

        public static ModConfig Load(ICoreAPI api)
        {
            ModConfig config;
            try
            {
                config = api.LoadModConfig<ModConfig>(Filename);
            }
            catch (Exception exception)
            {
                // on load failure, return defaults but log it.
                api.Logger.Warning($"Failed to load {Filename}: {exception.Message}");
                return new ModConfig();
            }
            if (config != null)
            {
                return config;
            }
            config = new ModConfig();
            Save(api, config);
            return config;
        }
        private static void Save(ICoreAPI api, ModConfig config) {
            api.StoreModConfig(config, Filename);
        }
    }
}