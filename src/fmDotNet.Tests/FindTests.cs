using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class FindTests
    {
        public FindTests() { }

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

        [TestMethod]
        public void FindAll_Should_Return_AllRecords()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.AllRecords);
            
            // act
            DataSet res = find.Execute();

            // assert
            Assert.IsTrue(res.Tables[0].Rows.Count >= 1);
        }

        [TestMethod]
        public void FindRandom_Should_Return_SingleRecord()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord);

            // act
            DataSet res = find.Execute();

            // assert
            Assert.AreEqual(1, res.Tables[0].Rows.Count);
        }

        [TestMethod]
        public void FindRequestForSpecific_ShouldReturn_SpecificRecord()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.Subset);
            
            // depends on data must make sure data is in file.
            var queryParm = "Greatest";
            find.AddSearchField("Name", queryParm);

            // act
            DataSet res = find.Execute();

            // assert
            Assert.IsTrue(res.Tables[0].Rows[0]["Name"].ToString().Contains(queryParm));
        }
    }
}