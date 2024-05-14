using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TrudeCommon.Events
{
    public class TrudeEventEmitter
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static void EmitEvent(TRUDE_EVENT type)
        {
            string name = TrudeEventUtils.GetEventName(type);
            logger.Info("Trying to emit event: {0}", name);
            EventWaitHandle handle = null;
            try
            {
                handle = EventWaitHandle.OpenExisting(name);
            }
            catch (WaitHandleCannotBeOpenedException ex)
            {
                logger.Info("EventHandle can't be opened : {0}", name);
            }

            if(handle != null)
            {
                logger.Info("Event found! Emitting.: {0}", name);
                handle.Set();
            }
        }
    }
}
