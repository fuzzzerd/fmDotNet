using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class CompoundFindTests
    {
        public CompoundFindTests() { }

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
        public void CompoundFind_Red_OR_Blue_ReturnsRedPlusBlue()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");
            // find how many red and how many blue we have
            var find = fms.CreateFindRequest(Enumerations.SearchType.AllRecords);
            DataSet res = find.Execute();
            var blueCount = 0;
            var redCount = 0;
            foreach (DataRow dr in res.Tables[0].Rows)
            {
                if (dr["Colors::Name"].ToString() == "Red") { redCount++; }
                if (dr["Colors::Name"].ToString() == "Blue") { blueCount++; }
            }

            // act
            var cpfRequest = fms.CreateCompoundFindRequest();
            cpfRequest.AddSearchCriterion("Colors::Name", "Blue", true, false);
            cpfRequest.AddSearchCriterion("Colors::Name", "Red", true, false);
            var response = cpfRequest.Execute();

            // assert
            Assert.IsTrue(blueCount >= 1, "Must have one or more Blues.");
            Assert.IsTrue(redCount >= 1, "Must have one or more Reds.");
            Assert.AreEqual(blueCount + redCount, response.Tables[0].Rows.Count);
        }


        [TestMethod]
        public void CompoundFind_Red_AND_Glass_ReturnsRedAndGlass()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var cpfRequest = fms.CreateCompoundFindRequest();
            cpfRequest.AddSearchCriterion("CupTypes::Type", "Glass", false, false);
            cpfRequest.AddSearchCriterion("Colors::Name", "Red", false, false);
            var response = cpfRequest.Execute();

            var countCorrect = 0;
            foreach (DataRow dr in response.Tables[0].Rows)
            {
                if (dr["Colors::Name"].ToString() == "Red"
                    && dr["CupTypes::Type"].ToString() == "Glass") { countCorrect++; }
                
            }

            // assert
            Assert.AreNotEqual(0, response.Tables[0].Rows.Count, "No records found, check database data"); // make sure we found at least one
            Assert.AreEqual(countCorrect, response.Tables[0].Rows.Count);
        }

        [TestMethod]
        public void CompoundFind_Red_AND_Glass_OMIT_Chipped_ReturnsRedAndGlass_Without_Chipped()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var cpfRequest = fms.CreateCompoundFindRequest();
            cpfRequest.AddSearchCriterion("CupTypes::Type", "Glass", false, false);
            cpfRequest.AddSearchCriterion("Colors::Name", "Red", false, false);
            cpfRequest.AddSearchCriterion("Description", "Chipped", false, true);
            var response = cpfRequest.Execute();

            var countCorrect = 0;
            foreach (DataRow dr in response.Tables[0].Rows)
            {
                if (dr["Colors::Name"].ToString() == "Red"
                    && dr["CupTypes::Type"].ToString() == "Glass"
                    && !dr["Description"].ToString().ToLower().Contains("chipped")) { countCorrect++; }

            }

            // assert
            Assert.AreNotEqual(0, response.Tables[0].Rows.Count, "No records found, check database data"); // make sure we found at least one
            Assert.AreEqual(countCorrect, response.Tables[0].Rows.Count);
        }
    }
}