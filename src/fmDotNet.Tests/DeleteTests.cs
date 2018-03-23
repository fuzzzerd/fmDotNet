using System;
using System.Linq;
using System.Data;
using Xunit;

namespace fmDotNet.Tests
{
    [Collection("CompoundFind")]
    public class DeleteTests
    {
        [Fact]
        public void DeleteRecord_Should_DeleteThatRecord()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // find one and duplicate it, so we can delete it
            var fr = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord);
            var newRecID = fr.Execute().Tables[0].Rows[0]["recordID"].ToString();
            var dupReq = fms.CreateDuplicateRequest(newRecID);
            var dupRecID = dupReq.Execute();

            // act
            var returnCode = fms.CreateDeleteRequest(dupRecID).Execute();

            // assert
            Assert.Equal("0", returnCode);
        }

        [Fact]
        public void DeleteRequest_ShouldError_OnInvalid_RecordID()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // act // assert
             Assert.Throws<InvalidOperationException>(() => fms.CreateDeleteRequest(Guid.NewGuid().ToString()).Execute());
        }
    }
}