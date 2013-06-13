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
        public void DuplicateRecord_Should_CreateNewSame()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("DuplicateRequest.Tests");

            // find how many red and how many blue we have
            var cnr = fms.CreateNewRecordRequest();
            var name = Guid.NewGuid().ToString();
            cnr.AddField("Name", name);
            var newRecID = cnr.Execute();
            
            // act
            var dupReq = fms.CreateDuplicateRequest(newRecID);
            var dupRecID = dupReq.Execute();

            // assert

            var freq = fms.CreateFindRequest(Enumerations.SearchType.Subset);
            freq.AddSearchField("Name", name);
            var ds = freq.Execute();
            
            Assert.AreEqual(2, ds.Tables[0].Rows.Count);

            // clean up
            fms.CreateDeleteRequest(newRecID).Execute();
            fms.CreateDeleteRequest(dupRecID).Execute();
        }
    }
}