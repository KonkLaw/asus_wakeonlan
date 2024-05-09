using Newtonsoft.Json;
using System.Text;

namespace AsusWakeOnLan;

class Config
{
	public string? Login;
	public string? Password;
	public string? RootUrl;
	public string? WakeUpMac;
	public int LoadPageTimeOutMilliseconds;
}

static class ConfigHelper
{
	public static Config LoadConfig()
	{
		const string fileName = "settings.txt";
		string content = File.ReadAllText(fileName);
		Config result = JsonConvert.DeserializeObject<Config>(content)!;
		result.Login = Encoding.UTF8.GetString(Convert.FromBase64String(result.Login!));
		result.Password = Encoding.UTF8.GetString(Convert.FromBase64String(result.Password!));
		const int minTime = 1000;
		const int maxTime = 30_000;
		if (result.LoadPageTimeOutMilliseconds < minTime
		    || result.LoadPageTimeOutMilliseconds > maxTime)
			throw new ArgumentException($"Time should be > {minTime} and < {maxTime}");
		return result;
	}
}