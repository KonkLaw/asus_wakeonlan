using System.Net;
using Newtonsoft.Json;
using System.Text;

namespace AsusWakeOnLan;

class Config
{
    public bool IsDebug { get; init; }

    public string Login {get; init;}
	public string Password {get; init; }
	public string RootUrl {get; init; }
	public string WakeUpMac {get; init; }
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress RemoteIP { get; init; }

    public int LoadPageAttempts { get; init; }
    public double WaitForLoginSeconds { get; init; }
    public double WaitWolPageLoadSeconds { get; init; }
    public double WaitIndicatorWaitingSeconds { get; init; }
    public double PingTimeoutSeconds { get; init; }
    public int PingAttempts { get; init; }

    public Config(
        bool isDebug,
        string login, string password, string rootUrl, string wakeUpMac, IPAddress remoteIP,
        int loadPageAttempts, double waitForLoginSeconds, double waitWolPageLoadSeconds,
        double waitIndicatorWaitingSeconds, double pingTimeoutSeconds, int pingAttempts)
    {
        IsDebug = isDebug;
        Login = login;
        Password = password;
        RootUrl = rootUrl;
        WakeUpMac = wakeUpMac;
        RemoteIP = remoteIP;
        LoadPageAttempts = loadPageAttempts;
        WaitForLoginSeconds = waitForLoginSeconds;
        WaitWolPageLoadSeconds = waitWolPageLoadSeconds;
        WaitIndicatorWaitingSeconds = waitIndicatorWaitingSeconds;
        PingTimeoutSeconds = pingTimeoutSeconds;
        PingAttempts = pingAttempts;
    }

    public Config CloneWithNewCredentials(string newLogin, string newPassword)
        => new(
            IsDebug,
            newLogin, newPassword, RootUrl, WakeUpMac, RemoteIP,
            LoadPageAttempts, WaitForLoginSeconds, WaitWolPageLoadSeconds,
            WaitIndicatorWaitingSeconds, PingTimeoutSeconds, PingAttempts
        );
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
        if (result.RemoteIP == null)
            throw new ArgumentException("RemoteIP is null");

        result = result.CloneWithNewCredentials(
            Encoding.UTF8.GetString(Convert.FromBase64String(result.Login!)),
            Encoding.UTF8.GetString(Convert.FromBase64String(result.Password!)));

        return result;
	}
}

class IPAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress? ReadJson(JsonReader reader, Type objectType, IPAddress? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return reader.Value is string value ? IPAddress.Parse(value) : null;
    }

    public override void WriteJson(JsonWriter writer, IPAddress? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }
}