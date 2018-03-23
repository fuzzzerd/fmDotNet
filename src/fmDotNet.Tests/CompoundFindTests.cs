using System;
using System.Linq;
using System.Data;
using Xunit;
using fmDotNet;

namespace fmDotNet.Tests
{
    [Collection("CompoundFind")]
    public class CompoundFindTests
    {
        [Fact]
        public void CompoundFind_Red_OR_Blue_ReturnsRedPlusBlue()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
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
            Assert.True(blueCount >= 1, "Must have one or more Blues.");
            Assert.True(redCount >= 1, "Must have one or more Reds.");
            Assert.Equal(blueCount + redCount, response.Tables[0].Rows.Count);
        }


        [Fact]
        public void CompoundFind_Red_AND_Glass_ReturnsRedAndGlass()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
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
            Assert.NotEqual(0, response.Tables[0].Rows.Count); // make sure we found at least one
            Assert.Equal(countCorrect, response.Tables[0].Rows.Count);
        }

        [Fact]
        public void CompoundFind_Red_AND_Glass_OMIT_Chipped_ReturnsRedAndGlass_Without_Chipped()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
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
            Assert.NotEqual(0, response.Tables[0].Rows.Count); // make sure we found at least one
            Assert.Equal(countCorrect, response.Tables[0].Rows.Count);
        }

        [Fact]
        public void CompoundFind_WithSortOrder_ShouldSort()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var cpfRequest = fms.CreateCompoundFindRequest();
            cpfRequest.AddSearchCriterion("CupTypes::Type", "Glass", false, false);
            cpfRequest.AddSearchCriterion("Colors::Name", "Red", false, false);
            cpfRequest.AddSearchCriterion("Description", "Chipped", false, true);

            cpfRequest.AddSortField("Name", Enumerations.Sort.Descend);

            var response = cpfRequest.Execute();

            var countCorrect = 0;
            foreach (DataRow dr in response.Tables[0].Rows)
            {
                if (dr["Colors::Name"].ToString() == "Red"
                    && dr["CupTypes::Type"].ToString() == "Glass"
                    && !dr["Description"].ToString().ToLower().Contains("chipped")) { countCorrect++; }

            }

            // assert
            Assert.NotEqual(0, response.Tables[0].Rows.Count); // make sure we found at least one
            Assert.Equal(countCorrect, response.Tables[0].Rows.Count);
        }
    }
}