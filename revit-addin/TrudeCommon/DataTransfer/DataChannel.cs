using NLog;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace TrudeCommon.DataTransfer
{
    public class DataChannel
    {
        private MemoryMappedFile mmf;
        private Mutex mutex;
        static Logger logger = LogManager.GetCurrentClassLogger();
        int capacity = 0;
        string filename = string.Empty;
        public DataChannel(string eventName, int capacity)
        {
            this.capacity = capacity;
            this.filename = eventName + "_MMF"; // THIS NAMING IS IMPORTANT, the event names are already registered 
            mmf = MemoryMappedFile.CreateOrOpen(filename, capacity);

            logger.Debug("Creating Memory mapped file : {0}", filename);
            mutex = new Mutex(false, eventName+"_MUTEX"); // DON'T CHANGE false here, otherwise mutex will be locked by this thread
        }

        public byte[] ReadData()
        {
            mutex.WaitOne();
            logger.Debug("Got access to memory mapped file: {0}! Reading...", filename);

            byte[] data = new byte[capacity];
            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.ReadArray(0, data, 0, data.Length);
                logger.Debug("Read from memory mapped file \"{0}\" data length : {1} bytes", filename, data.Length);
            }
            mutex.ReleaseMutex();
            return data;
        }
        public void WriteData(byte[] data)
        {
            if(data.Length > capacity)
            {
                logger.Error("Data size too large for channel : {0}!", filename);
                return;
            }
            mutex.WaitOne();
            logger.Debug("Got access to memory mapped file: {0}! Writing...", filename);

            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.WriteArray(0, data, 0, data.Length);
                // Write zeros after the data
                int zerosLength = capacity - data.Length; // Specify the number of zeros you want to write
                byte[] zeros = new byte[zerosLength]; // Create an array of zeros
                viewAccessor.WriteArray(data.Length, zeros, 0, zeros.Length); 
                logger.Debug("Write to memory mapped file \"{0}\" data length : {1} bytes", filename, data.Length);
            }
            mutex.ReleaseMutex();
        }

        public string ReadString()
        {
            byte[] data = ReadData();
            string value = Encoding.UTF8.GetString(data, 0, data.Length);
            value = value.Replace("\0", string.Empty).Trim();

            logger.Debug("Read String: {0}", value);
            return value;
        }

        public void WriteString(string str)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);
            WriteData(data);
            logger.Debug("Written String: {0}", str);
        }

    }
}
