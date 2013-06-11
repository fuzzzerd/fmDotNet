/*
 * Revisions:
 *  # NB - 10/28/2008 10:45:20 AM - Source File Created 
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 */
using System;
using System.Xml;


namespace fmDotNet.Requests
{
    /// <summary>
    /// This allows us to make as many "new record" requests as necessary.
    /// </summary>
    public class NewRecord
    {
        private FMSAxml fms;
        private string theRequest;
        private string newCommand;
        private int fieldCounter = 0;
        private string fieldString;
        // private string recordRequest;
        private string script;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewRequest"/> class.
        /// </summary>
        /// <param name="f">The instance of the outer class.</param>
        /// <remarks>While this class is publicly available, use the "CreateNewRecordRequest" method to create a new request.</remarks>
        internal NewRecord(FMSAxml f)
        {
            fms = f;
            newCommand = "&-new";
        }

        /// <summary>
        /// Executes the "new" command.  Throws an error if the command fails.
        /// </summary>
        public string Execute()
        {
            // if (fms.fmResponseLayout.Length > 0)
            //    theRequest += "&-lay.response=" + fms.fmResponseLayout;

            theRequest += script;
            theRequest += fieldString;

            theRequest += newCommand;

            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData= "-db=" + fms.CurrentDataBase + "&-lay=" + fms.CurrentLayout + theRequest;
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
        /// Adds a field name/value pair to the request.  This populates the field with data.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        public void AddField(string fieldName, string fieldValue)
        {
            fieldCounter++;
            fieldString += "&" + Uri.EscapeUriString(fieldName) + "=" + Uri.EscapeUriString(fieldValue);
        }

        /// <summary>
        /// Specify what script to run after the new command.
        /// Does not use a parameter.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScript(string theScript)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript);
        }
        /// <summary>
        /// Specify what script to run after the new command.
        /// And passes a parameter to the script.
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