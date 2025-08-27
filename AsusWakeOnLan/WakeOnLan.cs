using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace AsusWakeOnLan;

class WakeOnLan
{
    private readonly WebDriver driver;
    private readonly Config config;
    private readonly OutputHelper outputHelper;

    public WakeOnLan(WebDriver driver, Config config, OutputHelper outputHelper)
    {
        this.driver = driver;
        this.config = config;
        this.outputHelper = outputHelper;
    }

    public void RunWol()
    {
        outputHelper.WriteLine("Loading start page");
            outputHelper.Indent();
            LoadPage();
            outputHelper.Unindent();
        outputHelper.WriteLine("Logging in");
            outputHelper.Indent();
            Login();
            outputHelper.Unindent();
        outputHelper.WriteLine("Navigating to WOL");
            outputHelper.Indent();
            NavigateToWol();
            outputHelper.Unindent();
        outputHelper.WriteLine("Waking up");
            outputHelper.Indent();
            WakeUpAndWait();
            outputHelper.Unindent();
        outputHelper.WriteLine("Logging out");
            outputHelper.Indent();
            Logout();
            outputHelper.Unindent();
        outputHelper.WriteLine("Ping PC");
            outputHelper.Indent();
            PingAndWait();
            outputHelper.Unindent();
    }

    private void LoadPage()
    {
        TimeSpan initialTimeout = driver.Manage().Timeouts().PageLoad;

        const int defaultLoginTimeoutSec = 1;
        string url = config.RootUrl + "/Main_Login.asp";

        int connectionAttempt = config.LoadPageAttempts;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(defaultLoginTimeoutSec);
        do
        {
            try
            {
                driver.Navigate().GoToUrl(url);
                outputHelper.WriteLine("Ok");
                break;
            }
            catch (WebDriverException exception)
            {
                connectionAttempt--;
                if (connectionAttempt == 0)
                    throw new WolException("Can't connect to router");


                TimeSpan newTimeout = 2 * driver.Manage().Timeouts().PageLoad;
                double newTimeoutSeconds = newTimeout.TotalSeconds;
                outputHelper.WriteLine($"Can't load router page. Reload with wait = {newTimeoutSeconds}. Exception: {exception.Message}");
                driver.Manage().Timeouts().PageLoad = newTimeout;
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

        var loginWait = new WebDriverWait(driver, TimeSpan.FromSeconds(config.WaitForLoginSeconds));
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
                outputHelper.WriteLine("Logout screen. Reload.");
                return null;
            }

            elements = d.FindElements(By.Id(singInIdLogin));
            if (elements.Count != 0 && elements[0].Displayed)
                return elements[0];
            outputHelper.WriteLine("Waiting for loading");
            return null;
        })!;
        loginInput.SendKeys(config.Login);
        driver.FindElement(By.Name(singInNamePassword)).SendKeys(config.Password);
        driver.FindElement(By.Id(singInIdButtonId)).Click();
        outputHelper.WriteLine("Ok");
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
        var pageLoadWait = new WebDriverWait(driver, TimeSpan.FromSeconds(config.WaitWolPageLoadSeconds));
        _ = pageLoadWait.Until(d =>
        {
            IWebElement? icon = d.FindElement(By.Id(udbStatusIconId));
            if (icon is { Displayed: true, Enabled: true })
            {
                outputHelper.WriteLine("Ok");
                return icon;
            }
            outputHelper.WriteLine("Page is not fully loaded");
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
        outputHelper.WriteLine("Send request and wait...");
        Stopwatch time = Stopwatch.StartNew();
        bool wasShown = false;
        while (true)
        {
            bool isShowing = loadingIcon.Enabled && loadingIcon.Displayed;
            if (wasShown && !isShowing)
            {
                outputHelper.WriteLine($"Wait was ended successfully ({time.ElapsedMilliseconds} ms)");
                break;
            }
            if (isShowing)
                wasShown = true;

            if (time.ElapsedMilliseconds > TimeSpan.FromSeconds(config.WaitIndicatorWaitingSeconds).TotalMilliseconds)
            {
                outputHelper.WriteLine("Wait was tool long - exit");
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
                outputHelper.WriteLine("can't locate logout");
                logOutButton.Click();
                return null;
            }
        })!;
        outputHelper.WriteLine("Ok");
        alert.Accept();
    }

    private void PingAndWait()
    {
        using (Ping ping = new())
        {
            int attempts = config.PingAttempts;
            while (attempts > 0)
            {
                PingReply reply = ping.Send(config.RemoteIP,
                    (int)TimeSpan.FromSeconds(config.PingTimeoutSeconds).TotalMilliseconds);
                if (reply.Status == IPStatus.Success)
                {
                    outputHelper.WriteLine($"Ping successful: {reply.RoundtripTime} ms");
                    return;
                }
                outputHelper.WriteLine($"Ping failed: {reply.Status}");
                attempts--;
            }
            outputHelper.WriteLine("Exit by attempts count");
        }
    }
}