using System;
using System.Linq;
using System.Data;
using Xunit;

namespace fmDotNet.Tests
{
    public class DuplicateTests
    {

        [Fact]
        public void DuplicateRecord_Should_Duplicate_With_New_RecordID()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            var fr = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord);
            var newRecID = fr.Execute().Tables[0].Rows[0]["recordID"].ToString();
            
            // act
            var dupReq = fms.CreateDuplicateRequest(newRecID);
            var dupRecID = dupReq.Execute(); 

            // assert
            Assert.NotNull(dupRecID);

            // clean up
            fms.CreateDeleteRequest(dupRecID).Execute();
        }

        [Fact]
        public void DuplicateRequest_ShouldError_OnInvalid_RecordID()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // act// assert
            Assert.Throws<InvalidOperationException>(() => fms.CreateDeleteRequest(Guid.NewGuid().ToString()).Execute());
        }
    }
}