
using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace fmDotNet.Tests 
{
    public static class Setup
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
                return config;
        }
        
        public static FMSAxml SetupFMSAxml()
        {
            var config = InitConfiguration();

            var fms = new FMSAxml(
                theServer: config["TestServerName"],
                theAccount: config["TestServerUser"],
                thePort: Convert.ToInt32(config["TestServerPort"]),
                thePW: config["TestServerPass"]
                );
            return fms;
        }
    }
}
