using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TrudeCommon.Events
{
    internal class EventData
    {
        public EventWaitHandle waitHandle;
        public List<Action> handlers;
    }
    internal class ThreadLaunchData
    {
        public Dictionary<TRUDE_EVENT, EventData> eventData;
    }
    internal class TrudeEventManager
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public Dictionary<TRUDE_EVENT, EventData> eventData = new Dictionary<TRUDE_EVENT, EventData>();

        public bool started = false;
        Thread eventListenerThread = null;
        public TrudeEventManager() { }
        public static void ThreadFunc(object data)
        {
            ThreadLaunchData launchData = (ThreadLaunchData)data;
            Dictionary<TRUDE_EVENT, EventData> eventData = launchData.eventData;

            EventWaitHandle[] waitHandles = new EventWaitHandle[eventData.Keys.Count];
            List<Action>[] handlers = new List<Action>[eventData.Keys.Count];

            int idx = 0;
            foreach(TRUDE_EVENT name in eventData.Keys)
            {
                EventData ed = eventData[name];
                waitHandles[idx] = ed.waitHandle;
                handlers[idx] = ed.handlers;
                idx++;
            }


            while(true)
            {
                int signalIdx = -1;
                signalIdx = EventWaitHandle.WaitAny(waitHandles); // GET THE SIGNALING INDEX
                if (signalIdx < 0 || signalIdx > waitHandles.Length) continue;

                // FIRE ALL CALLBACKS
                foreach(Action action in handlers[signalIdx])
                {
                    action();
                }

                // RESET WAIT HANDLE
                waitHandles[signalIdx].Reset();
            }
        }


        public void AddEventHandler(TRUDE_EVENT eventType, Action handler)
        {
            if(started)
            {
                logger.Error("Event Manager has already started! Can't add event handler now.");
                return;
            }
            if(!eventData.ContainsKey(eventType))
            {
                logger.Error("Event not added.");
                return;
            }
            eventData[eventType].handlers.Add(handler);
        }

        public void AddEvent(TRUDE_EVENT eventType, ConcurrentQueue<TRUDE_EVENT> eventQueue)
        {
            if(eventData.ContainsKey(eventType))
            {
                return;
            }
            else
            {
                eventData.Add(eventType, new EventData());
                eventData[eventType].waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, TrudeEventUtils.GetEventName(eventType));
                eventData[eventType].handlers = new List<Action>
                {
                    () =>
                    {
                        logger.Info("Enqueuing event: {0}", TrudeEventUtils.GetEventName(eventType));
                        eventQueue.Enqueue(eventType);
                    }
                };
            }
        }

        public void Start()
        {
            started = true;

            ThreadLaunchData data = new ThreadLaunchData();
            data.eventData = eventData;

            eventListenerThread = new Thread(new ParameterizedThreadStart(ThreadFunc));
            eventListenerThread.IsBackground = true; // IMPORTANT
            eventListenerThread.Start(data);
        }

        public void Terminate()
        {
            if(eventListenerThread != null)
            {
                try
                {
                    eventListenerThread.Abort();
                } 
                catch (PlatformNotSupportedException ex)
                {
                    logger.Warn(ex);
                    logger.Warn("Platform doesn't support thread abort. Make sure thread is background!");
                }
            }
        }

    }
}
