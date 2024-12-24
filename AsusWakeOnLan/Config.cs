using Newtonsoft.Json;
using System.Text;

namespace AsusWakeOnLan;

class Config
{
    public string? Login {get; init;}
	public string? Password {get; init; }
	public string? RootUrl {get; init; }
	public string? WakeUpMac {get; init; }
    public bool IsDebug { get; init; }

	public Config(string? login, string? password, string? rootUrl, string? wakeUpMac, bool isRelease)
    {
        Login = login;
        Password = password;
        RootUrl = rootUrl;
        WakeUpMac = wakeUpMac;
        IsDebug = isRelease;
    }

    public Config CloneWithNewCredentials(string newLogin, string newPassword)
        => new(newLogin, newPassword, RootUrl, WakeUpMac, IsDebug);
}

static class ConfigHelper
{
	public static Config LoadConfig()
	{
		const string fileName = "settings.txt";
		string content = File.ReadAllText(fileName);
		Config result = JsonConvert.DeserializeObject<Config>(content)!;

		if (result.Login == null)
            throw new ArgumentException("Login is null");
		if (result.Password == null)
            throw new ArgumentException("Password is null");
		if (result.RootUrl == null)
            throw new ArgumentException("RootUrl is null");
		if (result.WakeUpMac == null)
            throw new ArgumentException("WakeUpMac is null");

        result = result.CloneWithNewCredentials(
            Encoding.UTF8.GetString(Convert.FromBase64String(result.Login!)),
            Encoding.UTF8.GetString(Convert.FromBase64String(result.Password!)));

        return result;
	}
}