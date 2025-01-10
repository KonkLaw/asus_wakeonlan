using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AsusWakeOnLan;

readonly struct WolHelper
{
    private readonly WebDriver driver;
    private readonly Config config;

    public WolHelper(WebDriver driver, Config config)
    {
        this.driver = driver;
        this.config = config;
    }

    public void RunWol()
    {
        Console.WriteLine("Loading start page");
        LoadPage();
        Console.WriteLine("Logging in");
        Login();
        Console.WriteLine("Navigating to WOL");
        NavigateToWol();
        Console.WriteLine("Waking up");
        WakeUpAndWait();
        Console.WriteLine("Logging out");
        Logout();
        Console.WriteLine("Logged out");
    }

    private void LoadPage()
    {
        TimeSpan initialTimeout = driver.Manage().Timeouts().PageLoad;

        const int defaultLoginTimeoutSec = 1;
        const int defaultConnectionAttempt = 5;
        string url = config.RootUrl + "/Main_Login.asp";

        int connectionAttempt = defaultConnectionAttempt;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(defaultLoginTimeoutSec);
        do
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
                driver.Manage().Timeouts().PageLoad = 2 * driver.Manage().Timeouts().PageLoad;
            }
        } while (true);

        driver.Manage().Timeouts().PageLoad = initialTimeout;
    }

    private void Login()
    {
        const string sessionIsBusyClass = "nologin-text";
        const string logoutClass = "logout-text";
        const string singInIdLogin = "login_username";
        const string singInNamePassword = "login_passwd";
        const string singInIdButtonId = "button";
        const int waitForLoginSeconds = 15;

        var loginWait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitForLoginSeconds));
        IWebElement loginInput = loginWait.Until(d =>
        {
            // sometimes model is busy by other session
            ReadOnlyCollection<IWebElement>? elements = d.FindElements(By.ClassName(sessionIsBusyClass));
            if (elements.Count > 0)
                throw new WolException(elements[0].Text);

            // sometimes we may get logout screen. refresh solves the problem
            ReadOnlyCollection<IWebElement>? logoutElements = d.FindElements(By.ClassName(logoutClass));
            if (logoutElements.Count > 0)
            {
                d.Navigate().Refresh();
                Console.WriteLine(" Logout screen. Reload.");
                return null;
            }

            elements = d.FindElements(By.Id(singInIdLogin));
            if (elements.Count != 0 && elements[0].Displayed)
                return elements[0];
            Console.WriteLine(" Waiting for loading");
            return null;
        })!;
        loginInput.SendKeys(config.Login);
        driver.FindElement(By.Name(singInNamePassword)).SendKeys(config.Password);
        driver.FindElement(By.Id(singInIdButtonId)).Click();
    }

    private void NavigateToWol()
    {
        const string wolLocalPath = "/Main_WOL_Content.asp";
        const string udbStatusIconId = "usb_status";

        Uri uri = new Uri(driver.Url);
        string url = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.Length - uri.LocalPath.Length) + wolLocalPath;
        driver.Navigate().GoToUrl(url);

        // sometimes wake up is not working fully ok
        // one of ideas - wait until page is fully loaded
        // to track it we choose some element which is appears in the end of loading
        // and wait until it is displayed
        const int waitPageLoad = 5;
        var pageLoadWait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitPageLoad));
        _ = pageLoadWait.Until(d =>
        {
            IWebElement? icon = d.FindElement(By.Id(udbStatusIconId));
            if (icon is { Displayed: true, Enabled: true })
                return icon;
            Console.WriteLine(" Page is not fully loaded");
            return null;
        });
    }

    private void WakeUpAndWait()
    {
        const string captchaField = "captcha_field";
        const string macTextBoxName = "destIP";
        const string loadingIconId = "loadingIcon";
        const string wakeButtonId = "cmdBtn";

        ReadOnlyCollection<IWebElement>? captureFields = driver.FindElements(By.Id(captchaField));
        if (captureFields.Any(c => c.Displayed))
            throw new WolException("Captcha is required");

        driver.FindElement(By.Name(macTextBoxName)).SendKeys(config.WakeUpMac);

        IWebElement loadingIcon = driver.FindElement(By.Id(loadingIconId));
        driver.FindElement(By.Id(wakeButtonId)).Click();

        // Checking wait indicator. It should pop up and hide.
        Console.WriteLine(" Send request and wait...");
        Stopwatch time = Stopwatch.StartNew();
        bool wasShown = false;
        while (true)
        {
            bool isShowing = loadingIcon.Enabled && loadingIcon.Displayed;
            if (wasShown && !isShowing)
            {
                Console.WriteLine($" Wait was ended successfully ({time.ElapsedMilliseconds} ms)");
                break;
            }
            if (isShowing)
                wasShown = true;

            if (time.ElapsedMilliseconds > TimeSpan.FromSeconds(5).TotalMilliseconds)
            {
                Console.WriteLine(" wait was tool long - exit");
                break;
            }
        }
    }

    private void Logout()
    {
        IWebElement logOutButton = driver.FindElement(By.XPath("//div[text()=\"Logout\"]"));
        logOutButton.Click();
        
        const int exitAlertWaitSeconds = 5;
        var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(exitAlertWaitSeconds));
        IAlert alert = alertWait.Until(webDriver =>
        {
            try
            {
                return webDriver.SwitchTo().Alert();
            }
            catch (NoAlertPresentException e)
            {
                Console.WriteLine(" can't locate logout");
                logOutButton.Click();
                return null;
            }
        })!;
        alert.Accept();
    }
}