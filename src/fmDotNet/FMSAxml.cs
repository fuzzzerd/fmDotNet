/*
 * Revisions:
 *  # NB - 10/28/2008 10:25:52 AM - Source File Created, re-organized entire class library.
 *  # NB - 10/30/2008 - Fixed several issues:
 *                      1. Modified RootOfDoc to do HTTP GET/POST depending on the 
 *                         length of data being submitted.
 *                      2. Updates to Requests.Find 
 *  # NB - 02/18/2009 - Fixed issue in RootOfDoc that caused GET to be used, in some 
 *                          cases where POST should have been used. 
 *  # NB - 09/18/2009 - Fixed an issue in PopulateRow with parsing numbers.
 *  # NB - 2013-06-04 - Switched to POST for all web requests.
 *  # NB - 2013-06-07 - Cleaned up constructor code, to follow DRY.
 *  # NB - 2013-06-11 - Refactored RootOfDoc DTD validation, both FMS12 and FMS13 appear 
 *                      to fail DTD validation, FMS11 works as expected.
 *  # NB - 2013-06-13 - Cleaned up MakeUrl (removed ?, and added where as argument when needed).
 *  # NB - 2018-03-23 - Clean up for NET Standard 2.0
 */
using fmDotNet.Enumerations;
using fmDotNet.Requests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;


namespace fmDotNet
{
    public class FMSAxml
    {
        #region "Private, protected, and internal variables"

        // Set to false for "quick" file/layout changing
        private Boolean checkFileAndLayout = true;

        private FMField[] fields;
        /// <summary>
        /// Read-only array of FM fields.
        /// </summary>
        /// <value>The fields.</value>
        internal FMField[] Fields
        {
            get
            {
                return fields;
            }
            set
            {
                fields = value;
            }
        }
        #endregion

        #region "Public Properties"

        // Fields, private set, public get
        public Scheme Protocol { get; private set; }
        public Grammar XmlGrammer { get; private set; }

        public String ServerAddress { get; private set; }       // IP or DNS name
        public Int32 Port { get; private set; }                 // 80 for HTTP or 443 for HTTPS
        public Int32 ResponseTimeout { get; private set; }      // used in HttpWebRequest to set the timeout
        public Boolean DTDValidation { get; private set; }      // false by default because it breaks in FMSA9

        private List<String> availableDBs;

        /// <summary>
        /// Read-only List of available files.
        /// </summary>
        /// <value></value>
        public ReadOnlyCollection<String> AvailableDatabases
        {
            get
            {
                return availableDBs.AsReadOnly();
            }
        }
        private List<String> availableLayouts;
        /// <summary>
        /// Read-only List of available layouts of a given file.
        /// </summary>
        /// <value>The available layouts.</value>
        public ReadOnlyCollection<String> AvailableLayouts
        {
            get
            {
                return availableLayouts.AsReadOnly();
            }
        }

        private List<String> availableScripts;
        /// <summary>
        /// Read-only List of scripts in the chosen file.
        /// </summary>
        /// <value>The available scripts.</value>
        public ReadOnlyCollection<String> AvailableScripts
        {
            get
            {
                return availableScripts.AsReadOnly();
            }
        }
        /// <summary>
        /// Represents the currently selected FM file, set by the "SetDatabase" method.
        /// </summary>
        public String CurrentDatabase { get; set; }
        /// <summary>
        /// Represents the currently selected layout, set by SetLayout method.
        /// </summary>
        public String CurrentLayout { get; set; }
        public String ResponseLayout { get; set; }
        public String BaseUrl { get; set; }
        public String FMAccount { get; set; }
        public String FMPassword { get; set; }

        /// <summary>
        /// Read-only property: total records in found set.
        /// </summary>
        /// <value>The record count.</value>
        public int TotalRecords
        {
            get;
            private set;
        }

        // public read-only properties
        /// <summary>
        /// Read-only property: build info of the Web Publishing Engine (WPE).
        /// </summary>
        /// <value>The WPE build.</value>
        public string WPEBuild
        {
            get;
            private set;
        }
        /// <summary>
        /// Read-only property: version info of the Web Publishing Engine (WPE).
        /// </summary>
        /// <value>The WPE version.</value>
        public string WPEVersion
        {
            get;
            private set;
        }
        /// <summary>
        /// Read-only property: name of the Web Publishing Engine (WPE).
        /// </summary>
        /// <value>The WPE name.</value>
        public string WPEName
        {
            get;
            private set;
        }
        #endregion

        #region "Constructor Methods"
        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Default constructor, requires all the info.
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theGrammar">The XML grammar requested from FMSA.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.</param>
        /// <param name="timeOut">Milliseconds to wait for FMSA's response. (Default is 100,000 or 100 seconds).</param>
        /// <param name="dtd">Use DTD validation (Default is False). DO NOT USE WITH FMSA 9!!!!</param>
        /// <remarks>In this version of fmDotNet we're only going to use fmresultset as the available grammar no matter what is specified in the class constructor parameters</remarks>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, Grammar theGrammar, string theAccount, string thePW, int timeOut, bool dtd)
        {
            Protocol = theScheme;
            ServerAddress = theServer;
            Port = thePort;
            ResponseTimeout = timeOut;
            DTDValidation = dtd;

            XmlGrammer = Grammar.fmresultset;

            FMAccount = theAccount;
            FMPassword = thePW;
            BaseUrl = MakeURL();
            availableDBs = GetFiles();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Requires all the info except HTTP request time-out and DTD validation
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theGrammar">The XML grammar requested from FMSA.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.</param>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, Grammar theGrammar, string theAccount, string thePW)
            : this(theScheme, theServer, thePort, theGrammar, theAccount, thePW, 100000, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Requires all the info except HTTP request time-out
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theGrammar">The XML grammar requested from FMSA.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.</param>
        /// <param name="dtd">Use DTD validation (Default is False). DO NOT USE WITH FMSA 9!!!!</param>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, Grammar theGrammar, string theAccount, string thePW, bool dtd)
            : this(theScheme, theServer, thePort, theGrammar, theAccount, thePW, 100000, dtd) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Mininal info.  Assumes HTTP on port 80 and fmresultset as the returned XML grammar. No DTD validation and a default 100 sec response time-out.
        /// </summary>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.</param>
        public FMSAxml(String theServer, String theAccount, String thePW)
            : this(Scheme.HTTP, theServer, 80, Grammar.fmresultset, theAccount, thePW, 100000, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Assumes fmresultset as the returned XML grammar. Using HTTP (unless port = 443), no DTD validation, and a default 100 sec response time-out.
        /// </summary>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.  Can be empty ("").</param>
        public FMSAxml(string theServer, int thePort, string theAccount, string thePW)
            : this(thePort != 443 ? Scheme.HTTP : Scheme.HTTPS, theServer, thePort, Grammar.fmresultset, theAccount, thePW, 100000, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Assumes fmresultset as the returned XML grammar. No DTD validation and a default 100 sec response time-out.
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.  Can be empty ("").</param>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, string theAccount, string thePW)
            : this(theScheme, theServer, thePort, Grammar.fmresultset, theAccount, thePW, 100000, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Assumes fmresultset as the returned XML grammar.  No DTD validation.
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.  Can be empty ("").</param>
        /// <param name="timeOut">Milliseconds to wait for FMSA's response. (Default is 100,000 or 100 seconds)</param>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, string theAccount, string thePW, int timeOut)
            : this(theScheme, theServer, thePort, Grammar.fmresultset, theAccount, thePW, timeOut, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSAxml"/> class.
        /// Assumes fmresultset as the returned XML grammar.  Default 100 second response time-out.
        /// </summary>
        /// <param name="theScheme">The scheme (HTTP/HTTPS).</param>
        /// <param name="theServer">The IP or DNS name of the web server.</param>
        /// <param name="thePort">The port.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.  Can be empty ("").</param>
        /// <param name="dtd">Use DTD validation (Default is False). DO NOT USE WITH FMSA 9!!!!</param>
        public FMSAxml(Scheme theScheme, string theServer, int thePort, string theAccount, string thePW, bool dtd)
            : this(theScheme, theServer, thePort, Grammar.fmresultset, theAccount, thePW, 100000, dtd) { }

        #endregion

        #region "Public Methods"

        /// <summary>
        /// Sets the chosen FileMaker file.  Throws an "MissingMemberException" if the file is not available.
        /// </summary>
        /// <param name="theDB">The FM file name.</param>
        /// <remarks>Populates the Lists of layouts and scripts if successful.</remarks>
        public void SetDatabase(string theDB)
        {
            SetDatabase(theDB, true);
        }

        /// <summary>
        /// Sets the chosen FileMaker file.  Throws an "MissingMemberException" if the file is not available.
        /// </summary>
        /// <param name="theDB">The FM file name.</param>
        /// <param name="skipScriptsAndLayouts">Specifies if we should skip populating the Lists of Scripts/Layouts. 
        /// Puts fmDotNet in "quick" switch mode if set to true, which skips layout validation.</param>
        public void SetDatabase(String theDB, Boolean skipScriptsAndLayouts)
        {
            // set the current "mode"
            checkFileAndLayout = !skipScriptsAndLayouts;

            // if the database is available
            if (CheckDatabase(theDB) == true)
            {
                // set the database
                CurrentDatabase = theDB;

                // if we were told to skip layout/scripts skip or process
                if (checkFileAndLayout == true)
                {
                    // populate the 
                    availableLayouts = GetLayouts();
                    availableScripts = GetScripts();
                }
                else
                {
                    // set lists so they aren't empty 
                    availableLayouts = new List<String>(0);
                    availableScripts = new List<String>(0);
                }
            }
            else
            {
                // raise error
                throw new System.MissingMemberException("The file: \"" + theDB + "\" is not available.");
            }
        }

        /// <summary>
        /// Sets the layout.  Throws an MissingMemberException if the layout is not in the chosen file.
        /// </summary>
        /// <param name="theLayout">The layout name.</param>
        public void SetLayout(String theLayout)
        {
            if (CheckLayout(theLayout) == true)
            {
                CurrentLayout = Uri.EscapeUriString(theLayout);
                ResponseLayout = ""; // make sure this one is reset
            }
            else if (checkFileAndLayout == false)
            {
                CurrentLayout = Uri.EscapeUriString(theLayout);
                ResponseLayout = ""; // make sure this one is reset
            }
            else
            {
                // raise error
                throw new System.MissingMemberException("The layout: \"" + theLayout + "\" is not available.");
            }
        }

        /// <summary>
        /// Sets the layout. Throws an MissingMemberException if the layout is not in the chosen file.
        /// </summary>
        /// <param name="returnFrom">The layout the found set is returned from.</param>
        /// <param name="useInSearch">The actual layout used in the search.</param>
        public void SetLayout(String returnFrom, String useInSearch)
        {
            if (CheckLayout(returnFrom) && CheckLayout(useInSearch))
            {
                ResponseLayout = Uri.EscapeUriString(useInSearch);
                CurrentLayout = Uri.EscapeUriString(returnFrom);
            }
            else if (checkFileAndLayout == false)
            {
                CurrentLayout = Uri.EscapeUriString(returnFrom);
                ResponseLayout = Uri.EscapeUriString(useInSearch);
            }
            else
            {
                if (CheckLayout(returnFrom) == false)
                    // raise error
                    throw new System.MissingMemberException("The layout: \"" + returnFrom + "\" is not available.");
                else
                    throw new System.MissingMemberException("The layout: \"" + useInSearch + "\" is not available.");
            }
        }

        /// <summary>
        /// Gets the List<String> of file names currently available to XML publishing on the chosen server.
        /// </summary>
        /// <remarks>http://testserver/fmi/xml/fmresultset.xml?-dbnames.</remarks>
        /// <returns>List of file names</returns>
        public List<String> GetFiles()
        {
            List<String> temp = new List<String>();

            String URLstring = BaseUrl;
            String theData = "-dbnames";
            String theError = "0";

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    this.FMAccount,
                    this.FMPassword,
                    this.ResponseTimeout,
                    this.DTDValidation);

                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name)
                    {
                        case "error":
                            // has one attrib: error code
                            foreach (XmlAttribute errorCode in rootNode.Attributes)
                            {
                                switch (errorCode.Name)
                                {
                                    case "code":
                                        theError = errorCode.Value;
                                        break;
                                }
                            }
                            break;

                        case "product":
                            // get the info
                            // loop through the info attributes
                            foreach (XmlAttribute attrib in rootNode.Attributes)
                            {
                                switch (attrib.Name)
                                {
                                    case "version":
                                        this.WPEVersion = attrib.Value;
                                        break;
                                    case "name":
                                        this.WPEName = attrib.Value;
                                        break;
                                    case "build":
                                        this.WPEBuild = attrib.Value;
                                        break;
                                }
                            } // foreach attrib
                            break;

                        case "resultset":
                            // available files
                            foreach (XmlNode file in rootNode.ChildNodes)
                            {
                                // file is the record node!!
                                string theFile = file.FirstChild.FirstChild.InnerText;
                                temp.Add(theFile);
                            }
                            break;
                    } // switch rootnode.name
                } // foreach
                if (theError != "0")
                {
                    throw new Exception("Could not retrieve the list of available files from FMSA.  Check to make sure FMSA is configured properly and that there are files hosted with accounts whose privilege set has the fmxml bit toggled.");
                    // error when retrieving the database names
                    //MessageBox.Show("Could not connect to the server to retrieve the available files.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                XmlNode FMSAerror = root.SelectSingleNode("error");
            }// try

            // need to exclude the custom error above!!
            // figure out what error the authentication really is
            catch (Exception tryError)
            {
                String tryErrorNumber = tryError.Message;
                int startErrorNbr = tryErrorNumber.IndexOf("(");
                tryErrorNumber = tryErrorNumber.Substring(startErrorNbr + 1, 4);
                tryErrorNumber = tryErrorNumber.Replace(")", "");
                switch (tryErrorNumber)
                {
                    case "401":
                        //throw new Exception("FileMaker Server could not authenticate the request.\nMost likely Database Visibility is on.  Please specify a valid account & password.");
                        // MessageBox.Show("FileMaker Server could not authenticate the request.\nMost likely Database Visibility is on.  Please specify a valid account & password.", "Error ...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    default:
                        // MessageBox.Show("Unhandled Error.\n" + tryError.Message, "Error...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
            }
            return temp;
        } // GetFiles

        /// <summary>
        /// Gets the picture as a stream and loads it from the stream as an Image
        /// </summary>
        /// <param name="pictureRef">The URL referencing the picture.</param>
        /// <returns>the picture as an Image element</returns>
        public Image GetPicture(String pictureRef)
        {
            Image temp = null;
            if (pictureRef.Length > 0)
            {
                String pictureURL = Protocol + "://" + ServerAddress + ":" + Port + pictureRef;
                WebClient wClient = new WebClient();
                NetworkCredential nc = new NetworkCredential(FMAccount, FMPassword);
                wClient.Credentials = nc;
                Stream response = wClient.OpenRead(pictureURL);
                temp = Image.FromStream(response);
                response.Close();
            }
            return temp;
        }

        /// <summary>
        /// Gets the URL reference to a picture.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="recordID">The record ID.</param>
        /// <returns>URL string</returns>
        /// <example>http://testserver/fmi/xml/cnt/data.jpg?-db=xml_repeat_test&-lay=show&-recid=1&-field=thePicture</example>
        /// <remarks>Can be used in any app with PictureBox like this: pictureBox.Image.Load(theRef)</remarks>
        public String GetPictureReference(String fieldName, String recordID)
        {
            string temp = "";

            // first check to see if the field is on the layout...
            // and it is a container
            foreach (FMField f in fields)
            {
                if (f.Name == fieldName)
                {
                    if (f.Type == "container")
                    {
                        temp = Protocol + "://" + ServerAddress + ":" + Port + "/fmi/xml/cnt/data.jpg?-db=" + CurrentDatabase + "&-lay=" + CurrentLayout + "&-recid=" + recordID + "&-field=" + fieldName;
                    }
                    else
                    {
                        throw new FormatException("Field: " + fieldName + " is not a container but a \"" + f.Type + "\"");
                    }
                    break;
                }
                else
                {
                    throw new FieldAccessException("Field: " + fieldName + " is not available on layout: " + CurrentLayout);
                }
            }
            return temp;
        }

        /// <summary>
        /// Gets the number of repetitions for a given field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>Integer # of repetitions</returns>
        public Int32 GetNumberOfRepetitions(String fieldName)
        {
            int temp = 1;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == fieldName)
                {
                    temp = fields[i].RepetitionCount;
                    break;
                }
            }
            return temp;
        }

        /// <summary>
        /// Gets the value list items.
        /// </summary>
        /// <param name="valuelistName">Name of the valuelist.</param>
        /// <returns>List of value list items</returns>
        public List<String> GetValueListData(String valuelistName)
        {
            var data = this.GetValueList(valuelistName);

            return data.Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Gets the value list items.
        /// </summary>
        /// <param name="valuelistName">Name of the valuelist.</param>
        /// <returns>A dictionary key/value pair of the value list</returns>
        public Dictionary<string, string> GetValueList(String valuelistName)
        {
            // http://testserver/fmi/xml/FMPXMLLAYOUT.xml?-db=xml_repeat_test&-lay=show&-view
            // and parse it out to get the data
            var returnData = new Dictionary<string, string>();
            String URLstring = Protocol + "://" + ServerAddress + ":" + Port + "/fmi/xml/FMPXMLLAYOUT.xml";
            String theData = "-db=" + CurrentDatabase + "&-lay=" + CurrentLayout + "&-view";
            String theError = "0";

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    this.FMAccount,
                    this.FMPassword,
                    this.ResponseTimeout,
                    this.DTDValidation);

                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name.ToLower())
                    {
                        case "error":
                            // has one attrib: error code
                            foreach (XmlAttribute errorCode in rootNode.Attributes)
                            {
                                switch (errorCode.Name)
                                {
                                    case "code":
                                        theError = errorCode.Value;
                                        break;
                                }
                            }
                            break;

                        case "valuelists":
                            foreach (XmlNode vl in rootNode.ChildNodes)
                            {
                                string vlName = vl.Attributes.GetNamedItem("NAME").Value;
                                if (vlName == valuelistName)
                                {
                                    foreach (XmlNode v in vl.ChildNodes)
                                    {
                                        if (!returnData.Keys.Contains(v.InnerText))
                                        {
                                            returnData.Add(v.InnerText, v.Attributes.GetNamedItem("DISPLAY").Value);
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                    }
                } // foreach
            } // try
            catch (Exception)
            {
                throw;
            }

            return returnData;
        }

        /// <summary>
        /// Gets the list of value list names on a given layout.
        /// </summary>
        /// <param name="theLayout">The layout.</param>
        /// <returns>List of value list names.</returns>
        public List<String> GetValueLists(String theLayout)
        {
            /* queries the layout, extracting the value lists
             * example: http://testserver/fmi/xml/FMPXMLLAYOUT.xml?-db=xml_repeat_test&-lay=show&-view
             * note we're using the layout grammar here
             */
            List<String> temp = new List<String>();
            String URLstring = Protocol + "://" + ServerAddress + ":" + Port + "/fmi/xml/FMPXMLLAYOUT.xml";
            String theData = "-db=" + CurrentDatabase + "&-lay=" + Uri.EscapeUriString(theLayout) + "&-view";
            String theError = "0";

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    this.FMAccount,
                    this.FMPassword,
                    this.ResponseTimeout,
                    this.DTDValidation);

                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name.ToLower())
                    {
                        case "errorcode":
                            // only has a value

                            theError = rootNode.InnerText;
                            break;

                        case "valuelists":
                            foreach (XmlNode vl in rootNode.ChildNodes)
                            {
                                temp.Add(vl.Attributes.GetNamedItem("NAME").Value);
                            }
                            break;
                    }
                } // foreach
            } // try
            catch
            {
                throw;
            }

            return temp;
        } // get value lists

        /// <summary>
        /// Gets the record count for a chosen layout.
        /// </summary>
        /// <remarks>expensive way to get the total record count since it requires a round trip to FMSA for just one bit of info getFields returns this too, in addition to an Array of the field defs</remarks>
        /// <param name="chosenLayout">The chosen layout.</param>
        /// <returns></returns>
        public Int32 GetRecordCount(String chosenLayout)
        {
            if (this.TotalRecords > 0 && CurrentLayout == chosenLayout)
            {
                return this.TotalRecords;
            }
            else
            {
                int temp = 0;
                String URLstring = Protocol + "://" + ServerAddress + ":" + Port + "/fmi/xml/fmresultset.xml";
                String theData = "-db=" + CurrentDatabase + "&-lay=" + Uri.EscapeUriString(chosenLayout) + "&-view";
                String errorCode = "0";

                try
                {
                    XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    this.FMAccount,
                    this.FMPassword,
                    this.ResponseTimeout,
                    this.DTDValidation);

                    foreach (XmlNode rootNode in root.ChildNodes)
                    {
                        switch (rootNode.Name.ToLower())
                        {
                            case "error":
                                errorCode = rootNode.Attributes.GetNamedItem("code").Value;
                                if (errorCode != "0")
                                    HandleFMSerrors(errorCode);
                                break;

                            case "datasource":
                                this.TotalRecords = Convert.ToInt32(rootNode.Attributes.GetNamedItem("total-count").Value);
                                temp = this.TotalRecords;
                                break;
                        }
                    }
                } // try
                catch
                {
                    throw new System.Xml.XmlException("Error in retrieving the total record count for layout: " + chosenLayout + ", of file: " + CurrentDatabase);
                } // catch
                finally
                {

                }
                return temp;
            }
        }

        /// <summary>
        /// Gets the array of FM fields (in the FMfield struct) for a given layout.  
        /// </summary>
        /// <remarks>note that we're using the resultset xml grammar here but with just -view so we get an empty resultset.  Cuts down on the amount of data transferred.
        /// </remarks>
        /// <param name="chosenLayout">The chosen layout.</param>
        /// <returns>FMfield array.  Also sets the recordCount property</returns>
        public List<FMField> GetFields(String chosenLayout)
        {
            /* need this syntax
             * http://testserver/fmi/xml/fmresultset.xml?-db=wim_MLB&-lay=teamshalf&-view
             */

            List<FMField> temp = null;
            String URLstring = Protocol + "://" + ServerAddress + ":" + Port + "/fmi/xml/fmresultset.xml";
            String theData = "-db=" + CurrentDatabase + "&-lay=" + Uri.EscapeUriString(chosenLayout) + "&-view";
            String errorCode = "";

            try
            {
                XmlNode root = FMSAxml.RootOfDoc(URLstring,
                    theData,
                    this.FMAccount,
                    this.FMPassword,
                    this.ResponseTimeout,
                    this.DTDValidation);

                foreach (XmlNode rootNode in root.ChildNodes)
                {
                    switch (rootNode.Name.ToLower())
                    {
                        case "error":
                            errorCode = rootNode.Attributes.GetNamedItem("code").Value;
                            if (errorCode != "0")
                                HandleFMSerrors(errorCode);
                            break;

                        case "datasource":
                            this.TotalRecords = Convert.ToInt32(rootNode.Attributes.GetNamedItem("total-count").Value);
                            break;

                        case "metadata":
                            int fieldCount = CountFields(rootNode);
                            temp = new List<FMField>(fieldCount);
                            int i = 0;
                            foreach (XmlNode field in rootNode.ChildNodes)
                            {
                                switch (field.Name)
                                {
                                    case "field-definition":
                                        // regular and related fields on the layout
                                        FMField f = PopulateFieldInfo(field);
                                        temp.Add(f);
                                        //temp[i] = PopulateFieldInfo(field);
                                        i++;
                                        break;

                                    case "relatedset-definition":
                                        // fields in portals
                                        // has one attribute: the name of the relationship used in the portal
                                        string thePortal = field.Attributes.GetNamedItem("table").Value;
                                        foreach (XmlNode portalField in field.ChildNodes)
                                        {
                                            FMField fr = PopulateFieldInfo(portalField);
                                            fr.Portal = thePortal;
                                            temp.Add(fr);
                                            //temp[i] = PopulateFieldInfo(portalField);
                                            //temp[i].portal = thePortal;
                                            i++;
                                        }
                                        break;
                                }

                            } // foreach field
                            break;
                    }
                } // foreach rootnode
            } // try
            catch
            {
                throw new System.Xml.XmlException("Error in retrieving field information for layout: " + chosenLayout + ", of file: " + CurrentDatabase);
            } // catch

            return temp;
        } // get fields

        /// <summary>
        /// Grabs all the field info from a FM XML field node.
        /// </summary>
        /// <param name="theNode">The XML node representing a FileMaker field.</param>
        /// <returns>FMField struct</returns>
        public FMField PopulateFieldInfo(XmlNode theNode)
        {
            FMField temp = new FMField();

            if (theNode.Attributes.GetNamedItem("global").Value == "yes")
            {
                temp.Global = "yes";
            }
            else
            {
                // don't want to leave it null...
                temp.Global = "";
            }
            temp.Name = theNode.Attributes.GetNamedItem("name").Value;
            temp.RepetitionCount = Convert.ToInt32(theNode.Attributes.GetNamedItem("max-repeat").Value);
            temp.Result = theNode.Attributes.GetNamedItem("result").Value;
            temp.Type = theNode.Attributes.GetNamedItem("type").Value;

            return temp;
        }

        #region "Create Request Methods"
        /// <summary>
        /// Creates a new record request by instantiating a NewRecordRequest class.
        /// Use this method instead of "new NewRecord" since it takes care of
        /// linking to the outer class instance.
        /// </summary>
        /// <returns>NewRecordRequest class instance</returns>
        public NewRecord CreateNewRecordRequest()
        {
            return new Requests.NewRecord(this);
        }

        /// <summary>
        /// Creates a new find request. Let's you specify a simple find or a findAll.
        /// </summary>
        /// <param name="findAll">set to true for a findAll search.</param>
        /// <returns>FindRequest class</returns>
        public Find CreateFindRequest(SearchType s)
        {
            return new Requests.Find(s, this);
        }

        /// <summary>
        /// Creates a new find request.  Simple find (no findAll).
        /// </summary>
        /// <returns>FindRequest class</returns>
        public Find CreateFindRequest()
        {
            return new Requests.Find(SearchType.Subset, this);
        }

        /// <summary>
        /// Creates a new compound find request.  For complex queries.
        /// </summary>
        /// <returns>FindRequest class</returns>
        public CompoundFind CreateCompoundFindRequest()
        {
            return new Requests.CompoundFind(this);
        }

        /// <summary>
        /// Creates a new Edit.  Use this method instead of declaring a new 
        /// instance of the EditRequest class directly.  This method takes care
        /// of referencing the current FMSAxml.
        /// </summary>
        /// <param name="recID">The rec ID.</param>
        /// <returns>An instance of the EditRequest class</returns>
        public Edit CreateEditRequest(String recID)
        {
            return new Requests.Edit(this, recID);
        }

        /// <summary>
        /// Creates a new Delete. Use this method insetead of declaring a new
        /// instance of the DeleteRequest class directly.  This methods takes care of
        /// referencing the current instance of FMSAxml.
        /// </summary>
        /// <param name="recID">The rec ID.</param>
        /// <returns>An instance of DeleteRequest</returns>
        public Delete CreateDeleteRequest(String recID)
        {
            return new Requests.Delete(this, recID);
        }

        /// <summary>
        /// Creates a new Duplicate request.  Use this method instead of declaring a new 
        /// instance of the DupRequest class directly.  This methods takes care of 
        /// referencing the current instance of FMSAxml.
        /// </summary>
        /// <param name="recID">The rec ID.</param>
        /// <returns>An instance of the DupRequest class</returns>
        public Duplicate CreateDuplicateRequest(String recID)
        {
            return new Requests.Duplicate(this, recID);
        }
        #endregion

        #endregion

        #region "Private Utility Methods"
        /// <summary>
        /// Checks the given file name against the generated list of hosted files.
        /// </summary>
        /// <param name="theDB">The file name.</param>
        /// <returns>true if the file is available.</returns>
        private Boolean CheckDatabase(String theDB)
        {
            // returns true if the db was found in the available files
            return availableDBs.Contains(theDB);
        }

        /// <summary>
        /// Checks if the layout is in the chosen file.
        /// </summary>
        /// <param name="theLayout">The layout name.</param>
        /// <returns>true if the layout is in the file.</returns>
        private Boolean CheckLayout(String theLayout)
        {
            // returns true if the layout was found in the chosen database
            // List availableLayouts = GetFiles();
            return availableLayouts.Contains(theLayout);
        }

        /// <summary>
        /// Makes the base URL to connect to FMS
        /// </summary>
        /// <remarks> string is case sensitive for xml grammar!! </remarks>
        /// <returns>URL string</returns>
        private String MakeURL()
        {
            // add error checking here to make sure all pieces are correct
            // raise error if not
            var returnValue = String.Format("{0}://{1}:{2}/fmi/xml/{3}.xml",
                Protocol,
                ServerAddress,
                Port,
                XmlGrammer);

            return returnValue;

        } // MakeURL

        /// <summary>
        /// Gets the list of layout names for the chosen file. Requires that a file has been chosen with SetDatabase().
        /// </summary>
        /// <remarks>http://testserver/fmi/xml/fmresultset.xml?-db=wim_MLB&-layoutnames</remarks>
        /// <returns>List of layout names</returns>
        private List<String> GetLayouts()
        {
            List<String> temp = new List<String>();

            // go grab the layouts for this file

            string URLString = BaseUrl + "?-db=" + CurrentDatabase + "&-layoutnames";
            string theError = "0";
            int foundLayouts = 0;

            HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(URLString);
            rq.Timeout = ResponseTimeout;  // default is 100,000 milliseconds (100 seconds)

            NetworkCredential nc = new NetworkCredential(FMAccount, FMPassword);
            rq.Credentials = nc;
            WebResponse wr = rq.GetResponse();

            using (XmlTextReader rdr = new XmlTextReader(URLString, wr.GetResponseStream()))
            {

                XmlUrlResolver resolver = null;
                if (DTDValidation == true)
                {
                    resolver = new XmlUrlResolver();
                    resolver.Credentials = nc;
                }

                rdr.XmlResolver = resolver;

                //Create the XmlDocument.
                XmlDocument response = new XmlDocument();

                try
                {
                    response.Load(rdr);
                    XmlNode root = response.DocumentElement;

                    //MessageBox.Show(root.Name);
                    foreach (XmlNode rootNode in root.ChildNodes)
                    {
                        // populate the lstLayouts
                        switch (rootNode.Name)
                        {
                            case "error":
                                theError = rootNode.Attributes.GetNamedItem("code").Value;
                                if (theError != "0")
                                {
                                    throw new System.Xml.XmlException("FileMaker Server returned an error in retrieving the XML.  Error: " + theError);
                                }
                                break;
                            case "resultset":

                                // make the label
                                foundLayouts = rootNode.ChildNodes.Count;

                                // loop through each record node and get the layoutnames
                                foreach (XmlNode lay in rootNode.ChildNodes)
                                {
                                    string theLay = lay.FirstChild.FirstChild.InnerText;
                                    temp.Add(theLay);
                                }
                                break;
                        }

                    } // foreach node under root

                } // try
                catch (System.Net.WebException anError)
                {
                    // would typically be an authorization problem, 401
                    // throw new System.Net.WebException(anError.Message);
                    if (anError.Message.Contains("(401) Unauthorized"))
                    {
                        throw new System.UnauthorizedAccessException("The provided account and password are not correct, or the privilege set does not have the fmxml privilege bit set.");
                    }
                }

                catch //(Exception anError)
                {
                    // looking for the error that comes from not having any
                    // priv sets with the fmxml bit set
                    throw;

                } // catch exception
            } // using

            return temp;

        } // GetLayouts

        /// <summary>
        /// Gets the list of scripts in the chosen file. Requires that a file has been chosen with SetDatabase().
        /// </summary>
        /// <remarks>http://testserver/fmi/xml/fmresultset.xml?-db=wim_MLB&-scriptnames</remarks>
        /// <returns>List of script names</returns>
        private List<String> GetScripts()
        {
            List<String> temp = new List<String>();

            // go grab the layouts for this file

            string URLString = BaseUrl + "?-db=" + CurrentDatabase + "&-scriptnames";
            string theError = "0";

            HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(URLString);
            rq.Timeout = ResponseTimeout;  // default is 100,000 milliseconds (100 seconds)

            NetworkCredential nc = new NetworkCredential(FMAccount, FMPassword);
            rq.Credentials = nc;
            WebResponse wr = rq.GetResponse();

            using (XmlTextReader rdr = new XmlTextReader(URLString, wr.GetResponseStream()))
            {

                XmlUrlResolver resolver = null;
                if (DTDValidation == true)
                {
                    resolver = new XmlUrlResolver();
                    resolver.Credentials = nc;
                }

                rdr.XmlResolver = resolver;
                //Create the XmlDocument.
                XmlDocument response = new XmlDocument();

                try
                {
                    response.Load(rdr);
                    XmlNode root = response.DocumentElement;

                    //MessageBox.Show(root.Name);
                    foreach (XmlNode rootNode in root.ChildNodes)
                    {
                        // populate the lstLayouts
                        switch (rootNode.Name)
                        {
                            case "error":
                                theError = rootNode.Attributes.GetNamedItem("code").Value;
                                if (theError != "0")
                                {
                                    throw new System.Xml.XmlException("FileMaker Server returned an error in retrieving the XML.  Error: " + theError);
                                }
                                break;
                            case "resultset":

                                // loop through each record node and get the script names
                                foreach (XmlNode script in rootNode.ChildNodes)
                                {
                                    string thescript = script.FirstChild.FirstChild.InnerText;
                                    temp.Add(thescript);
                                }
                                break;
                        }

                    } // foreach node under root

                } // try
                catch (System.Net.WebException anError)
                {
                    // would typically be an authorization problem, 401
                    // throw new System.Net.WebException(anError.Message);
                    if (anError.Message.Contains("(401) Unauthorized"))
                    {
                        throw new System.UnauthorizedAccessException("The provided account and password are not correct, or the privilege set does not have the fmxml privilege bit set.");
                    }
                }
                catch //(Exception anError)
                {

                } // catch exception
            } // using
            return temp;

        } // get scripts
        #endregion

        #region "Static Methods"

        /// <summary>
        /// Gets the root node of the XML document returned by FMSA.  This is the basis for all XML parsing.
        /// </summary>
        /// <param name="theURL">The URL.</param>
        /// <param name="theData"></param>
        /// <param name="account">The account.</param>
        /// <param name="pw">The pw.</param>
        /// <param name="timeout">The amount of time this web request should wait before its a timeout.</param>
        /// <param name="validateDtd">Determines if this request will use DtdValidation.</param>
        /// <returns>XmlNode root of the FMSA XML</returns>
        public static XmlNode RootOfDoc(String theUrl, String theData, String account, String pw, Int32 timeout, Boolean validateDtd)
        {
            // setup the credentials to use for making the request
            NetworkCredential nc = new NetworkCredential(account, pw);

            // prepare data for being sent 
            // takes our string and makes a byte array.
            var bytes = System.Text.Encoding.Default.GetBytes(theData);

            var rq = (HttpWebRequest)WebRequest.Create(theUrl);
            rq.Credentials = nc;    // set credentials
            rq.Timeout = timeout;   // default is 100,000 milliseconds (100 seconds)

            // setup the type of http request
            rq.ContentType = "application/x-www-form-urlencoded";
            // send all requests as HTTP POST, to ensure the data is not truncated
            rq.Method = "POST";
            rq.ContentLength = bytes.Length;

            // setup the stream and write the data
            using (var requestStream = rq.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();

                // get the respones stream from our "request"
                var webResponse = rq.GetResponse();

                using (var xmlResposneReader = new XmlTextReader(theUrl, webResponse.GetResponseStream()))
                {
                    // if we are going to validate, make 
                    // instance of the XmlUrlResolver
                    if (validateDtd == true)
                    {
                        var resolver = new XmlUrlResolver();
                        resolver.Credentials = nc;
                        xmlResposneReader.XmlResolver = resolver;
                    }
                    else
                    {
                        // do not do any validation
                        xmlResposneReader.XmlResolver = null;
                    }

                    try
                    {
                        // Create the XmlDocument.
                        XmlDocument response = new XmlDocument();
                        response.Load(xmlResposneReader);
                        return response.DocumentElement;
                    }
                    catch
                    {
                        // don't show message but raise an error!!
                        return null;
                    }
                    finally
                    {
                        xmlResposneReader.Close();
                    }
                } // using
            }
        } // RootOfDoc

        /// <summary>
        /// Counts the fields of a given XML FM fields node.
        /// </summary>
        /// <param name="r">XmlNode</param>
        /// <returns>integer, count of fields</returns>
        public static Int32 CountFields(XmlNode r)
        {
            Int32 temp = 0;
            // can't just do a childCount of the metadata node
            // some fields may be in portals
            foreach (XmlNode child in r.ChildNodes)
            {
                switch (child.Name)
                {
                    case "field-definition":
                        temp++;
                        break;

                    case "relatedset-definition":
                        temp += child.ChildNodes.Count;
                        break;
                }
            }
            return temp;
        }

        /// <summary>
        /// Populates the dataset row with FM data, used in the XML parsing code to create a DataSet.
        /// </summary>
        /// <param name="row">The row to put the data in.</param>
        /// <param name="theField">The XML node representing the FileMaker field.</param>
        /// <param name="f">The FMField data element to put the parsed data in.</param>
        /// <returns>DataRow with FM data</returns>
        public static DataRow PopulateRow(DataRow row, XmlNode theField, FMField[] f, String TimeStampFormat, String DateFormat)
        {
            /* now loop through all the fields in the record
             * and add the data to the row */

            String theName = theField.Attributes.GetNamedItem("name").Value;
            String currentType = "";
            for (int y = 0; y < f.Length; y++)
            {
                if (f[y].Name == theName)
                {
                    currentType = f[y].Result;
                    break;
                }
            }

            try
            {

                // add the data to the row ...
                if (theField.HasChildNodes)
                {
                    String theData = theField.FirstChild.InnerText;
                    if (theData.Length > 0)
                    {
                        // MessageBox.Show(theName + " = " + field.FirstChild.InnerText);
                        switch (currentType)
                        {
                            case "text":
                                row[theName] = theData;
                                break;
                            case "number":
                                // NB 9/18/2009 - Converted to TryParse to prevent errors on
                                // dirty data.
                                Double d = 0;
                                if (Double.TryParse(theData, out d))
                                {
                                    row[theName] = d;
                                }
                                else
                                {
                                    row[theName] = 0d;
                                }

                                //row[theName] = Convert.ToDouble(theData);    // ToInt32(theData);
                                break;
                            case "container":
                                row[theName] = theData;
                                break;
                            case "date":
                                //row[theName] = Convert.ToDateTime(theData);
                                row[theName] = ConvertToDate(theData, DateFormat);
                                break;
                            case "time":
                                // Change Koen Van Hulle - koen@shpartners.com
                                row[theName] = Convert.ToDateTime(theData);
                                break;
                            case "timestamp":
                                // Change Koen Van Hulle - koen@shpartners.com
                                //row[theName] = Convert.ToDateTime(theData);
                                row[theName] = ConvertToTimeStamp(theData, TimeStampFormat);
                                break;
                        }
                    } // if data length > 0
                } //  if field has child nodes

                return row;
            }
            catch (Exception ex)
            {
                ArgumentException ae = new ArgumentException(String.Format("Invalid data in {0}", theName), ex);
                throw ae;
            }
        }

        /// <summary>
        /// Convert to TimeStamp according to the XML-grammar
        /// Add Koen Van Hulle - SHpartners (http://www.shpartners.com)
        /// </summary>
        /// <returns>timestamp</returns>
        public static DateTime ConvertToTimeStamp(string TimeStampfield, string TimeStampFormat)
        {
            /*
             TimeStampFormat
             * MM = Month : 2 digits
             * dd = Day : 2 digits
             * yyyy = 4 digits
             * HH = hour : 2 digits
             * mm = minutes : 2 digits
             * ss = seconds : 2 digits
             
             */
            DateTime theTimeStamp;
            int posMM = TimeStampFormat.IndexOf("M");
            int posdd = TimeStampFormat.IndexOf("d");
            int posyyyy = TimeStampFormat.IndexOf("y");
            int posHH = TimeStampFormat.IndexOf("H");
            int posmm = TimeStampFormat.IndexOf("m");
            int posss = TimeStampFormat.IndexOf("s");

            int MM = Convert.ToInt32(TimeStampfield.Substring(posMM, 2));
            int dd = Convert.ToInt32(TimeStampfield.Substring(posdd, 2));
            int yyyy = Convert.ToInt32(TimeStampfield.Substring(posyyyy, 4));
            int HH = Convert.ToInt32(TimeStampfield.Substring(posHH, 2));
            int mm = Convert.ToInt32(TimeStampfield.Substring(posmm, 2));
            int ss = Convert.ToInt32(TimeStampfield.Substring(posss, 2));

            theTimeStamp = new DateTime(yyyy, MM, dd, HH, mm, ss);


            return theTimeStamp;
        }

        /// <summary>
        /// Convert to Date according to the XML-grammar
        /// Add Koen Van Hulle - SHpartners (http://www.shpartners.com)
        /// </summary>
        /// <returns>date</returns>
        public static DateTime ConvertToDate(String Datefield, String DateFormat)
        {
            /*
             DateFormat
             * MM = Month : 2 digits
             * dd = Day : 2 digits
             * yyyy = 4 digits
             */
            DateTime theDate;
            int posMM = DateFormat.IndexOf("M");
            int posdd = DateFormat.IndexOf("d");
            int posyyyy = DateFormat.IndexOf("y");

            int MM = Convert.ToInt32(Datefield.Substring(posMM, 2));
            int dd = Convert.ToInt32(Datefield.Substring(posdd, 2));
            int yyyy = Convert.ToInt32(Datefield.Substring(posyyyy, 4));

            theDate = new DateTime(yyyy, MM, dd);

            return theDate;
        }

        /// <summary>
        /// Gets the field type from the XML node representing the FileMaker field.
        /// </summary>
        /// <param name="fieldNode">The field node.</param>
        /// <returns>system Type info (number or text)</returns>
        public static Type GetSystemType(XmlNode fieldNode)
        {
            // loop through the attributes to get the type
            Type fieldType = Type.GetType("System.String");
            foreach (XmlAttribute attrib in fieldNode.Attributes)
            {
                switch (attrib.Name)
                {
                    case "result":

                        switch (attrib.Value.ToLower())
                        {
                            case "number":
                                fieldType = Type.GetType("System.Double");
                                break;

                            case "text":
                                fieldType = Type.GetType("System.String");
                                break;

                            case "date":
                                fieldType = Type.GetType("System.DateTime");
                                break;

                            case "timestamp":
                                // Add Koen Van Hulle - SHpartners
                                fieldType = Type.GetType("System.DateTime");
                                break;

                        } // switch attrib value
                        break;
                } // switch attrib name
            } // for each attrib
            return fieldType;
        }

        /// <summary>
        /// Handles the FMSA errors by throwing an error with relevant FM error numbers and description.  Throws an InvalidOperationException.
        /// </summary>
        /// <param name="theError">The error.</param>
        public static void HandleFMSerrors(String theError)
        {
            String errorDescription = "";
            // examines the error code generated by FMSA and throws an error
            switch (theError)
            {
                case "-3":
                    errorDescription = "FileMaker host not found or user name / password not correct or authorized";
                    break;

                case "-1":
                    errorDescription = "Unknown error";
                    break;

                case "1":
                    errorDescription = "User cancelled action";
                    break;

                case "2":
                    errorDescription = "Memory error";
                    break;

                case "3":
                    errorDescription = "Command is unavailable (for example, wrong operating system, wrong mode, etc.)";
                    break;

                case "4":
                    errorDescription = "Command is unknown";
                    break;

                case "5":
                    errorDescription = "Command is invalid (for example, a Set Field script step does not have a calculation specified)";
                    break;

                case "6":
                    errorDescription = "File is read-only";
                    break;

                case "7":
                    errorDescription = "Running out of memory";
                    break;

                case "8":
                    errorDescription = "Empty result";
                    break;

                case "9":
                    errorDescription = "Insufficient privileges";
                    break;

                case "10":
                    errorDescription = "Requested data is missing";
                    break;

                case "11":
                    errorDescription = "Name is not valid";
                    break;

                case "12":
                    errorDescription = "Name already exists";
                    break;

                case "13":
                    errorDescription = "File or object in use";
                    break;

                case "14":
                    errorDescription = "Out of range";
                    break;

                case "15":
                    errorDescription = "Can't divide by zero";
                    break;

                case "16":
                    errorDescription = "Operation failed, request retry.";
                    break;

                case "17":
                    errorDescription = "Attempt to convert foreign character set to UTF-16 failed";
                    break;

                case "18":
                    errorDescription = "Client must provide account information to proceed";
                    break;

                case "19":
                    errorDescription = "String contains characters other than A-Z, a-z, o-9 (ASCII)";
                    break;

                case "100":
                    errorDescription = "File is missing";
                    break;

                case "101":
                    errorDescription = "Record is missing";
                    break;

                case "102":
                    errorDescription = "Field is missing";
                    break;

                case "103":
                    errorDescription = "Relationship is missing";
                    break;

                case "104":
                    errorDescription = "Script is missing";
                    break;

                case "105":
                    errorDescription = "Layout is missing";
                    break;

                case "106":
                    errorDescription = "Table is missing";
                    break;

                case "107":
                    errorDescription = "Index is missing";
                    break;

                case "108":
                    errorDescription = "Value list is missing";
                    break;

                case "109":
                    errorDescription = "Privilege set is missing";
                    break;

                case "110":
                    errorDescription = "Related tables are missing";
                    break;

                case "111":
                    errorDescription = "Field repetition is invalid";
                    break;

                case "112":
                    errorDescription = "Window is missing";
                    break;

                case "113":
                    errorDescription = "Function is missing";
                    break;

                case "114":
                    errorDescription = "File reference is missing";
                    break;

                case "130":
                    errorDescription = "Files are damaged or missing and must be reinstalled";
                    break;

                case "131":
                    errorDescription = "Language pack files are missing (such as template files)";
                    break;

                case "200":
                    errorDescription = "Record access is denied";
                    break;

                case "201":
                    errorDescription = "Field cannot be modified";
                    break;

                case "202":
                    errorDescription = "Field access is denied";
                    break;

                case "203":
                    errorDescription = "No records in file to print or password doesn't allow print access";
                    break;

                case "204":
                    errorDescription = "No access to field(s) in sort order";
                    break;

                case "205":
                    errorDescription = "User does not have access privileges to create new records; import can overwrite existing data";
                    break;

                case "206":
                    errorDescription = "User does not have password change privileges, or file is not modifiable";
                    break;

                case "207":
                    errorDescription = "Cannot access field definitions or file is not modifiable";
                    break;

                case "208":
                    errorDescription = "Password does not contain enough characters";
                    break;

                case "209":
                    errorDescription = "New password must be different from existing one";
                    break;

                case "210":
                    errorDescription = "User account is inactive";
                    break;

                case "211":
                    errorDescription = "Password has expired";
                    break;

                case "212":
                    errorDescription = "Invalid user account and/or password. Please try again";
                    break;

                case "213":
                    errorDescription = "User account and/or password does not exist";
                    break;

                case "214":
                    errorDescription = "Too many login attempts";
                    break;

                case "215":
                    errorDescription = "Administrator privileges cannot be duplicated";
                    break;

                case "216":
                    errorDescription = "Guest account cannot be duplicated";
                    break;

                case "217":
                    errorDescription = "User does not have sufficient privileges to modify administrator account";
                    break;

                case "300":
                    errorDescription = "The file is locked or in use";
                    break;

                case "301":
                    errorDescription = "Record is in use by another user";
                    break;

                case "302":
                    errorDescription = "Table is in use by another user";
                    break;

                case "303":
                    errorDescription = "Database scheme is in use by another user";
                    break;

                case "304":
                    errorDescription = "Layout is in use by another user";
                    break;

                case "305":
                    errorDescription = "no such error as per FMI documentation";
                    break;

                case "306":
                    errorDescription = "Record modification ID does not match";
                    break;

                case "400":
                    errorDescription = "Find criteria is empty";
                    break;

                case "401":
                    errorDescription = "No records match the request";
                    break;

                case "402":
                    errorDescription = "Selected field is not a match field for a lookup";
                    break;

                case "403":
                    errorDescription = "Exceeding maximum record limit for trial version of FileMaker Pro";
                    break;

                case "404":
                    errorDescription = "Sort order is invalid";
                    break;

                case "405":
                    errorDescription = "Number of records specified exceeds number of records that can be omitted";
                    break;

                case "406":
                    errorDescription = "Replace/Reserialize criteria is invalid";
                    break;

                case "407":
                    errorDescription = "One or both match fields are missing (invalid relationship)";
                    break;

                case "408":
                    errorDescription = "Specified field has inappropriate data type for this operation";
                    break;

                case "409":
                    errorDescription = "Import order is invalid";
                    break;

                case "410":
                    errorDescription = "Export order is invalid";
                    break;

                case "411":
                    errorDescription = "Cannot perform delete because related records cannot be deleted ";
                    break;

                case "412":
                    errorDescription = "Wrong version of FileMaker Pro used to recover file";
                    break;

                case "413":
                    errorDescription = "Specified field has inappropriate field type";
                    break;

                case "414":
                    errorDescription = "Layout cannot display the result";
                    break;

                case "415":
                    errorDescription = "One or more required related records are not available";
                    break;

                case "500":
                    errorDescription = "Date value does not meet validation entry options";
                    break;

                case "501":
                    errorDescription = "Time value does not meet validation entry options";
                    break;

                case "502":
                    errorDescription = "Number value does not meet validation entry options";
                    break;

                case "503":
                    errorDescription = "Value in field is not within the range specified in validation entry options";
                    break;

                case "504":
                    errorDescription = "Value in field is not unique as required in validation entry options ";
                    break;

                case "505":
                    errorDescription = "Value in field is not an existing value in the database as required in validation entry options";
                    break;

                case "506":
                    errorDescription = "Value in field is not listed on the value list specified in validation entry option ";
                    break;

                case "507":
                    errorDescription = "Value in field failed calculation test of validation entry option";
                    break;

                case "508":
                    errorDescription = "Invalid value entered in Find mode ";
                    break;

                case "509":
                    errorDescription = "Field requires a valid value";
                    break;

                case "510":
                    errorDescription = "Related value is empty or unavailable";
                    break;

                case "511":
                    errorDescription = "Value in field exceeds maximum number of allowed characters";
                    break;

                case "600":
                    errorDescription = "Print error has occurred";
                    break;

                case "601":
                    errorDescription = "Combined header and footer exceed one page ";
                    break;

                case "602":
                    errorDescription = "Body doesn't fit on a page for current column setup ";
                    break;

                case "603":
                    errorDescription = "Print connection lost ";
                    break;

                case "700":
                    errorDescription = "File is of the wrong file type for import";
                    break;

                case "701":
                    errorDescription = "Data Access Manager can't find database extension file";
                    break;

                case "702":
                    errorDescription = "The Data Access Manager was unable to open the session";
                    break;

                case "703":
                    errorDescription = "The Data Access Manager was unable to open the session; try later";
                    break;

                case "704":
                    errorDescription = "Data Access Manager failed when sending a query ";
                    break;

                case "705":
                    errorDescription = "Data Access Manager failed when executing a query ";
                    break;

                case "706":
                    errorDescription = "EPSF file has no preview image ";
                    break;

                case "707":
                    errorDescription = "Graphic translator cannot be found ";
                    break;

                case "708":
                    errorDescription = "Can't import the file or need color computer to import file";
                    break;

                case "709":
                    errorDescription = "QuickTime movie import failed";
                    break;

                case "710":
                    errorDescription = "Unable to update QuickTime file reference because the database is read-only ";
                    break;

                case "711":
                    errorDescription = "Import translator can not be found";
                    break;

                case "712":
                    errorDescription = "XTND version is incompatible ";
                    break;

                case "713":
                    errorDescription = "Couldn't initialize the XTND system";
                    break;

                case "714":
                    errorDescription = "Password privileges do not allow the operation";
                    break;

                case "715":
                    errorDescription = "Specified Excel worksheet or named range is missing";
                    break;

                case "716":
                    errorDescription = "A SQL query using DELETE, INSERT, or UPDATE is not allowed for ODBC import";
                    break;

                case "717":
                    errorDescription = "There is not enough XML/XSL information to proceed with the import or export";
                    break;

                case "718":
                    errorDescription = "Error in parsing XML file (from Xerces)";
                    break;

                case "719":
                    errorDescription = "Error in Transforming XML using XSL (from Xalan)";
                    break;

                case "720":
                    errorDescription = "Error when exporting; intended document format does not support repeating fields";
                    break;

                case "721":
                    errorDescription = "Unknown error occurred in the parser or the transformer";
                    break;

                case "722":
                    errorDescription = "Cannot import data into a file that has no fields";
                    break;

                case "723":
                    errorDescription = "You do not have permission to add records to or modify records in the target table";
                    break;

                case "724":
                    errorDescription = "You do not have permission to add records to the target table";
                    break;

                case "725":
                    errorDescription = "You do not have permission to modify records in the target table";
                    break;

                case "726":
                    errorDescription = "There are more records in the import file than in the target table. Not all records were imported";
                    break;

                case "727":
                    errorDescription = "There are more records in the target table than in the import file. Not all records were updated";
                    break;

                case "729":
                    errorDescription = "Errors occurred during import. Records could not be imported";
                    break;

                case "730":
                    errorDescription = "Unsupported Excel version. (Convert file to Excel 7.0 (Excel 95), Excel 97, 2000, or XP format and try again)";
                    break;

                case "731":
                    errorDescription = "The file you are importing from contains no data";
                    break;

                case "732":
                    errorDescription = "This file cannot be inserted because it contains other files";
                    break;

                case "733":
                    errorDescription = "A table cannot be imported into itself";
                    break;

                case "734":
                    errorDescription = "This file type cannot be displayed as a picture";
                    break;

                case "735":
                    errorDescription = "This file type cannot be displayed as a picture. It will be inserted and displayed as a file";
                    break;

                case "800":
                    errorDescription = "Unable to create file on disk";
                    break;

                case "801":
                    errorDescription = "Unable to create temporary file on System disk";
                    break;

                case "802":
                    errorDescription = "Unable to open file ";
                    break;

                case "803":
                    errorDescription = "File is single user or host cannot be found";
                    break;

                case "804":
                    errorDescription = "File cannot be opened as read-only in its current state ";
                    break;

                case "805":
                    errorDescription = "File is damaged; use Recover command";
                    break;

                case "806":
                    errorDescription = "File cannot be opened with this version of FileMaker Pro ";
                    break;

                case "807":
                    errorDescription = "File is not a FileMaker Pro file or is severely damaged ";
                    break;

                case "808":
                    errorDescription = "Cannot open file because access privileges are damaged";
                    break;

                case "809":
                    errorDescription = "Disk/volume is full ";
                    break;

                case "810":
                    errorDescription = "Disk/volume is locked ";
                    break;

                case "811":
                    errorDescription = "Temporary file cannot be opened as FileMaker Pro file";
                    break;

                case "812":
                    errorDescription = "Cannot open the file because it exceeds host capacity ";
                    break;

                case "813":
                    errorDescription = "Record Synchronization error on network";
                    break;

                case "814":
                    errorDescription = "File(s) cannot be opened because maximum number is open";
                    break;

                case "815":
                    errorDescription = "Couldn't open lookup file";
                    break;

                case "816":
                    errorDescription = "Unable to convert file ";
                    break;

                case "817":
                    errorDescription = "Unable to open file because it does not belong to this solution";
                    break;

                case "818":
                    errorDescription = "FileMaker Pro cannot network for some reason";
                    break;

                case "820":
                    errorDescription = "File is in the process of being closed";
                    break;

                case "821":
                    errorDescription = "Host forced a disconnect";
                    break;

                case "822":
                    errorDescription = "FMI files not found; reinstall missing files";
                    break;

                case "823":
                    errorDescription = "Cannot set file to single-user, guests are connected";
                    break;

                case "824":
                    errorDescription = "File is damaged or not a FileMaker file";
                    break;

                case "900":
                    errorDescription = "General spelling engine error ";
                    break;

                case "901":
                    errorDescription = "Main spelling dictionary not installed ";
                    break;

                case "902":
                    errorDescription = "Could not launch the Help system";
                    break;

                case "903":
                    errorDescription = "Command cannot be used in a shared file";
                    break;

                case "904":
                    errorDescription = "Command can only be used in a file hosted under FileMakerÊServer";
                    break;

                case "905":
                    errorDescription = "No active field selected, command can only be used if there is an active field";
                    break;

                case "920":
                    errorDescription = "Can’t initialize the spelling engine";
                    break;

                case "921":
                    errorDescription = "User dictionary cannot be loaded for editing";
                    break;

                case "922":
                    errorDescription = "User dictionary cannot be found";
                    break;

                case "923":
                    errorDescription = "User dictionary is read-only";
                    break;

                case "950":
                    errorDescription = "Adding repeating related fields is not supported";
                    break;

                case "951":
                    errorDescription = "An unexpected error occurred";
                    break;

                case "952":
                    errorDescription = "Email error message, mail format not found";
                    break;

                case "953":
                    errorDescription = "Email error message, mail value missing";
                    break;

                case "954":
                    errorDescription = "Unsupported XML grammar";
                    break;

                case "955":
                    errorDescription = "No database name";
                    break;

                case "956":
                    errorDescription = "Maximum number of database sessions exceeded";
                    break;

                case "957":
                    errorDescription = "Conflicting commands";
                    break;

                case "958":
                    errorDescription = "Parameter missing in query";
                    break;

                case "971":
                    errorDescription = "The user name is invalid";
                    break;

                case "972":
                    errorDescription = "The password is invalid";
                    break;

                case "973":
                    errorDescription = "The database is invalid";
                    break;

                case "974":
                    errorDescription = "Permission denied";
                    break;

                case "975":
                    errorDescription = "The field has restricted access";
                    break;

                case "976":
                    errorDescription = "Security is disabled";
                    break;

                case "977":
                    errorDescription = "Invalid client IP address (for the IP restriction feature)";
                    break;

                case "978":
                    errorDescription = "The number of allowed guests has been exceeded ";
                    break;

                case "1200":
                    errorDescription = "Generic calculation error";
                    break;

                case "1201":
                    errorDescription = "Too few parameters in the function";
                    break;

                case "1202":
                    errorDescription = "Too many parameters in the function";
                    break;

                case "1203":
                    errorDescription = "Unexpected end of calculation";
                    break;

                case "1204":
                    errorDescription = "Number, text constant, field name or \"(\" expected";
                    break;

                case "1205":
                    errorDescription = "Comment is not terminated with \"*/\"";
                    break;

                case "1206":
                    errorDescription = "Text constant must end with a quotation mark";
                    break;

                case "1207":
                    errorDescription = "Unbalanced parenthesis";
                    break;

                case "1208":
                    errorDescription = "Operator missing, function not found or \"(\" not expected";
                    break;

                case "1209":
                    errorDescription = "Name (such as field name or layout name) is missing";
                    break;

                case "1210":
                    errorDescription = "Plug-in function has already been registered";
                    break;

                case "1211":
                    errorDescription = "List usage is not allowed in this function";
                    break;

                case "1212":
                    errorDescription = "An operator (for example, +, -, *) is expected here";
                    break;

                case "1213":
                    errorDescription = "This variable has already been defined in the Let function";
                    break;

                case "1214":
                    errorDescription = "AVERAGE, COUNT, EXTEND, GETREPETITION, MAX, MIN, NPV, STDEV, SUM and GETSUMMARY: expression found where a field alone is needed";
                    break;

                case "1215":
                    errorDescription = "This parameter is an invalid Get function parameter";
                    break;

                case "1216":
                    errorDescription = "Only Summary fields allowed as first argument in GETSUMMARY";
                    break;

                case "1217":
                    errorDescription = "Break field is invalid";
                    break;

                case "1218":
                    errorDescription = "Cannot evaluate the number";
                    break;

                case "1219":
                    errorDescription = "A field cannot be used in its own formula";
                    break;

                case "1220":
                    errorDescription = "Field type must be normal or calculated";
                    break;

                case "1221":
                    errorDescription = "Data type must be number, date, time, or timestamp";
                    break;

                case "1222":
                    errorDescription = "Calculation cannot be stored";
                    break;

                case "1223":
                    errorDescription = "The function referred to does not exist";
                    break;

                case "1400":
                    errorDescription = "ODBC client driver initialization failed; make sure the ODBC client drivers are properly installed.";
                    break;

                case "1401":
                    errorDescription = "Failed to allocate environment (ODBC)";
                    break;

                case "1402":
                    errorDescription = "Failed to free environment (ODBC)";
                    break;

                case "1403":
                    errorDescription = "Failed to disconnect (ODBC)";
                    break;

                case "1404":
                    errorDescription = "Failed to allocate connection (ODBC)";
                    break;

                case "1405":
                    errorDescription = "Failed to free connection (ODBC)";
                    break;

                case "1406":
                    errorDescription = "Failed check for SQL API (ODBC)";
                    break;

                case "1407":
                    errorDescription = "Failed to allocate statement (ODBC)";
                    break;

                case "1408":
                    errorDescription = "Extended error (ODBC)";
                    break;

            } // switch

            throw new System.InvalidOperationException("FileMaker Server returned error " + theError + ", " + errorDescription);

        } // handle FMS errors

        #endregion
    }
}