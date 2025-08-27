using OpenQA.Selenium;

namespace AsusWakeOnLan;

static class Program
{
    private static WebDriver? driver;

    private static void Main()
    {
        AppDomain.CurrentDomain.ProcessExit += HandleExit;
        AppDomain.CurrentDomain.UnhandledException += HandleExit;
        var outputHelper = new OutputHelper();

        Config config = ConfigHelper.LoadConfig();
        outputHelper.WriteLine("Driver creation");
        driver = WebDriverHelper.GetWebDriver(config.IsDebug);
        try
        {
            new WakeOnLan(driver, config, outputHelper).RunWol();
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
            AppDomain.CurrentDomain.ProcessExit -= HandleExit;
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