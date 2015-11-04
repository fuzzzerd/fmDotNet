using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class DeleteTests
    {
        public DeleteTests() { }

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
        public void DeleteRecord_Should_DeleteThatRecord()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
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
            Assert.AreEqual("0", returnCode);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeleteRequest_ShouldError_OnInvalid_RecordID()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // act
            var returnCode = fms.CreateDeleteRequest(Guid.NewGuid().ToString()).Execute();

            // assert
            Assert.AreNotEqual("0", returnCode);
        }
    }
}