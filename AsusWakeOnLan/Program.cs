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
        AppDomain.CurrentDomain.ProcessExit += HandleExit;
        AppDomain.CurrentDomain.UnhandledException += HandleExit;

        Config config = ConfigHelper.LoadConfig();
        Console.WriteLine("Driver creation");
        driver = GetWebDriver();
        try
        {
            RunWol(driver, config);
            Console.WriteLine("Success");
        }
        catch (WolException exception)
        {
            Console.WriteLine(exception.Message);
            Console.WriteLine();
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
            DisposeDriver();
        }
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
        const bool isRelease = true;
        var options = new FirefoxOptions
        {
            AcceptInsecureCertificates = true,
        };
        if (isRelease)
        {
            options.AddArgument("--headless");
            // Size is important for consistent behaviour
            options.AddArgument("--window-size=1920,1080");
            return options;
        }
        else
        {
            return options;
        }
    }

    private static void RunWol(WebDriver driver, Config config)
    {
        string url = config.RootUrl + "/Main_Login.asp";

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
        TimeSpan defaultTimeout = driver.Manage().Timeouts().PageLoad;
        int defaultLoginTimeoutSec = config.LoadPageTimeOutMilliseconds;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(defaultLoginTimeoutSec);
        int connectionAttempt = 5;
        while (true)
        {
            try
            {
                driver.Navigate().GoToUrl(url);
                break;
            }
            catch (WebDriverException)
            {
                connectionAttempt--;
                if (connectionAttempt == 0)
                    throw new WolException("Can't connect to router");
                Console.WriteLine(" Can't connect to router. Reload.");
            }
        }
        driver.Manage().Timeouts().PageLoad = defaultTimeout;

        Console.WriteLine("Logging in");
        var webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitForActiveSeconds));
        IWebElement singInInput = webDriverWait.Until(d =>
        {
            ReadOnlyCollection<IWebElement>? elements = d.FindElements(By.ClassName(sessionIsBusyClass));
            if (elements.Count > 0)
                throw new WolException(elements[0].Text);

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
        singInInput.SendKeys(config.Login);
        driver.FindElement(By.Name(singInNamePassword)).SendKeys(config.Password);
        driver.FindElement(By.Id(singInIdButtonId)).Click();
        Console.WriteLine("Waking up");
        Uri uri = new Uri(driver.Url);
        url = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.Length - uri.LocalPath.Length) + wolLocalPath;
        driver.Navigate().GoToUrl(url);
        driver.FindElement(By.Name(macTextBoxName)).SendKeys(config.WakeUpMac);
        driver.FindElement(By.Id(wakeButtonId)).Click();
        Thread.Sleep(1000); // wait for waking up
        Console.WriteLine("Logging out");
        driver.ExecuteScript(logoutScript);
        driver.SwitchTo().Alert().Accept();
    }

    private static void HandleExit(object? sender, EventArgs e)
    {
        Console.WriteLine("Handle exit");
        DisposeDriver();
    }

    private static void DisposeDriver()
    {
        driver?.Dispose();
        driver = null;
    }
}

class WolException : Exception
{
    public WolException(string text) : base(text) { }
}