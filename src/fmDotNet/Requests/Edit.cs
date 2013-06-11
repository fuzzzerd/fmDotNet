/*
 * Revisions:
 *  # NB - 10/28/2008 10:45:20 AM - Source File Created 
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 */
using System;
using System.Xml;

namespace fmDotNet.Requests
{
    public class Edit
    {
        private FMSAxml fms;
        private string theRequest;
        private string editCommand;
        private int fieldCounter = 0;
        private string fieldString;
        // private string recordRequest;
        private string script;

        /// <summary>
        /// Creates an instance of the Edit request class.
        /// </summary>
        /// <param name="f">The instance of the outer FMSAxml class</param>
        /// <param name="recID">The FileMaker RecordID of the row to edit.</param>
        /// <remarks>While this class is publicly available, use the "CreateEditRequest" method to create a new request.</remarks>
        internal Edit(FMSAxml f, string recID)
        {
            fms = f;
            editCommand = "&-edit";
            theRequest += "&-recid=" + recID;
        }
        // end constructors

        /// <summary>
        /// Executes the edit request.  Returns modification ID.
        /// </summary>
        /// <returns></returns>
        public string Execute()
        {
            if (fms.ResponseLayout.Length > 0)
                theRequest += "&-lay.response=" + fms.ResponseLayout;

            theRequest += script;
            theRequest += fieldString;
            theRequest += editCommand;

            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData = "-db=" + fms.CurrentDatabase + "&-lay=" + fms.CurrentLayout + theRequest;
            String errorCode = "";
            String theRecordID = "";
            String theModID = "";

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    fms.FMAccount,
                    fms.FMPassword,
                    fms.ResponseTimeout,
                    fms.DTDValidation);

                /* loop through the XML document returned by FMSA */
                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name.ToLower())
                    {
                        case "error":
                            errorCode = rootNode.Attributes.GetNamedItem("code").Value;
                            if (errorCode != "0")
                                FMSAxml.HandleFMSerrors(errorCode);
                            break;

                        case "resultset":
                            /* this is where the data is */
                            /* we just need the recid */
                            foreach (XmlNode rec in rootNode.ChildNodes)
                            {
                                /* recordid, mod id is in the attributes */
                                foreach (XmlAttribute attrib in rec.Attributes)
                                {
                                    switch (attrib.Name)
                                    {
                                        case "record-id":
                                            theRecordID = attrib.Value;
                                            break;

                                        case "mod-id":
                                            theModID = attrib.Value;
                                            break;
                                    }
                                } /*  for each attrib */
                            } /*   for each rec  */
                            break;
                    } /* switch rootnode name */
                } // foreach root node
                /* return the result
                 */
                return theModID;
            }  // try
            catch (Exception ex)
            {
                Tools.LogUtility.WriteEntry(ex, System.Diagnostics.EventLogEntryType.Error);
                throw;
            } // catch
        }

        /// <summary>
        /// The modification ID is an incremental counter that specifies the current version of a record. 
        /// By specifying a modification ID you can make sure that you are editing the current version of a record. 
        /// If the modification ID value you specify does not match the current modification ID value in the database, 
        /// the edit query command is not allowed and an error code is returned.
        /// </summary>
        /// <param name="modID">The mod ID</param>
        public void SetModID(string modID)
        {
            theRequest += "&-modid=" + modID;
        }

        /// <summary>
        /// Adds a field name/value pair to the request.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        public void AddField(string fieldName, string fieldValue)
        {
            fieldCounter++;
            fieldString += "&" + Uri.EscapeUriString(fieldName) + "=" + Uri.EscapeUriString(fieldValue);
        }

        /// <summary>
        /// Specifies what script to run after the -new command is executed.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScript(string theScript)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript);
        }
        /// <summary>
        /// Specifies what script to run after the -new command is executed.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        /// <param name="parameter">The parameter value.</param>
        public void AddScript(string theScript, string parameter)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript) + "&-script.param=" + Uri.EscapeUriString(parameter);
        }
    };
}