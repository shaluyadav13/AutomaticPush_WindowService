using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Globalization;
using System.Configuration;
using System.Collections.Generic;
using System.Threading;

using DbLinq.Data.Linq;
using DbLinq.Data.Linq.Mapping;
using System.Linq.Expressions;


namespace AttendaceTrackingService
{
    public partial class ATSManagement : ServiceBase
    {


        private static int maxThreads = 1;

        public ATSManagement()
        {
            InitializeComponent();
        }

        public void start()
        {
            ApplicationLog.WriteDataLog("Service Started" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
            #region startTry
            try
            {
                #region using
                using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
                {
                    int threadIndex = 0, callNumIndex = 0, callCnt, sectCnt=0;

                   // UpdateGradebook[] upGb = { new UpdateGradebook(), new UpdateGradebook(), new UpdateGradebook(), new UpdateGradebook() };
                    UpdateGradebook upGb = new UpdateGradebook();

                    string day = "";
                    //DateTime morningStartTime = Convert.ToDateTime("14:10");
                    //DateTime eveningStartTime = Convert.ToDateTime("20:00");
                    if (DateTime.Now.DayOfWeek.ToString().ToLower() == "thursday")
                    {
                        day = "R";

                    }
                    else
                    {
                        day = DateTime.Now.DayOfWeek.ToString()[0].ToString();
                    }
                    var d = DateTime.Now.Hour;
                    var courseCallNumbers = from sec in db.sections
                                            where sec.days.Contains(day)
                                            //&& sec.end.Value <= TimeSpan.FromHours(DateTime.Now.Hour)
                                            select new { sec.call_number, sec.course_name};
                    var courseCallNumbers2 = from sec in db.sections
                                             where sec.days.Contains(day)
                                             //&& sec.end.Value <= TimeSpan.FromHours(DateTime.Now.Hour)
                                             select sec;
                    //var test1 = TimeSpan.FromHours(DateTime.Now.Hour);
                    #region Evening
                    //if (DateTime.Now > morningStartTime)
                    //{
                    //    courseCallNumbers = from sec in db.sections
                    //                        //where sec.days.Contains(day)
                    //                        //&& sec.end.Value <= TimeSpan.FromHours(DateTime.Now.Hour)
                    //                        //&& sec.end.Value >= TimeSpan.FromHours(morningStartTime.Hour)
                    //                        select new { sec.call_number, sec.course_name };

                    //    var hour = from sec in db.sections
                    //               //where sec.days.Contains(day)
                    //               select new { sec.end };

                    //    courseCallNumbers2 = from sec in db.sections
                    //                         //where sec.days.Contains(day)
                    //                         //&& sec.end.Value <= TimeSpan.FromHours(DateTime.Now.Hour)
                    //                         //&& sec.end.Value >= TimeSpan.FromHours(morningStartTime.Hour)
                    //                         select sec;
                    //}
                    #endregion

                    callCnt = courseCallNumbers2.Count();
                    ApplicationLog.WriteDataLog("Total number of courses updating now:" + callCnt);
                    if (callCnt != 0)
                    {
                        sectCnt = callCnt / maxThreads;
                        string[][] callNumbers = new string[maxThreads][];
                        callNumbers[0] = new string[sectCnt];
                        int count = 0;
                        foreach (var callNumb in courseCallNumbers)
                        {
                            callNumbers[threadIndex][callNumIndex] = callNumb.call_number;
                            count++;
                            upGb.callNumbers = callNumbers[threadIndex];
                            upGb.callNumbersCount = sectCnt + 1;
                            //if call numbers array row per thread is filled up and ready to run.
                            //if (callNumIndex >= sectCnt)
                            //{
                            //    upGb[threadIndex].callNumbers = callNumbers[threadIndex];
                            //    upGb[threadIndex].callNumbersCount = sectCnt + 1;
                            //    threadIndex++;
                            //    if (threadIndex != maxThreads)
                            //    {
                            //        sectCnt = (callCnt - count) / (maxThreads - threadIndex);
                            //        //to check number of columns required
                            //        if ((callCnt - count) % (maxThreads - threadIndex) == 0)
                            //        {
                            //            callNumbers[threadIndex] = new string[sectCnt];
                            //            sectCnt = sectCnt - 1;
                            //        }
                            //        else
                            //        {
                            //            callNumbers[threadIndex] = new string[sectCnt + 1];
                            //        }
                            //    }
                            //    callNumIndex = 0;
                            //}
                            //else
                             callNumIndex++;
                        }

                        ApplicationLog.WriteDataLog("Updating Gradebook for courses started" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                        try
                        {

                            Parallel.Invoke(() => upGb.UpdateCourses()
                                //, () => upGb[1].UpdateCourses1()
                                //, () => upGb[2].UpdateCourses2()
                                //, () => upGb[3].UpdateCourses3()
                            );
                        }
                        catch (Exception ex)
                        {
                            ApplicationLog.WriteDataLog("Parallel Exception:" + ex.Message + "_" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                        }
                        ApplicationLog.WriteDataLog("Gradebook updated successfully at:" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                    }
                    else
                    {
                        ApplicationLog.WriteDataLog("There are no courses to update at:" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                    }
                }
                #endregion //using


                this.OnStop();
            }
            #endregion //startTry
            catch (Exception e)
            {
                //writing exception information to log file
                ApplicationLog.WriteDataLog("Exception:" + e.Message + "_" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                //stops the service in case of any exception

                this.OnStop();
            }
        }
        protected override void OnStart(string[] args)
        {
            Thread worker = new Thread(start);
            worker.IsBackground = false;
            worker.Start();
            base.OnStart(args);

        }
        protected override void OnStop()
        {
            ServiceController service = new ServiceController();
            ApplicationLog.WriteDataLog(service.Status + " Service Stopped at" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
            ApplicationLog.WriteDataLog("-----------------------------------------------------------------------------------------------------");
            service.Stop();
        }
    }
}