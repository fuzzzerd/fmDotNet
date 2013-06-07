/*
 * Revisions:
 *  # NB - 10/28/2008 10:45:20 AM - Source File Created 
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 */
using System;
using System.Xml;

namespace fmDotNet.Requests
{
    public class Duplicate
    {
        private FMSAxml fms;
        private string theRequest;
        private string dupCommand;
        private string script;
        // constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DupRequest"/> class.  Use the createDupRequest method instead!
        /// </summary>
        /// <param name="f">The instance of the outer class.</param>
        /// <param name="recID">The rec ID.</param>
        /// <remarks>While this class is publicly available, use the "CreateDuplicateRequest" method to create a new request.</remarks>
        internal Duplicate(FMSAxml f, string recID)
        {
            fms = f;
            dupCommand = "&-dup";
            theRequest = "&-recid=" + recID;
        }

        // end constructors

        /// <summary>
        /// Executes the duplicate request.
        /// </summary>
        /// <returns>RecordID of new record</returns>
        public String Execute()
        {
            theRequest += script;
            theRequest += dupCommand;

            // setup the URL and the DATA as seperate strings
            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData = "-db=" + fms.CurrentDataBase + "&-lay=" + fms.CurrentLayout + theRequest;

            String errorCode = "";
            String theRecordID = "";

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

                        case "datasource":

                        case "resultset":

                            /* this is where the data is */
                            /* now add a row to the table for each found record */
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
                                    }
                                } /*  for each attrib */
                            } /*   for each rec  */
                            break;
                    } /* switch rootnode name */
                } // foreach root node
                /* return the result */
                return theRecordID;
            }  // try
            catch (Exception ex)
            {
                Tools.LogUtility.WriteEntry(ex, System.Diagnostics.EventLogEntryType.Error);
                throw;
            } // catch
        }

        /// <summary>
        /// Specifies what script to run after the -dup command.
        /// </summary>
        /// <param name="theScript">The script.</param>
        public void AddScript(string theScript)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript);
        }
        /// <summary>
        /// Specifies what script to run after the -dup command, and what parameter to pass to the script.
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