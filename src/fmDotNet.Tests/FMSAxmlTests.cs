using System;
using System.Linq;
using Xunit;

namespace fmDotNet.Tests
{

    public class FMSAxmlTests
    {
        #region "File/Server Independent Tests"
        //
        // These tests only require one hosted file with guest/anonymous access
        //

        [Fact]
        public void NewFMSAxml_Should_Populate_AvailableDatabases()
        {
            // arrange & act (in this case)
            var fms = Setup.SetupFMSAxml();

            // assert
            Assert.True(fms.AvailableDatabases.Count >= 1);
        }

        [Fact]
        public void SetDatabase_Should_Update_Current_Database()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.Equal(fms.AvailableDatabases.FirstOrDefault(), fms.CurrentDatabase);
        }

        [Fact]
        public void SetDatabase_NoSkip_ShouldRead_Layouts()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            
            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.True(fms.AvailableLayouts.Count >= 1);
        }

        [Fact]
        public void SetDatabase_SetSkip_Should_ReturnNo_Layouts()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), true);

            // assert
            Assert.True(fms.AvailableLayouts.Count == 0);
        }

        [Fact]
        public void SetDatabase_NoSkip_ShouldRead_Scripts()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), false);

            // assert
            Assert.True(fms.AvailableScripts.Count >= 1);
        }

        [Fact]
        public void SetDatabase_SetSkip_Should_ReturnNo_Scripts()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();

            // act
            fms.SetDatabase(fms.AvailableDatabases.FirstOrDefault(), true);

            // assert
            Assert.True(fms.AvailableScripts.Count == 0);
        }

        [Fact]
        public void SetDatabase_ToDatabaseThatIsNotThere_Should_Throw()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();

            // act &  assert
            Assert.Throws<MissingMemberException>(() => fms.SetDatabase(Guid.NewGuid().ToString()));
        }

        [Fact]
        public void SetValidLayout_Should_Update_CurrentLayout()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            var db = fms.AvailableDatabases.FirstOrDefault();
            fms.SetDatabase(db, false);
            var lay = fms.AvailableLayouts.FirstOrDefault();

            // act
            fms.SetLayout(lay);
            
            // assert
            Assert.Equal(lay, fms.CurrentLayout);
        }

        [Fact]
        public void SetInvalidLayout_Should_Update_CurrentLayout()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            var db = fms.AvailableDatabases.FirstOrDefault();
            fms.SetDatabase(db, false);
            var lay = Guid.NewGuid().ToString();

            // act
            Assert.Throws<MissingMemberException>(() => fms.SetLayout(lay));
        }
        #endregion

        #region "Tests for fmDotNet.Tests.fmp12"
        [Fact]
        public void GetHardCodedValueListData_ShouldReturn_HardCoded()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Hard-Coded");

            // assert
            Assert.True(data.Count >= 1);
        }

        [Fact]
        public void GetHardCodedWithDupeValueListData_ShouldReturn_HardCodedWithDupe()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Hard-Coded-Dupe");

            // assert
            Assert.True(data.Count >= 1);
        }

        [Fact]
        public void GetValueListData_ShouldReturn_ValueList_Data()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueListData("Colors");

            // assert
            Assert.True(data.Count >= 1);
        }

        [Fact]
        public void GetValueList_ShouldReturn_ValueList()
        {
            // arrange
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests");
            fms.SetLayout("FindRequest.Tests");

            // act
            var data = fms.GetValueList("Colors");

            // assert
            Assert.True(data.Count() >= 1);
        }

        #endregion
    }
}