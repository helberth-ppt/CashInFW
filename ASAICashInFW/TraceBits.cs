//-----------------------------------------------------------------------
// <copyright file="TraceBits.cs" company="Wincor Nixdorf Int. Gmbh">
//     Copyright (c) Wincor Nixdorf Int. Gmbh.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace ProTopas.Impl.ASAICashInFW
{
    /// <summary>
    /// Internal class holding the module segment information of ProTopas.Impl.ForAllFW assembly.
    /// </summary>
    internal class TraceBits
    {
        /// <summary>
        /// Gets the 'main function info' trace bit.
        /// </summary>
        public static byte TL_CALL
        {
            get
            {
                return 10;
            }
        }

        public static byte TL_INFO
        {
            get
            {
                return 11;
            }
        }

        public static byte TL_DATAL
        {
            get
            {
                return 20;
            }
        }
    }
}
