﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AttendaceTrackingService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
//#if (!DEBUG)
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
            { 
                new ATSManagement() 
            };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteDataLog(ex.Message);
                //stops the service in case of any exception
                ATSManagement service = new ATSManagement();
                service.Stop();
            }
//#else
//            //debug code: this allows the process to run as a non-service.
//            //it will kick off the service start point, but never kill it.
//            //shut down the debugger to exit
            ATSManagement service1 = new ATSManagement();
            service1.start();
//#endif
        }
    }
}
