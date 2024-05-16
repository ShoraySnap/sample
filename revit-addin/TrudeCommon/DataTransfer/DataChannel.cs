﻿using NLog;
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

            logger.Info("Creating Memory mapped file : {0}", filename);
            mutex = new Mutex(false, eventName+"_MUTEX"); // DON'T CHANGE false here, otherwise mutex will be locked by this thread
        }

        public byte[] ReadData()
        {
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file: {0}! Reading...", filename);

            byte[] data = new byte[capacity];
            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.ReadArray(0, data, 0, data.Length);
                logger.Info("Read from memory mapped file \"{0}\" data length : {1} bytes", filename, data.Length);
            }
            mutex.ReleaseMutex();
            return data;
        }
        public void WriteData(byte[] data)
        {
            if(data.Length > capacity)
            {
                logger.Info("Data size too large for channel : {0}!", filename);
                return;
            }
            mutex.WaitOne();
            logger.Info("Got access to memory mapped file: {0}! Writing...", filename);

            using (MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor())
            {
                viewAccessor.WriteArray(0, data, 0, data.Length);
                logger.Info("Write to memory mapped file \"{0}\" data length : {1} bytes", filename, data.Length);
            }
            mutex.ReleaseMutex();
        }

    }
}
