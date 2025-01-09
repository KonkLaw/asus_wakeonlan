using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

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
        driver = GetWebDriver(config.IsDebug);
        try
        {
            new WolHelper(driver, config).RunWol();
            Console.WriteLine("Success");
        }
        catch (WolException exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("Error:");
            Console.WriteLine(exception.Message);
            Console.WriteLine();
            Console.ResetColor();
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

    private static WebDriver GetWebDriver(bool isDebug)
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