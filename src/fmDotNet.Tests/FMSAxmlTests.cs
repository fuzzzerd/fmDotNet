using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fmDotNet.Tests
{
    [TestClass]
    public class FMSAxmlTests
    {
        public FMSAxmlTests() { }

        FMSAxml SetupFMSAxml()
        {
            var asr = new System.Configuration.AppSettingsReader();

            var fms = new FMSAxml(
                theServer: (string)asr.GetValue("TestServerName", typeof(string)),
                theAccount: (string)asr.GetValue("TestServerUser", typeof(string)),
                thePort: (int)asr.GetValue("TestServerPort", typeof(int)),
                thePW: (string)asr.GetValue("TestServerPass", typeof(string))
                );
            return fms;
        }

        #region "File/Server Independent Tests"
        //
        // These tests only require one hosted file with guest/anonymous access
        //

        [TestMethod]
        public void NewFMSAxml_Should_Populate_AvailableDatabases()
        {
            // arrange & act (in this case)
            var fms = this.SetupFMSAxml();

            // assert
            Assert.IsTrue(fms.AvailableDatabases.Count >= 1);
        }

        [TestMethod]
        public void SetDatabase_Should_Update_Current_Database()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.AreEqual(fms.AvailableDatabases.FirstOrDefault(), fms.CurrentDatabase);
        }

        [TestMethod]
        public void SetDatabase_NoSkip_ShouldRead_Layouts()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            
            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.IsTrue(fms.AvailableLayouts.Count >= 1);
        }

        [TestMethod]
        public void SetDatabase_SetSkip_Should_ReturnNo_Layouts()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), true);

            // assert
            Assert.IsTrue(fms.AvailableLayouts.Count == 0);
        }

        [TestMethod]
        public void SetDatabase_NoSkip_ShouldRead_Scripts()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.IsTrue(fms.AvailableScripts.Count >= 1);
        }

        [TestMethod]
        public void SetDatabase_SetSkip_Should_ReturnNo_Scripts()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), true);

            // assert
            Assert.IsTrue(fms.AvailableScripts.Count == 0);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMemberException))]
        public void SetDatabase_ToDatabaseThatIsNotThere_Should_Throw()
        {
            // arrange
            var fms = this.SetupFMSAxml();

            // act
            fms.SetDatabase(Guid.NewGuid().ToString());

            // assert
            Assert.IsTrue(fms.AvailableScripts.Count == 0);
        }

        [TestMethod]
        public void SetValidLayout_Should_Update_CurrentLayout()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var db = fms.AvailableDatabases.FirstOrDefault();
            fms.SetDatabase(db, false);
            var lay = fms.AvailableLayouts.FirstOrDefault();

            // act
            fms.SetLayout(lay);
            
            // assert
            Assert.AreEqual(lay, fms.CurrentLayout);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMemberException))]
        public void SetInvalidLayout_Should_Update_CurrentLayout()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            var db = fms.AvailableDatabases.FirstOrDefault();
            fms.SetDatabase(db, false);
            var lay = Guid.NewGuid().ToString();

            // act
            fms.SetLayout(lay);

            // assert
            Assert.AreEqual(lay, fms.CurrentLayout);
        }
        #endregion

        #region "Tests for fmDotNet.Tests.fmp12"
        [TestMethod]
        public void GetHardCodedValueListData_ShouldReturn_HardCoded()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Hard-Coded");

            // assert
            Assert.IsTrue(data.Count >= 1);
        }

        [TestMethod]
        public void GetHardCodedWithDupeValueListData_ShouldReturn_HardCodedWithDupe()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Hard-Coded-Dupe");

            // assert
            Assert.IsTrue(data.Count >= 1);
        }

        [TestMethod]
        public void GetValueListData_ShouldReturn_ValueList_Data()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Colors");

            // assert
            Assert.IsTrue(data.Count >= 1);
        }

        [TestMethod]
        public void GetValueList_ShouldReturn_ValueList()
        {
            // arrange
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueList("Colors");

            // assert
            Assert.IsTrue(data.Count() >= 1);
        }

        #endregion
    }
}