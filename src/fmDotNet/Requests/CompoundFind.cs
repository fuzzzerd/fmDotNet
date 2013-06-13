/*
 * Revisions:
 *  # NB - 10/28/2008 10:49:31 AM - Source File Created                      
 *  # NB - 2013-06-07 - Changed constructor to `internal` to force correct usage.
 *  # NB - 2031-06-13 - Made parameters optional for AddFindCriterion, default to false, same as FM API.
 */
using fmDotNet.Enumerations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;

namespace fmDotNet.Requests
{
    /// <summary>
    /// Class representing a CompoundFindRequest.
    /// </summary>
    /// <remarks>While this class is publicly available, use the "CreateCompoundFindRequest" method to create a new request.</remarks>
    public class CompoundFind
    {
        /// <summary>
        /// Class used to store each new find operation. Each is 
        /// roughly the same as a new FindRequest in FileMaker Pro.
        /// </summary>
        internal class FindCriterion
        {
            public string FieldName { get; set; }
            public string FieldValue { get; set; }
            public Boolean IsOr { get; set; }
            public Boolean IsOmit { get; set; }
        }

        private FMSAxml fms;

        private string theRequest;
        private int sortCounter = 0;
        private string script;
        private const string findCommand = "-findquery";

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

        internal List<FindCriterion> FindCriteria = new List<FindCriterion>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundFindRequest"/> class.
        /// </summary>
        /// <param name="f">The instance of the outer FMSAxml class</param>
        internal CompoundFind(FMSAxml f)
        {
            fms = f;
        }

        /// <summary>
        /// Executes the find and returns a DataSet.
        /// </summary>
        /// <returns>DataSet, with all data from the chosen layout and subtables for portals.  Related records use the parent's record ID as their foreign key.</returns>
        public DataSet Execute()
        {
            theRequest += script;
            theRequest += BuildFindString();
            theRequest += theRequest.EndsWith("&") ? findCommand : "&" + findCommand;

            DataSet ds = new DataSet();

            // need an overload for &-max, probably one for fmLayout too
            /* if there is related data on the layout in portals then
             * we're returning multiple tables in the dataset
             * 
             * example:
             * http://testserver/fmi/xml/fmresultset.xml?-db=wim_MLB&-lay=list_franchises&-findall
             */

            String URLstring = fms.Protocol + "://" + fms.ServerAddress + ":" + fms.Port + "/fmi/xml/fmresultset.xml";
            String theData = "-db=" + fms.CurrentDatabase + "&-lay=" + fms.CurrentLayout + theRequest;
            Int32 foundFields;
            Int32 foundRecords;
            String errorCode = "";
            
            FMField[] fmf = null;

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring, theData, fms.FMAccount, fms.FMPassword, fms.ResponseTimeout, fms.DTDValidation);

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
                                        if (!table.Columns.Contains(fmf[x].Name))
                                        {
                                            table.Columns.Add(fmf[x].Name, fieldType);
                                        }
                                        else
                                        {
                                            // if the column does already exist, add a dup otherwise
                                            // we risk getting in trouble with assigning data to columns
                                            table.Columns.Add(fmf[x].Name + "_dup", fieldType);
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
                                            fmf[x].Portal = thePortal;
                                            fieldType = FMSAxml.GetSystemType(portalField);
                                            subTable.Columns.Add(fmf[x].Name, fieldType);

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

                                string primaryKey = "";

                                string theRecordID = "";
                                string theModID = "";

                                /* recordid, mod id is in the attributes */
                                foreach (XmlAttribute attrib in rec.Attributes)
                                {
                                    switch (attrib.Name)
                                    {
                                        case "record-id":
                                            theRecordID = attrib.Value;
                                            primaryKey = theRecordID;

                                            break;
                                        case "mod-id":
                                            theModID = attrib.Value;
                                            break;
                                    }

                                } /*  for each attrib */

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
                                                newRow = FMSAxml.PopulateRow(newRow, recChild, fmf, TimeStampFormat, DateFormat);
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
                                                                newRow = FMSAxml.PopulateRow(newRow, c, fmf, TimeStampFormat, DateFormat);
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
                Tools.LogUtility.WriteEntry(ex, System.Diagnostics.EventLogEntryType.Error);
                throw;
            } // catch
            // return ds;
        }

        private string BuildFindString()
        {
            var sb = new StringBuilder();
            sb.Append("&-query=");

            var ands = FindCriteria.Where(q => q.IsOr == false && q.IsOmit == false).Select(q => "q" + ((int)FindCriteria.IndexOf(q) + 1));
            var ors = FindCriteria.Where(q => q.IsOr).Select(q => "(q" + ((int)FindCriteria.IndexOf(q) + 1) + ");");
            var omits = FindCriteria.Where(q => q.IsOmit).Select(q => "!(q" + ((int)FindCriteria.IndexOf(q) + 1) + ");");
            
            var andsQ = String.Format("({0});", string.Join(", ", ands.ToArray()));
            var orsQ = string.Join("", ors.ToArray());
            var omitsQ = string.Join("", omits.ToArray());

            if (!String.IsNullOrEmpty(andsQ) && andsQ != "();")
            {
                sb.Append(andsQ);
            }
            if (!String.IsNullOrEmpty(orsQ))
            {
                sb.Append(orsQ);
            }
            if (!String.IsNullOrEmpty(omitsQ))
            {
                sb.Append(omitsQ);
            }

            for (int i = 0; i < FindCriteria.Count; i++)
            {
                var lineOut = string.Format("&-q{0}={1}&-q{0}.value={2}",
                    i + 1,
                    Uri.EscapeUriString(FindCriteria[i].FieldName),
                    Uri.EscapeUriString(FindCriteria[i].FieldValue));

                sb.Append(lineOut);
            }

            return sb.ToString();

        } // executeFind


        internal void AddSearchCriterion(FindCriterion criterion)
        {
            FindCriteria.Add(criterion);
        }

        /// <summary>
        /// Adds a search criterion to this compound find. Each item will be anded with the previous unless
        /// isOr and/or isOmit are set to true.
        /// </summary>
        /// <param name="fieldName">Field name to search.</param>
        /// <param name="fieldValue">The value to search for</param>
        /// <param name="isOr">Is this find an OR find? Default is AND. Default is false.</param>
        /// <param name="isOmit">Omit records that match this criteria. Default is false</param>
        public void AddSearchCriterion(string fieldName, string fieldValue, Boolean isOr = false, Boolean isOmit = false)
        {
            var fc = new FindCriterion()
            {
                FieldName = fieldName,
                FieldValue = fieldValue,
                IsOr = isOr,
                IsOmit = isOmit
            };

            this.AddSearchCriterion(fc);
        }

        /// <summary>
        /// Adds a sort field to the request.  Sorted ascending.
        /// </summary>
        /// <param name="sortField">The sort field.</param>
        public void AddSortField(string sortField)
        {
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
            sortCounter++;
            theRequest += "&-sortfield." + sortCounter + "=" + Uri.EscapeUriString(sortField) + "&-sortorder." + sortCounter + "=" + Uri.EscapeUriString(VLname);
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
            script += "&-script.presort=" + Uri.EscapeUriString(theScript);
        }

        /// <summary>
        /// Specifies the FileMaker script to run after finding records (if specified) and before sorting records, and what parameter to pass to the script.
        /// </summary>
        /// <param name="theScript">The script name.</param>
        /// <param name="parameter">The parameter value.</param>
        public void AddScriptPresort(string theScript, string parameter)
        {
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
            theRequest = "&-max=" + max.ToString();
        }
    }
}