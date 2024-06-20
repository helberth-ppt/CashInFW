//-----------------------------------------------------------------------
// <copyright file="Instance.cs" company="Wincor Nixdorf Int. Gmbh">
//     Copyright (c) Wincor Nixdorf Int. Gmbh.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using ProTopas.Diagnostics;
namespace ProTopas.Impl.ASAICashInFW
{
    /// <summary>
    /// Class used externally to create the FrameWork(s) within the current dll.
    /// </summary>
    public class Instance
    {

        /// <summary>
        /// Initializes a new instance of the FrameWork 'Instance' class called externally during fwload(pm).
        /// </summary>
        public static int CreateFrameWorkInstance(string arguments)
        {


            CCTrcErr trc = new CCTrcErr(SegmentInfo.ModuleId, SegmentInfo.Segment);
            trc.Trace(TraceBits.TL_INFO, string.Format("> {0}_CreateFrameWorkInstance {1}", SegmentInfo.ASAIFramework, arguments));

            if (arguments == SegmentInfo.ASAIFramework)
            {
                ASAICashInFW fw = new ASAICashInFW(arguments);
                return fw.FrmGetHandle();
            }
            else
                return -1;
        }
    }
}
