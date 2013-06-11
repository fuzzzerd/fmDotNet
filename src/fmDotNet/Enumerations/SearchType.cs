/* 
 * Use XML Code Comments on all public methods. Add revisions here with Date/Time and initials.
 * Revisions:
 *  # NB - 10/28/2008 10:27:12 AM - Source File Created
 * 
 */

namespace fmDotNet.Enumerations
{
    /// <summary>
    /// The 3 different find requests: -find, -findall and -findany
    /// </summary>
    public enum SearchType 
    {   
        /// <summary>
        /// Returns a limited set of results.
        /// </summary>
        Subset, 
        /// <summary>
        /// Returns ALL records.
        /// </summary>
        AllRecords, 
        /// <summary>
        /// Returns a SINGLE random record.
        /// </summary>
        RandomRecord
    }
}