using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using TrudeCommon.DataTransfer;

namespace TrudeCommon.Events
{
    public class TrudeEventEmitter
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static void EmitEvent(TRUDE_EVENT type)
        {
            if(!HandshakeManager.IsHandshakeValid())
            {
                logger.Warn("Handshake not matching, abort emitting event with data!");
                return;
            }

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

        public static void EmitEventWithStringData(TRUDE_EVENT type, string data, DataTransferManager manager)
        {
            if(!HandshakeManager.IsHandshakeValid())
            {
                logger.Warn("Handshake not matching, abort emitting event with data!");
            }

            logger.Trace("Transferring data for event: {0} data: {1}", TrudeEventUtils.GetEventName(type), data);
            manager.WriteString(type, data);
            EmitEvent(type);
        }
    }
}
