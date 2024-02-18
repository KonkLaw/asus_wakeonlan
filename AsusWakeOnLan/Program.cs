using System.Collections.ObjectModel;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace AsusWakeOnLan;

class Program
{
    private static WebDriver? driver;

    private static void Main()
    {
        AppDomain.CurrentDomain.ProcessExit += HandleByDriverDisposing;
        AppDomain.CurrentDomain.UnhandledException += HandleByDriverDisposing;

        LoginInfo loginInfo = ReadConfig();
        Console.WriteLine("Driver creation");
        driver = GetWebDriver();
        try
        {
            RunWol(driver, loginInfo);
            Console.WriteLine("Success");
        }
        catch (NoLoginException exception)
        {
            Console.WriteLine(exception.Message);
            Console.ReadLine();
            Console.WriteLine("Press any key");
            Console.ReadLine();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Console.WriteLine();
            Console.WriteLine("Press any key");
            Console.ReadLine();
        }
        finally
        {
            driver.Quit();
        }
    }

    private static LoginInfo ReadConfig()
    {
        string[] settings = File.ReadAllLines("settings.txt");
        string login = Encoding.UTF8.GetString(Convert.FromBase64String(settings[0]));
        string password = Encoding.UTF8.GetString(Convert.FromBase64String(settings[1]));
        string rootUrl = settings[2];
        string mac = settings[3];
        return new LoginInfo(login, password, rootUrl, mac);
    }

    private static WebDriver GetWebDriver()
    {
        var driverService = FirefoxDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true;
        WebDriver webDriver = new FirefoxDriver(driverService, GetFirefoxOptions());
        return webDriver;
    }

    private static FirefoxOptions GetFirefoxOptions()
    {
        var options = new FirefoxOptions
        {
            AcceptInsecureCertificates = true,
        };
        options.AddArgument("--headless");
        // Size is important for consistent behaviour
        options.AddArgument("--window-size=1920,1080");
        return options;
    }

    private static void RunWol(WebDriver driver, LoginInfo loginInfo)
    {
        string url = loginInfo.RootUrl + "/Main_Login.asp";

        const string wolLocalPath = "/Main_WOL_Content.asp";
        const int waitForActiveSeconds = 15;
        const string logoutClass = "logout-text";
        const string singInIdLogin = "login_username";
        const string sessionIsBusyClass = "nologin-text";
        const string singInNamePassword = "login_passwd";
        const string singInIdButtonId = "button";
        const string macTextBoxName = "destIP";
        const string wakeButtonId = "cmdBtn";
        const string logoutScript = "javascript:logout();";

        Console.WriteLine("Load start page");
        driver.Navigate().GoToUrl(url);
        Console.WriteLine("Logging in");
        var webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitForActiveSeconds));
        IWebElement singInInput = webDriverWait.Until(d =>
        {
            ReadOnlyCollection<IWebElement>? elements = d.FindElements(By.ClassName(sessionIsBusyClass));
            if (elements.Count > 0)
                throw new NoLoginException(elements[0].Text);

            ReadOnlyCollection<IWebElement>? logoutElements = d.FindElements(By.ClassName(logoutClass));
            if (logoutElements.Count > 0)
            {
                d.Navigate().Refresh();
                Console.WriteLine(" Logout screen. Reload.");
                return null;
            }

            Console.WriteLine(" Waiting for load");
            elements = d.FindElements(By.Id(singInIdLogin));
            if (elements.Count == 0)
                return null;
            IWebElement element = elements[0];
            if (element.Displayed)
                return element;
            return null;
        })!;
        singInInput.SendKeys(loginInfo.Login);
        driver.FindElement(By.Name(singInNamePassword)).SendKeys(loginInfo.Password);
        driver.FindElement(By.Id(singInIdButtonId)).Click();
        Console.WriteLine("Waking up");
        Uri uri = new Uri(driver.Url);
        url = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.Length - uri.LocalPath.Length) + wolLocalPath;
        driver.Navigate().GoToUrl(url);
        driver.FindElement(By.Name(macTextBoxName)).SendKeys(loginInfo.Mac);
        driver.FindElement(By.Id(wakeButtonId)).Click();
        Thread.Sleep(1000); // wait for waking up
        Console.WriteLine("Logging out");
        driver.ExecuteScript(logoutScript);
        driver.SwitchTo().Alert().Accept();
    }

    private static void HandleByDriverDisposing(object? sender, EventArgs e) => driver?.Dispose();
}

record LoginInfo(string Login, string Password, string RootUrl, string Mac);

class NoLoginException : Exception
{
    public NoLoginException(string text) : base(text) { }
}