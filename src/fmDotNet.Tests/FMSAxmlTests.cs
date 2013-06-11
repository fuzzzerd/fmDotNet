using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fmDotNet.Tests
{
    [TestClass]
    public class FMSAxmlTests
    {
        public FMSAxmlTests()
        {

        }

        FMSAxml SetupFMSAxml()
        {
            var asr = new System.Configuration.AppSettingsReader();

            var fms = new FMSAxml(
                theServer: (string)asr.GetValue("TestServerName", typeof(string)) as string,
                theAccount: (string)asr.GetValue("TestServerUser", typeof(string)) as string,
                thePort: (int)asr.GetValue("TestServerPort", typeof(int)),
                thePW: (string)asr.GetValue("TestServerPass", typeof(string)) as string
                );
            return fms;
        }


        [TestMethod]
        public void ListFiles_ShouldReturn_OneOrMore()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            var files = fms.GetFiles();

            // assert
            Assert.IsTrue(files.Count >= 1);
        }


        [TestMethod]
        public void SetDB_NoSkip_ShouldRead_Layouts()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var files = fms.GetFiles();
            
            // act
            fms.SetDB(files.FirstOrDefault(), false);

            // assert
            Assert.IsTrue(fms.AvailableLayouts.Count >= 1);
        }

        [TestMethod]
        public void SetDB_SetSkip_Should_ReturnNo_Layouts()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var files = fms.GetFiles();

            // act
            fms.SetDB(files.FirstOrDefault(), true);

            // assert
            Assert.IsTrue(fms.AvailableLayouts.Count == 0);
        }

        [TestMethod]
        public void SetDB_NoSkip_ShouldRead_Scripts()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var files = fms.GetFiles();

            // act
            fms.SetDB(files.FirstOrDefault(), false);

            // assert
            Assert.IsTrue(fms.AvailableScripts.Count >= 1);
        }

        [TestMethod]
        public void SetDB_SetSkip_Should_ReturnNo_Scripts()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var files = fms.GetFiles();

            // act
            fms.SetDB(files.FirstOrDefault(), true);

            // assert
            Assert.IsTrue(fms.AvailableScripts.Count == 0);
        }
    }
}