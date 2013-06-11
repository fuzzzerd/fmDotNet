/*
 * Use XML Code Comments on all public methods. Add revisions here with Date/Time and initials.
 * Revisions:
 *  # NB - 10/28/2008 10:37:19 AM - Source File Created
 * 
 */

namespace fmDotNet.Enumerations
{
    /// <summary>
    /// The different search options you can use.
    /// </summary>
    public enum SearchOption 
    { 
        /// <summary>
        /// Field matches search value exactly.
        /// </summary>
        equals, 
        /// <summary>
        /// Field contains search value.
        /// </summary>
        contains, 
        /// <summary>
        /// Field starts with search value.
        /// </summary>
        beginsWith, 
        /// <summary>
        /// Field ends with search value.
        /// </summary>
        endsWith, 
        /// <summary>
        /// Field is greater than search value.
        /// </summary>
        biggerThan, 
        /// <summary>
        /// Field is greater than or equal to search value.
        /// </summary>
        biggerOrEqualThan, 
        /// <summary>
        /// Field is less than search value.
        /// </summary>
        lessThan, 
        /// <summary>
        /// Field is less than or equal to search value.
        /// </summary>
        lessOrEqualThan, 
        /// <summary>
        /// Exclude records that match search value.
        /// </summary>
        omit
    }
}