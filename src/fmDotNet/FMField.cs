/*
 * Revisions:
 *  # NB - 10/28/2008 10:42:22 AM - Source File Created
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace fmDotNet
{
    /// <summary>
    /// Unit that holds information about a FileMaker field.
    /// </summary>
    public struct FMField
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name;
        /// <summary>
        /// The data type of the field (text, number, container,...)
        /// </summary>
        public string Result;
        /// <summary>
        /// "true" if the field is a global
        /// </summary>
        public string Global;
        /// <summary>
        /// Field type (normal, calculated,...)
        /// </summary>
        public string Type;
        /// <summary>
        /// The number of repetitions the field has
        /// </summary>
        public int RepetitionCount;
        /// <summary>
        /// If the field is displayed through a portal, this will represent the name of the relationship used for the portal.
        /// </summary>
        public string Portal;
        /* not included but available if we want to grab it
         * auto-enter
         * not-empty
         */
    }
}