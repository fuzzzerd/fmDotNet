using System;
using System.Linq;
using System.Data;
using Xunit;

namespace fmDotNet.Tests
{
    [Collection("CompoundFind")]
    public class NewRecordTests
    {
        [Fact]
        public void NewRecord_ShouldCreate_NewRecordWithID()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var nrRequest = fms.CreateNewRecordRequest();
            nrRequest.AddField("Name", Guid.NewGuid().ToString());
            nrRequest.AddField("Description", "New Record Request Test Description");
            nrRequest.AddField("ColorID", "1");
            nrRequest.AddField("TypeID", "1");
            var response = nrRequest.Execute();

            // assert
            Assert.NotNull(response);

            // clean up
            fms.CreateDeleteRequest(response).Execute();
        }

        [Fact]
        public void NewRecord_WithExistingIDShould_ThrowException()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var nrRequest = fms.CreateNewRecordRequest();
            nrRequest.AddField("Name", Guid.NewGuid().ToString());
            nrRequest.AddField("Description", "New Record Request Test Description");
            nrRequest.AddField("ColorID", "1");
            nrRequest.AddField("TypeID", "1");
            nrRequest.AddField("ID", "1");

            string response = "";

            Assert.Throws<InvalidOperationException>(() => response = nrRequest.Execute());
        }
    }
}