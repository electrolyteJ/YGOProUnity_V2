namespace App.Core
{
    public interface IAppConfig
    {
        string Get(string key, string defaultValue);
    }
}
