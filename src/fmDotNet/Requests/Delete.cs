/*
 * Revisions:
 *  # NB - 10/28/2008 10:45:20 AM - Source File Created 
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 */
using System;
using System.Xml;


namespace fmDotNet.Requests
{
    public class Delete
    {
        private FMSAxml fms;
        private string theRequest;
        private string delCommand;
        private string script;
        // constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DeleteRequest"/> class.
        /// </summary>
        /// <param name="f">The outer class instance</param>
        /// <param name="recID">The rec ID.</param>
        /// <remarks>While this class is publicly available, use the "CreateDeleteRequest" method to create a new request.</remarks>
        internal Delete(FMSAxml f, String recID)
        {
            fms = f;
            delCommand = "&-delete";
            theRequest = "&-recid=" + recID;
        }

        // end constructors

        /// <summary>
        /// Executes the Delete request.
        /// </summary>
        /// <returns>an error code string</returns>
        public String Execute()
        {
            theRequest += script;
            theRequest += delCommand;

            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData ="-db=" + fms.CurrentDatabase + "&-lay=" + fms.CurrentLayout + theRequest;
            String errorCode = "0";

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

                    } /* switch rootnode name */
                } // foreach root node

                // return the result
                return errorCode;
            }  // try
            catch
            {
                throw;
            } // catch
        }

        /// <summary>
        /// Specify what script to run after the -delete command.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScript(string theScript)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript);
        }
        /// <summary>
        /// Specify what script to run after the -delete command and what parameter to pass to the script.
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