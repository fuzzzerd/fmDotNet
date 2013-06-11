/*
 * Use XML Code Comments on all public methods. Add revisions here with Date/Time and initials.
 * Revisions:
 *  # NB - 10/28/2008 10:29:01 AM - Source File Created
 * 
 */

namespace fmDotNet.Enumerations
{
    /// <summary>
    /// The 2 different schemes to access FMSA through the web server.
    /// </summary>
    public enum Scheme 
    {
        /// <summary>
        /// Use standard HTTP traffic.
        /// </summary>
        HTTP,
        /// <summary>
        /// Use encryped HTTPS (SSL/TLS) traffic.
        /// </summary>
        HTTPS 
    };
}