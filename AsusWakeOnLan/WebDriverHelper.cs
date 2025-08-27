using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace AsusWakeOnLan;

public static class WebDriverHelper
{
    public static WebDriver GetWebDriver(bool isDebug)
    {
        var driverService = FirefoxDriverService.CreateDefaultService();
        driverService.HideCommandPromptWindow = true;
        WebDriver webDriver = new FirefoxDriver(driverService, GetFirefoxOptions(isDebug));
        return webDriver;
    }

    private static FirefoxOptions GetFirefoxOptions(bool isDebug)
    {
        var options = new FirefoxOptions
        {
            AcceptInsecureCertificates = true,
        };
        if (!isDebug)
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
}