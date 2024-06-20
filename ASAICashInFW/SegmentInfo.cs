//-----------------------------------------------------------------------
// <copyright file="SegmentInfo.cs" company="Wincor Nixdorf Int. Gmbh">
//     Copyright (c) Wincor Nixdorf Int. Gmbh.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace ProTopas.Impl.ASAICashInFW
{
    /// <summary>
    /// Internal class holding the module segment information of ForAllFW assembly.
    /// </summary>
    internal class SegmentInfo
    {
        /// <summary>
        /// Gets the module segment string.
        /// </summary>
        public static string Segment
        {
            get
            {
                return string.Format("$MOD$ 180220 1200 {0}.dll", ASAIFramework);
            }
        }

        /// <summary>
        /// Gets the module identifier.
        /// </summary>
        public static int ModuleId
        {
            get
            {
                return 1200;
            }
        }

        public static string ASAIFramework
        {
            get
            {
                return "ASAICashInFW";
            }
        }


    }
}
