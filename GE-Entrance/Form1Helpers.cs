using System.Globalization;
using System;
using System.Linq;
using System.Net.NetworkInformation;

internal static class Form1Helpers
{

    public static string GetMacAddress()
    {
        NetworkInterface nic = NetworkInterface.GetAllNetworkInterfaces()
             .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                  n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                  n.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
        return nic?.GetPhysicalAddress().ToString();
    }

    // Parse a JS Date to DD.MM, HH:mm
    public static string ParseJavaScriptDate(string jsDate)
    {
        if (string.IsNullOrEmpty(jsDate))
        {
            return null;
        }

        try
        {
            DateTime dateTime = DateTime.ParseExact(jsDate, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            return dateTime.ToString("dd.MM, HH:mm");
        }
        catch (FormatException)
        {
            // Handle invalid format
            Console.WriteLine("Invalid date format.");
            return null;
        }
    }
}