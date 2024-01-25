using System.Collections.ObjectModel;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace AsusWakeOnLan;

class Program
{
    static void Main()
    {
        ReadConfig(out string rootUrl, out string mac, out string login, out string password);
        string url = rootUrl + "/Main_Login.asp";
        const string wolLocalPath = "/Main_WOL_Content.asp";

        const int waitForActiveSeconds = 10;
        const string singInIdLogin = "login_username";
        const string sessionIsBusyClass = "nologin-text";
        const string singInNamePassword = "login_passwd";
        const string singInIdButtonId = "button";
        const string macTextBoxName = "destIP";
        const string wakeButtonId = "cmdBtn";
        const string logoutScript = "javascript:logout();";


        Console.WriteLine("Driver creation");
        var driverService = FirefoxDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true;
        var options = new FirefoxOptions
        {
            AcceptInsecureCertificates = true,
        };
        options.AddArgument("--headless");
        WebDriver driver = new FirefoxDriver(driverService, options);
        bool wasException = true;
        try
        {
            Console.WriteLine("Load start page");
            //driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            driver.Navigate().GoToUrl(url);
            Console.WriteLine("Logging in");
            var webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitForActiveSeconds));
            IWebElement singInInput = webDriverWait.Until(d =>
            {
                ReadOnlyCollection<IWebElement>? elements = d.FindElements(By.ClassName(sessionIsBusyClass));
                if (elements.Count > 0)
                    throw new NoLoginException(elements[0].Text);
                IWebElement? element = d.FindElement(By.Id(singInIdLogin));
                return element is { Displayed: true, Enabled: true } ? element : null;
            })!;
            singInInput.SendKeys(login);
            driver.FindElement(By.Name(singInNamePassword)).SendKeys(password);
            driver.FindElement(By.Id(singInIdButtonId)).Click();
            Console.WriteLine("Waking up");
            Uri uri = new Uri(driver.Url);
            url = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.Length - uri.LocalPath.Length) + wolLocalPath;
            driver.Navigate().GoToUrl(url);
            driver.FindElement(By.Name(macTextBoxName)).SendKeys(mac);
            driver.FindElement(By.Id(wakeButtonId)).Click();
            Thread.Sleep(1000); // wait for waking up
            Console.WriteLine("Logging out");
            driver.ExecuteScript(logoutScript);
            driver.SwitchTo().Alert().Accept();
            wasException = false;
            Console.WriteLine("Success");
        }
        catch (NoLoginException exception)
        {
            Console.WriteLine(exception.Message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Console.WriteLine();
            Console.WriteLine(driver.PageSource);
            Console.WriteLine();
        }
        finally
        {
            driver.Quit();
        }

        if (wasException)
        {
            Console.WriteLine("Press any key");
            Console.ReadLine();
        }
    }

    private static void ReadConfig(out string rootUrl, out string mac, out string login, out string password)
    {
        string[] settings = File.ReadAllLines("settings.txt");
        login = Encoding.UTF8.GetString(Convert.FromBase64String(settings[0]));
        password = Encoding.UTF8.GetString(Convert.FromBase64String(settings[1]));
        rootUrl = settings[2];
        mac = settings[3];
    }
}

class NoLoginException : Exception
{
    public NoLoginException(string text) : base(text) { }
}