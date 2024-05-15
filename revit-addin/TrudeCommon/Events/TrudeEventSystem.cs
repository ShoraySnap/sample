using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrudeCommon.Events
{
    // Intended flow
    // Init -> Add Event -> Add Event Handler -> Start -> Shutdown
    public class TrudeEventSystem
    {
        public static Lazy<TrudeEventSystem> instance = new Lazy<TrudeEventSystem>(() => new TrudeEventSystem());
        TrudeEventManager eventManager;
        ConcurrentQueue<TRUDE_EVENT> eventQueue ;
        static Logger logger = LogManager.GetCurrentClassLogger();

        public bool initialized = false;

        public static TrudeEventSystem Instance
        {
            get { return instance.Value; }
        }

        TrudeEventSystem()
        {
        }

        public void Init()
        {
            logger.Info("Creating event manager...");
            eventManager = new TrudeEventManager();

            logger.Info("Creating main thread event queue..."); // IF NEEDED FOR SOME EVENTS, OR WE CAN JUST RAISE IExternalEvents
            eventQueue = new ConcurrentQueue<TRUDE_EVENT>();

            logger.Info("Events System initialized!");
            initialized = true;
        }

        public void AddThreadEventHandler(TRUDE_EVENT name, Action handler)
        {
            if (!initialized)
            {
                logger.Error("Event System Uninitialized!");
                return;
            }
            eventManager.AddEventHandler(name, handler);
        }

        public void SubscribeToEvent(TRUDE_EVENT name)
        {
            if (!initialized)
            {
                logger.Error("Event System Uninitialized!");
                return;
            }
            eventManager.AddEvent(name, eventQueue);
        }

        public ConcurrentQueue<TRUDE_EVENT> GetQueue()
        {
            if (!initialized)
            {
                logger.Error("Event System Uninitialized!");
                return null;
            }
            return eventQueue;
        }

        public void Start()
        {
            if (!initialized)
            {
                logger.Error("Event System Uninitialized!");
                return;
            }
            eventManager.Start();
        }

        public  void Shutdown()
        {
            if (!initialized)
            {
                logger.Error("Event System Uninitialized!");
                return;
            }
            logger.Info("Events shutting down...");
            eventManager.Terminate();
            logger.Info("Events shutdown successful!");
        }
    }
}
