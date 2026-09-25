namespace OrduNet.Web.Services
{
    public interface ISiteSettingsService
    {
        string GetSetting(string key, string defaultValue = "");
        Dictionary<string, string> GetAllSettings();
    }
}
