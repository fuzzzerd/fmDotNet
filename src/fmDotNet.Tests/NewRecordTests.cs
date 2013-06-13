using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class NewRecordTests
    {
        public NewRecordTests() { }

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
        public void NewRecord_ShouldCreate_NewRecordWithID()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var nrRequest = fms.CreateNewRecordRequest();
            nrRequest.AddField("Name", Guid.NewGuid().ToString());
            nrRequest.AddField("Description", "New Record Request Test Description");
            nrRequest.AddField("ColorID", "1");
            nrRequest.AddField("TypeID", "1");
            var response = nrRequest.Execute();

            // assert
            Assert.IsNotNull(response);

            // clean up
            fms.CreateDeleteRequest(response).Execute();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NewRecord_WithExistingIDShould_ThrowException()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            // act
            var nrRequest = fms.CreateNewRecordRequest();
            nrRequest.AddField("Name", Guid.NewGuid().ToString());
            nrRequest.AddField("Description", "New Record Request Test Description");
            nrRequest.AddField("ColorID", "1");
            nrRequest.AddField("TypeID", "1");
            nrRequest.AddField("ID", "1");
            var response = nrRequest.Execute();

            // assert
            Assert.IsNotNull(response);

            // clean up
            fms.CreateDeleteRequest(response).Execute();
        }
    }
}