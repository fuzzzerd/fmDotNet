using System;
using System.Linq;
using System.Data;
using Xunit;

namespace fmDotNet.Tests
{
    public class EditTests
    {
        [Fact]
        public void EditRecord_Should_UpdateRecord()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
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

            Assert.NotEqual("0", response);
            Assert.Equal(newName, op.Tables[0].Rows[0]["Name"].ToString());
            Assert.Equal(newDesc, op.Tables[0].Rows[0]["Description"].ToString());

            // clean up
            var e = fms.CreateEditRequest(recID);
            e.AddField("Name", nameBefore);
            e.AddField("Description", descBefore);
            e.Execute();
        }

        [Fact]
        public void EditRecord_Should_Throw_ForInvalid_RecordID()
        {
            // arrange 
            var fms = Setup.SetupFMSAxml();
            fms.SetDatabase("fmDotNet_Tests", false);
            fms.SetLayout("FindRequest.Tests");

            var newName = "Edit request test name";
            var newDesc = "This is the new description from the edit request that was just performed.";

            // act
            var edit = fms.CreateEditRequest(Guid.NewGuid().ToString());
            edit.AddField("Name", newName);
            edit.AddField("Description", newDesc);
            Assert.Throws<InvalidOperationException>(() => edit.Execute());

        }
    }
}