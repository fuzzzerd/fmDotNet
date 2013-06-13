using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class DuplicateTests
    {
        public DuplicateTests() { }

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
        public void DuplicateRecord_Should_Duplicate_With_New_RecordID()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            var fr = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord);
            var newRecID = fr.Execute().Tables[0].Rows[0]["recordID"].ToString();
            
            // act
            var dupReq = fms.CreateDuplicateRequest(newRecID);
            var dupRecID = dupReq.Execute(); 

            // assert
            Assert.IsNotNull(dupRecID);

            // clean up
            fms.CreateDeleteRequest(dupRecID).Execute();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DuplicateRequest_ShouldError_OnInvalid_RecordID()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // act
            var returnCode = fms.CreateDeleteRequest(Guid.NewGuid().ToString()).Execute();

            // assert
            Assert.AreNotEqual("0", returnCode);
        }
    }
}