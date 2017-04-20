using System.Configuration;

namespace svn_assist_client
{
    public static class ConfigHelper
    {
        public static string GetAppConfig(string strKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == strKey)
                {
                    return config.AppSettings.Settings[strKey].Value.ToString();
                }
            }
            return null;
        }
        public static void UpdateAppConfig(string newKey, string newValue)
        {
            bool exist = false;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach(string key in config.AppSettings.Settings.AllKeys)
            {
                if(key == newKey)
                {
                    exist = true;
                }
            }
            if(exist)
            {
                config.AppSettings.Settings.Remove(newKey);
            }
            config.AppSettings.Settings.Add(newKey, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
