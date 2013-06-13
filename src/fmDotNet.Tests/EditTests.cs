using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace fmDotNet.Tests
{
    [TestClass]
    public class EditTests
    {
        public EditTests() { }

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
        public void EditRecord_Should_UpdateRecord()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            var random = fms.CreateFindRequest(Enumerations.SearchType.RandomRecord).Execute();
            var recID = random.Tables[0].Rows[0]["recordID"].ToString();
            var nameBefore = random.Tables[0].Rows[0]["Name"].ToString();
            var descBefore = random.Tables[0].Rows[0]["Description"].ToString();

            var newName = "Edit request test name";
            var newDesc = "This is the new description from the edit request that was just performed.";

            // act
            var edit = fms.CreateEditRequest(recID);
            edit.AddField("Name", newName);
            edit.AddField("Description", newDesc);
            var response = edit.Execute();

            // assert
            var refind = fms.CreateFindRequest(Enumerations.SearchType.Subset);
            refind.SetRecordID(recID);
            var op = refind.Execute();

            Assert.AreNotEqual("0", response);
            Assert.AreEqual(newName, op.Tables[0].Rows[0]["Name"].ToString());
            Assert.AreEqual(newDesc, op.Tables[0].Rows[0]["Description"].ToString());

            // clean up
            var e = fms.CreateEditRequest(recID);
            e.AddField("Name", nameBefore);
            e.AddField("Description", descBefore);
            e.Execute();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void EditRecord_Should_Throw_ForInvalid_RecordID()
        {
            // arrange 
            var fms = this.SetupFMSAxml();
            fms.SetDatabase("fmDotNet.Tests", false);
            fms.SetLayout("FindRequest.Tests");

            var newName = "Edit request test name";
            var newDesc = "This is the new description from the edit request that was just performed.";

            // act
            var edit = fms.CreateEditRequest(Guid.NewGuid().ToString());
            edit.AddField("Name", newName);
            edit.AddField("Description", newDesc);
            var response = edit.Execute();

            // assert
            Assert.AreNotEqual("0", response);
        }
    }
}