using System;
using System.Collections.Generic;
using System.Text;
using TrudeCommon.Events;

namespace TrudeCommon.DataTransfer
{
    public class DataTransferManager
    {
        int defaultSize = 1024;

        Dictionary<TRUDE_EVENT, DataChannel> channels = new Dictionary<TRUDE_EVENT, DataChannel>();

        private DataChannel GetChannel(TRUDE_EVENT eventType, Dictionary<TRUDE_EVENT, DataChannel> dict)
        {
            if(dict.ContainsKey(eventType))
            {
                return dict[eventType];
            }
            else
            {
                dict[eventType] = new DataChannel(TrudeEventUtils.GetEventName(eventType), defaultSize);
                return dict[eventType];
            }
        }
        public string ReadString(TRUDE_EVENT eventType)
        {
            DataChannel inputChannel = GetChannel(eventType, channels);
            return inputChannel.ReadString();
        }

        public void WriteString(TRUDE_EVENT eventType, string value)
        {
            DataChannel outputChannel = GetChannel(eventType, channels);
            outputChannel.WriteString(value);
        }
    }
}
