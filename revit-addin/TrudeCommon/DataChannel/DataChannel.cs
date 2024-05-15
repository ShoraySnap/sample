using NLog;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace TrudeCommon.DataChannel
{
    public  class DataChannel
    {
        private MemoryMappedFile mmf;
        private Mutex mutex;
        static Logger logger = LogManager.GetCurrentClassLogger();
        int capacity = 0;
        public DataChannel(string filename, int capacity)
        {
            this.capacity = capacity;
            mmf = MemoryMappedFile.CreateOrOpen(filename, capacity);
            mutex = new Mutex(false, filename+"_MUTEX"); // DON'T CHANGE false here, otherwise mutex will be locked by this thread
        }

        public void WriteDataString(string data)
        {
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file!");

            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                viewAccessor.WriteArray(0, buffer, 0, buffer.Length);
                logger.Info("Written to memory mapped file! data : \"{0}\"", data);

            }

            mutex.ReleaseMutex();
        }

        public string ReadDataAsString()
        {
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file!");

            string data = "";

            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                byte[] readBuffer = new byte[capacity];
                viewAccessor.ReadArray(0, readBuffer, 0, readBuffer.Length);
                data = Encoding.UTF8.GetString(readBuffer);
                logger.Info("Read from memory mapped file! data : \"{0}\"", data);
            }

            mutex.ReleaseMutex();

            return data;
        }

    }
}
