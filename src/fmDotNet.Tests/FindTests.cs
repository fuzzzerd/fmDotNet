using System;
using System.Linq;
using System.Data;
using Xunit;

namespace fmDotNet.Tests
{
    public class FindTests
    {
        [Fact]
        public void FindAll_Should_Return_AllRecords()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.AllRecords);
            
            // act
            DataSet res = find.Execute();

            // assert
            Assert.True(res.Tables[0].Rows.Count >= 1);
        }

        [Fact]
        public void FindRandom_Should_Return_SingleRecord()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord);

            // act
            DataSet res = find.Execute();

            // assert
            Assert.Equal(1, res.Tables[0].Rows.Count);
        }

        [Fact]
        public void FindRequestForSpecific_ShouldReturn_SpecificRecord()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");
            var find = fms.CreateFindRequest(Enumerations.SearchType.Subset);
            
            // depends on data must make sure data is in file.
            var queryParm = "Greatest";
            find.AddSearchField("Name", queryParm);

            // act
            DataSet res = find.Execute();

            // assert
            Assert.Contains(queryParm, res.Tables[0].Rows[0]["Name"].ToString());
        }
    }
}