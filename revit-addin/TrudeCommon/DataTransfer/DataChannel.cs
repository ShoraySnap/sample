using NLog;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace TrudeCommon.DataTransfer
{
    internal class DataChannel
    {
        private MemoryMappedFile mmf;
        private Mutex mutex;
        static Logger logger = LogManager.GetCurrentClassLogger();
        int capacity = 0;
        string filename = string.Empty;
        public DataChannel(string filename, int capacity)
        {
            this.capacity = capacity;
            this.filename = filename;
            mmf = MemoryMappedFile.CreateOrOpen(filename, capacity);
            mutex = new Mutex(false, filename+"_MUTEX"); // DON'T CHANGE false here, otherwise mutex will be locked by this thread
        }

        public byte[] ReadData()
        {
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file! Reading...");

            byte[] data = new byte[capacity];
            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.ReadArray(0, data, 0, data.Length);
                logger.Info("Read from memory mapped file data length : {0} bytes", data.Length);
            }
            mutex.ReleaseMutex();
            return data;
        }
        public void WriteData(byte[] data)
        {
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file! Writing...");

            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.WriteArray(0, data, 0, data.Length);
                logger.Info("Write to memory mapped file \"{0}\" data length : {1} bytes", filename, data.Length);
            }
            mutex.ReleaseMutex();
        }

    }
}
