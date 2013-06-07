/*
 * Revisions:
 *  # NB - 10/28/2008 10:49:31 AM - Source File Created
 *  # NB - 10/30/2008 - Changed Execute() to return empty Dataset if FMS returns "error" 401
 *                          since 'no records found' is not worthy of an exception.
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 */
using fmDotNet.Enumerations;
using System;
using System.Data;
using System.Linq;
using System.Xml;

namespace fmDotNet.Requests
{
    /// <summary>
    /// Class representing a FindRequest
    /// </summary>
    /// <remarks>While this class is publicly available, use the "CreateFindRequest" method to create a new request.</remarks>
    public class Find
    {
        private FMSAxml fms;

        private string theRequest;
        private string fieldString;
        private int sortCounter = 0;
        private int fieldCounter = 0;
        private string logical = "&-lop=and"; // this is the default
        private string script;
        private string findCommand;

        /// <summary>
        /// Add by Koen Van Hulle - SHpartners (http://www.shpartners.com)
        /// 06 Jun 06 
        /// Represents the timestamp format, returned by the XML.
        /// </summary>
        private string TimeStampFormat;

        /// <summary>
        /// Add by Koen Van Hulle - SHpartners (http://www.shpartners.com)
        /// 06 Jun 06 
        /// Represents the timestamp format, returned by the XML.
        /// </summary>
        private string DateFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRequest"/> class.
        /// </summary>
        /// <param name="s">The SearchType enumerator</param>
        /// <param name="f">The instance of the outer FMSAxml class</param>
        /// <remarks>While this class is publicly available, use the "CreateFindRequest" method to create a new request.</remarks>
        internal Find(SearchType s, FMSAxml f)
        {
            fms = f;
            findCommand = RealSearchType(s);
        }

        /// <summary>
        /// Executes the find and returns a DataSet.
        /// </summary>
        /// <returns>DataSet, with all data from the chosen layout and subtables for portals.  Related records use the parent's record ID as their foreign key.</returns>
        public DataSet Execute()
        {
            if (fieldCounter <= 1)
                logical = "";

            theRequest += script;
            theRequest += logical;
            theRequest += fieldString;
            theRequest += findCommand;

            DataSet ds = new DataSet();

            // need an overload for &-max, probably one for fmLayout too
            /* if there is related data on the layout in portals then
             * we're returning multiple tables in the dataset
             * 
             * example:
             * http://testserver/fmi/xml/fmresultset.xml?-db=wim_MLB&-lay=list_franchises&-findall
             */

            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData = "-db=" + fms.CurrentDataBase + "&-lay=" + fms.CurrentLayout + theRequest;
            Int32 foundFields;
            Int32 foundRecords;
            String errorCode = "";
            
            FMField[] fmf = null;

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    fms.FMAccount,
                    fms.FMPassword,
                    fms.ResponseTimeout,
                    fms.DTDValidation);

                /* create a main table for the data on the layout */
                DataTable table = ds.Tables.Add("main");

                /* loop through the XML document returned by FMSA */
                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name.ToLower())
                    {
                        case "error":
                            errorCode = rootNode.Attributes.GetNamedItem("code").Value;
                            if (errorCode != "0")
                                if (errorCode == "401") // if there are no records, don't toss exception for that
                                    return ds;
                                else
                                    FMSAxml.HandleFMSerrors(errorCode); 
                            break;

                        case "datasource":
                            foundRecords = Convert.ToInt32(rootNode.Attributes.GetNamedItem("total-count").Value);
                            /*Add Koen Van Hulle - SHpartners (06 jun 2006)*/
                            TimeStampFormat = rootNode.Attributes.GetNamedItem("timestamp-format").Value;
                            DateFormat = rootNode.Attributes.GetNamedItem("date-format").Value;
                            break;

                        case "metadata":
                            /* grab the number of fields  */
                            foundFields = FMSAxml.CountFields(rootNode);
                            fmf = new FMField[foundFields];
                            int x = 0;

                            /* add columns for the record id & mod ID */
                            table.Columns.Add("recordID", Type.GetType("System.String"));
                            table.Columns.Add("modID", Type.GetType("System.String"));

                            // add a column for each field found
                            // regular and related fields on the layout!!
                            foreach (XmlNode field in rootNode.ChildNodes)
                            {
                                switch (field.Name)
                                {
                                    case "field-definition":
                                        /* normal field */
                                        fmf[x] = fms.PopulateFieldInfo(field);
                                        Type fieldType = FMSAxml.GetSystemType(field);
                                        // add a column for it
                                        // except if it already contains a column with that name
                                        if (!table.Columns.Contains(fmf[x].name))
                                        {
                                            table.Columns.Add(fmf[x].name, fieldType);
                                        }
                                        else
                                        {
                                            // if the column does already exist, add a dup otherwise
                                            // we risk getting in trouble with assigning data to columns
                                            table.Columns.Add(fmf[x].name + "_dup", fieldType);
                                        }

                                        x++;
                                        break;

                                    case "relatedset-definition":
                                        // fields in portals
                                        // has one attribute: the name of the relationship used in the portal (name of the TO, really)
                                        string thePortal = field.Attributes.GetNamedItem("table").Value;

                                        /* now add a table */
                                        DataTable subTable = ds.Tables.Add(thePortal);

                                        /* add columns for the foreign key, mod id, record id */
                                        subTable.Columns.Add("parentRecordID", Type.GetType("System.String"));
                                        subTable.Columns.Add("recordID", Type.GetType("System.String"));
                                        subTable.Columns.Add("modID", Type.GetType("System.String"));
                                        /* add a relationship to the main table */
                                        DataRelation rel;
                                        DataColumn left = ds.Tables["main"].Columns["recordID"];
                                        DataColumn right = ds.Tables[thePortal].Columns["parentRecordID"];
                                        rel = new DataRelation(thePortal, left, right);
                                        ds.Relations.Add(rel);

                                        /* now add columns to the related table */
                                        foreach (XmlNode portalField in field.ChildNodes)
                                        {
                                            fmf[x] = fms.PopulateFieldInfo(portalField);
                                            fmf[x].portal = thePortal;
                                            fieldType = FMSAxml.GetSystemType(portalField);
                                            subTable.Columns.Add(fmf[x].name, fieldType);

                                            x++;
                                        }
                                        break;
                                }
                                // string fieldName = "";
                            } // foreach field
                            break;

                        case "resultset":
                            /* this is where the data is */
                            /* now add a row to the table for each found record */
                            foreach (XmlNode rec in rootNode.ChildNodes)
                            {
                                DataRow newRow = null;

                                // get FileMaker internal RecordID
                                String theRecordID = (from XmlAttribute a in rec.Attributes
                                        where a.Name.ToLower() == "record-id"
                                        select a.Value).Single();
                                // set primary key to the internal FileMaker RecordID
                                String primaryKey = theRecordID;

                                // get FileMaker internal ModificationID
                                String theModID = (from XmlAttribute a in rec.Attributes
                                        where a.Name.ToLower() == "mod-id"
                                        select a.Value).Single();

                                /* see if this is a record for the main table
                                 * or for a related table
                                 * can have "field" or "relatedset" children
                                 */
                                if (rec.HasChildNodes)
                                {
                                    /* add a new record to the main table */
                                    newRow = table.NewRow();
                                    newRow["recordID"] = theRecordID;
                                    newRow["modID"] = theModID;

                                    /* need to do 2 foreaches, one for Field nodes
                                     * and one for resultset nodes (related records in
                                     * a portal
                                     */
                                    foreach (XmlNode recChild in rec.ChildNodes)
                                    {
                                        switch (recChild.Name)
                                        {
                                            case "field":
                                                /* now get the rest of the fields data into
                                                 * the row
                                                 * Changed by Koen Van Hulle - SHpartners
                                                 */
                                                newRow = FMSAxml.PopulateRow(newRow, 
                                                    recChild, 
                                                    fmf, 
                                                    TimeStampFormat, 
                                                    DateFormat);
                                                break;
                                        }
                                    }
                                    // time to add the row
                                    table.Rows.Add(newRow);

                                    /* 2nd foreach to look for the portal records */
                                    foreach (XmlNode recChild in rec.ChildNodes)
                                    {
                                        switch (recChild.Name)
                                        {
                                            case "relatedset":
                                                string relTable = recChild.Attributes.GetNamedItem("table").Value;
                                                /* has multiple records */
                                                foreach (XmlNode portalRec in recChild.ChildNodes)
                                                {
                                                    /* recordid, mod id is in the attributes */
                                                    foreach (XmlAttribute attrib in portalRec.Attributes)
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
                                                    /* ad a new row to the related table */
                                                    newRow = ds.Tables[relTable].NewRow();
                                                    newRow["recordID"] = theRecordID;
                                                    newRow["modID"] = theModID;
                                                    newRow["parentRecordID"] = primaryKey;

                                                    /* now go get the rest of the fields */
                                                    foreach (XmlNode c in portalRec.ChildNodes)
                                                    {
                                                        switch (c.Name)
                                                        {
                                                            case "field":
                                                                /* now get the rest of the fields data into
                                                                 * the row
                                                                 * Changed by Koen Van Hulle - SHpartners
                                                                 */
                                                                newRow = FMSAxml.PopulateRow(newRow, 
                                                                    c, 
                                                                    fmf, 
                                                                    TimeStampFormat, 
                                                                    DateFormat);
                                                                break;
                                                        }
                                                    }
                                                    /* add the row to the table  */
                                                    ds.Tables[relTable].Rows.Add(newRow);
                                                }
                                                break;
                                        } // switch name
                                    } // 2nd foreach
                                } // if rec has child nodes
                            } /*   for each rec  */
                            break;
                    } /* switch rootnode name */
                } // foreach root node
                /* return the result
                 * and also reset the fields collection with the fields we've found here
                 */
                fms.Fields = fmf;
                return ds;
            }  // try
            catch(Exception ex)
            {
                Tools.LogUtility.WriteEntry(ex,
                    System.Diagnostics.EventLogEntryType.Error);
                throw;
            } // catch
            // return ds;
        } // executeFind

        /// <summary>
        /// Adds the search field to the find request, adds it to the URL.
        /// </summary>
        /// <param name="theField">The field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="op">The operator enumerator (equal, greather than...)</param>
        public void AddSearchField(String theField, String fieldValue, SearchOption op)
        {
            // 1st overload, with operator
            if (findCommand == "&-findany")
                return;

            fieldCounter++;
            fieldString += "&" + Uri.EscapeUriString(theField) + "=" + Uri.EscapeUriString(fieldValue) + "&" + Uri.EscapeUriString(theField) + ".op=" + RealSearchCriterium(op);
        }

        /// <summary>
        /// Adds the search field to the find request, adds it to the URL.  Assumes the search operator to be "starts with".
        /// </summary>
        /// <param name="theField">The field.</param>
        /// <param name="fieldValue">The field value.</param>
        public void AddSearchField(string theField, string fieldValue)
        {
            // 2nd overload, no operator
            if (findCommand == "&-findany")
                return;

            fieldCounter++;
            fieldString += "&" + Uri.EscapeUriString(theField) + "=" + Uri.EscapeUriString(fieldValue);
        }

        /// <summary>
        /// Adds a sort field to the request.  Sorted ascending.
        /// </summary>
        /// <param name="sortField">The sort field.</param>
        public void AddSortField(string sortField)
        {
            // 1st overload: no sorting
            if (findCommand == "&-findany")
                return;

            sortCounter++;
            theRequest += "&-sortfield." + sortCounter + "=" + Uri.EscapeUriString(sortField);
        }

        /// <summary>
        /// Adds the sort field to the request.
        /// </summary>
        /// <param name="sortField">The sort field.</param>
        /// <param name="AorD">Ascending or Descending sort enumerator (FMSAxml.sort).</param>
        public void AddSortField(String sortField, Sort AorD)
        {
            // 2nd overload, sort either ascending or descending
            if (findCommand == "&-findany")
                return;

            sortCounter++;
            theRequest += "&-sortfield." + sortCounter + "=" + Uri.EscapeUriString(sortField) + "&-sortorder." + sortCounter + "=" + AorD.ToString();
        }

        /// <summary>
        /// Adds the sort field to the request.  Sorted by the specified value list.
        /// </summary>
        /// <param name="sortField">The sort field.</param>
        /// <param name="VLname">The value list name.</param>
        public void AddSortField(string sortField, string VLname)
        {
            // 3rd overload, sort by value list
            if (findCommand == "&-findany")
                return;

            sortCounter++;
            theRequest += "&-sortfield." + sortCounter + "=" + Uri.EscapeUriString(sortField) + "&-sortorder." + sortCounter + "=" + Uri.EscapeUriString(VLname);
        }

        /// <summary>
        /// Sets the record ID to use in the request.  Don't use "AddSearchField" when using this method.
        /// </summary>
        /// <param name="recID">The rec ID.</param>
        public void SetRecordID(string recID)
        {
            if (findCommand == "&-findany")
                return;

            // destroys everything else and executes the find
            theRequest = "&-recid=" + recID;
        }

        /// <summary>
        /// Makes the find request an "or" request.  "and" is the default.
        /// </summary>
        public void SetOr()
        {
            if (findCommand == "&-findany")
                return;

            // and is default
            // this needs to go before the field stuff, right after the layout
            logical = "&-lop=or";
        }

        /// <summary>
        /// Specifies what script to run after sorting and finding.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScript(string theScript)
        {
            script += "&-script=" + Uri.EscapeUriString(theScript);
        }

        /// <summary>
        /// Specifies what script to run after sorting and finding, and what parameter to pass to the script.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        /// <param name="parameter">The parameter value.</param>
        public void AddScript(string theScript, string parameter)
        {
            // need to check that the script is a valid script name?
            // or just rely on the error thrown?
            script += "&-script=" + Uri.EscapeUriString(theScript) + "&-script.param=" + Uri.EscapeUriString(parameter);
        }

        /// <summary>
        /// Specifies the FileMaker script to run after finding records (if specified) and before sorting records.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScriptPresort(string theScript)
        {
            if (findCommand == "&-findany")
                return;

            if (findCommand == "&-findany")
                return;

            script += "&-script.presort=" + Uri.EscapeUriString(theScript);
        }

        /// <summary>
        /// Specifies the FileMaker script to run after finding records (if specified) and before sorting records, and what parameter to pass to the script.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        /// <param name="parameter">The parameter value.</param>
        public void AddScriptPresort(string theScript, string parameter)
        {
            if (findCommand == "&-findany")
                return;

            script += "&-script.presort=" + Uri.EscapeUriString(theScript) + "&-script.presort.param=" + Uri.EscapeUriString(parameter);
        }

        /// <summary>
        /// Specifies the FileMaker script to run before finding records (if specified) and sorting records.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        public void AddScriptPrefind(string theScript)
        {
            script += "&-script.prefind=" + Uri.EscapeUriString(theScript);
        }

        /// <summary>
        /// Specifies the FileMaker script to run before finding records (if specified) and sorting records, and what parameter to pass to the script.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        /// <param name="parameter">The parameter value.</param>
        public void AddScriptPrefind(string theScript, string parameter)
        {
            //specifies the FileMaker script to run before finding records (if specified) and sorting records during the processing of the -find query command.
            script += "&-script.prefind=" + Uri.EscapeUriString(theScript) + "&-script.prefind.param=" + Uri.EscapeUriString(parameter);
        }

        /// <summary>
        /// This parameter specifies the number of records to skip in the beginning of the found set. The default value is 0.
        /// </summary>
        /// <remarks>If the skip value is greater than the number of records found, then no record is returned.</remarks>
        /// <param name="howMany">How many records to skip.</param>
        public void SetSkip(int howMany)
        {
            theRequest += "&-skip=" + howMany.ToString();
        }

        /// <summary>
        /// Specifies the maximum number of records returned. By default, FileMaker returns all records.
        /// </summary>
        /// <param name="max">The max.</param>
        public void SetMax(int max)
        {
            if (findCommand == "&-findany")
                return;

            theRequest = "&-max=" + max.ToString();
        }

        /// <summary>
        /// Translates the searchOption enumerator into the search operator string FMSA expects.
        /// </summary>
        /// <param name="o">SearchOption enumerator value</param>
        /// <returns>string</returns>
        public static String RealSearchCriterium(SearchOption o)
        {
            string temp = "";

            switch (o)
            {
                case SearchOption.beginsWith:
                    temp = "bw";
                    break;
                case SearchOption.biggerOrEqualThan:
                    temp = "gte";
                    break;
                case SearchOption.biggerThan:
                    temp = "gt";
                    break;
                case SearchOption.contains:
                    temp = "cn";
                    break;
                case SearchOption.endsWith:
                    temp = "ew";
                    break;
                case SearchOption.equals:
                    temp = "eq";
                    break;
                case SearchOption.omit:
                    temp = "neq";
                    break;
                case SearchOption.lessOrEqualThan:
                    temp = "lte";
                    break;
                case SearchOption.lessThan:
                    temp = "lt";
                    break;
            }

            return temp;
        }

        /// <summary>
        /// Utility function to return the required FMSA command string
        /// from our enum
        /// </summary>
        /// <param name="s">SearchType enumerator value</param>
        /// <returns>string</returns>
        public static String RealSearchType(SearchType s)
        {
            string temp = "";
            switch (s)
            {
                case SearchType.AllRecords:
                    temp = "&-findall";
                    break;
                case SearchType.RandomRecord:
                    temp = "&-findany";
                    break;
                case SearchType.Subset:
                    temp = "&-find";
                    break;
                default:
                    temp = "&-find";
                    break;
            }
            return temp;
        }
    };
}