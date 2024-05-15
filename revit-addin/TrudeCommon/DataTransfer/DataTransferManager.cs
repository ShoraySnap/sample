using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeCommon.DataTransfer
{
    public class DataTransferManager
    {
        private DataChannel inputChannel;
        private DataChannel outputChannel;

        public DataTransferManager(string inputChannelName, string outputChannelName, int size = 2048)
        {
            inputChannel = new DataChannel(inputChannelName, size);
            outputChannel = new DataChannel(outputChannelName, size);
        }

        public string ReadString()
        {
            byte[] data = inputChannel.ReadData();
            string value = Encoding.UTF8.GetString(data, 0, data.Length);
            value = value.Replace("\0", string.Empty).Trim();

            return value;
        }

        public void WriteString(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            outputChannel.WriteData(data);
        }
    }
}
