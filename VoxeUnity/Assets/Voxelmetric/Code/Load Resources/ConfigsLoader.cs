using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Load_Resources
{
    [System.Obsolete]
    public class ConfigLoader<T>
    {
        private readonly Dictionary<string, T> configs = new Dictionary<string, T>();
        private readonly string[] configFolders;

        public ConfigLoader(string[] folders)
        {
            configFolders = folders;
        }

        private void LoadConfigs()
        {
            foreach (string configFolder in configFolders)
            {
                TextAsset[] configFiles = Resources.LoadAll<TextAsset>(configFolder);
                foreach (TextAsset configFile in configFiles)
                {
                    T config = JsonConvert.DeserializeObject<T>(configFile.text);
                    if (!configs.ContainsKey(config.ToString()))
                    {
                        configs.Add(config.ToString(), config);
                    }
                }
            }
        }

        public T GetConfig(string configName)
        {
            if (configs.Keys.Count == 0)
            {
                LoadConfigs();
            }

            T conf;
            if (configs.TryGetValue(configName, out conf))
            {
                return conf;
            }

            Debug.LogError("Config not found for " + configName + ". Using defaults");
            return conf;
        }

        public T[] AllConfigs()
        {
            if (configs.Keys.Count == 0)
            {
                LoadConfigs();
            }

            T[] configValues = new T[configs.Count];
            configs.Values.CopyTo(configValues, 0);
            return configValues;
        }
    }
}
