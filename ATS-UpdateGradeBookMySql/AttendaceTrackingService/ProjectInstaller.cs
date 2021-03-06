﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AttendaceTrackingService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        //Required Functional Pieces; each one is an installer. Pretty self explanatory
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;
        public ProjectInstaller()
        {
            InitializeComponent();
            //Initialize installers
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            //Set the account whose permissions the service will have
            processInstaller.Account = ServiceAccount.User;
            //If user is specified (as it is here) and you don't want to have to input the username and password of the account to use each time this is installed specify them as below
            processInstaller.Username = @"nwmsu\s525138";
            processInstaller.Password = "Shrikrishn@10";

            //Start type: whether or not the service must be started manually or will start on server start
            serviceInstaller.StartType = ServiceStartMode.Manual;

            //The name you reference when starting the service
            serviceInstaller.ServiceName = "ATSManagement";

            //The name the service will have when looking at the services on a machine
            serviceInstaller.DisplayName = "ATSManagement";
            //Add the installers pieces you made to THE installers
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
