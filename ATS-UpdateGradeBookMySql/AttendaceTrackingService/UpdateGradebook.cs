using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System.IO;
using System.Net;
//using System.Threading;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace AttendaceTrackingService
{
    class UpdateGradebook
    {
        public int callNumbersCount;
        public string[] callNumbers;
        public UpdateGradebook()
        {
            callNumbers = new string[callNumbersCount];
        }
        public void UpdateCourses()
        {
            ApplicationLog.WriteThread1Log("UpdateCourses function" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
            #region VariablesDeclaration
            String access_token = "7438~uxczK5OTXW82M066sETZylnpYpPwsWvRG4j0JttEnzLoRiogY47iKl0wnnWcCchs";
            Dictionary<string, List<int>> emailandattendance;
            List<int> studentAttendanceInfo;
            //variables to store unique identifiers
            Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();
            Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();
            //Dictionary<string, double> AttendanceDictionary = new Dictionary<string, double>();
            Dictionary<string, double> studentAndAttndPercentge = new Dictionary<string, double>();
            Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
            int present = 0;

            int courseCount = 0;

            #endregion //VariablesDeclaration
            //File.WriteAllText(@"C:\Thread1.txt", String.Empty);

            try
            {

                ApplicationLog.WriteThread1Log("Updating Gradebook in this thread started at: " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                ApplicationLog.WriteThread1Log("Total courses updating in this thread are:" + callNumbersCount);

                #region Updating all courses
                foreach (string callNum in callNumbers)
                {
                    emailandattendance = new Dictionary<string, List<int>>();
                    //courseCount variable is to log the current number of course which is updating
                    courseCount++;
                    ApplicationLog.WriteThread1Log(courseCount + "." + "Course updating: _" + callNum);
                    //getting enrolled students ina course from our database
                    using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
                    {
                        int count = (from stu in db.students.ToList() // Total Number of Students
                                     join en in db.enrollments
                                     on stu.student_id equals en.student_id
                                     where en.call_number == callNum
                                     select stu).Count();


                        ApplicationLog.WriteThread1Log(courseCount + "." + "Total number of students in " + callNum + " are " + count);

                        int totalClasses = (from at in db.attendances.ToList() // Total Number of classes
                                            where at.call_number == callNum
                                            select DateTime.Parse(at.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();

                        ApplicationLog.WriteThread1Log(courseCount + "." + "Total classes for " + callNum + " " + totalClasses);

                        //var studentEnrolls = from stu in db.students // Fetching the student entrollments in the course
                        //                     join en in db.enrollments
                        //                     on stu.student_id equals en.student_id
                        //                     where en.call_number == callNum
                        //                     select new { stu.s_number, stu.student_id };
                        var stuEnrolls = from stu in db.students.AsEnumerable()
                                         join enr in db.enrollments
                                         on stu.student_id equals enr.student_id
                                         where enr.call_number == callNum
                                         orderby stu.name.Split(' ')[stu.name.Split(' ').Length - 1]
                                         select stu;

                        //getting student attendance details from database and adding it to emailandattendace list
                        #region studentattendaceInfo
                        if (count != 0)
                        {
                            foreach (var students in stuEnrolls)
                            {
                                studentAttendanceInfo = new List<int>();
                                present = (from att in db.attendances.ToList()
                                           where att.call_number == callNum && att.student_id == students.student_id
                                           select DateTime.Parse(att.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();
                                //studentAttendanceInfo.Add(present);
                                //missed = totalClasses - present;
                                //studentAttendanceInfo.Add(missed);
                                // AttendanceDictionary.Add(students.s_number, Convert.ToDouble(present));
                               // AttendanceDictionary[students.s_number] = (Convert.ToDouble(present));
                                if (studentAndAttndPercentge.ContainsKey(students.student_id))
                                {
                                    studentAndAttndPercentge[students.student_id] = (Convert.ToDouble(present));
                                }
                                else
                                {
                                    studentAndAttndPercentge.Add(students.student_id, (Convert.ToDouble(present)));
                                }
                            }
                        #endregion

                            string courseID = getCourseID(callNum);
                           // List<String> list = new List<string>();
                            //using (CourseIdReportDataContext Cid = new CourseIdReportDataContext())
                            //{
                            //    list = (from cid in Cid.CourseIdReports.ToList()
                            //                where cid.callNumber == callNum
                            //                select cid.courseId).ToList();
                            //}

                            ApplicationLog.WriteThread1Log(courseCount + "." + "Northwest Online course id for  " + callNum + " is " + courseID);
                            int countTotal = totalClasses;
                            string callNumber = callNum;
                            if (courseID != "")
                            {
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/students?access_token={2}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, access_token));
                                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                request.Method = "GET";
                                request.ContentType = "application/json";
                                request.UseDefaultCredentials = true;
                                request.PreAuthenticate = true;
                                request.Credentials = CredentialCache.DefaultCredentials;
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                List<Students> stuList = new List<Students>();
                                StreamReader reader = new StreamReader(response.GetResponseStream());
                                var objText1 = reader.ReadToEnd();
                                reader.Close();
                                var result = JToken.Parse(objText1);
                                if (result is JArray)
                                {
                                    foreach (JObject o in result.Children<JObject>())
                                    {
                                        int id = (int)o["id"];
                                        String name = o["name"].ToString();
                                        String sis_user_id = "";
                                        if (o["sis_user_id"] != null)
                                        {
                                            sis_user_id = o["sis_user_id"].ToString();
                                        }

                                        String sis_login_id = "";
                                        if (o["sis_login_id"] != null)
                                        {
                                            sis_login_id = o["sis_login_id"].ToString();
                                        }
                                        Students stu = new Students(id, name, sis_user_id, sis_login_id);
                                        stuList.Add(stu);
                                    }
                                    // Session["Student_list"] = stuList;
                                }
                                else
                                {
                                    // Session["Student_list"] = "";
                                }

                                ApplicationLog.WriteThread1Log("Number of Students " + stuList.Count + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");

                                Double totalPoints = Convert.ToDouble(totalClasses);
                                String assignmentID = "";
                                String assignID = getAssignmentID(courseID);
                                if (assignID == "")
                                {
                                    request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
                                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                    request.Method = "POST";
                                    request.ContentType = "application/json";
                                    request.UseDefaultCredentials = true;
                                    request.PreAuthenticate = true;
                                    request.Credentials = CredentialCache.DefaultCredentials;
                                    response = (HttpWebResponse)request.GetResponse();
                                    reader = new StreamReader(response.GetResponseStream());
                                    var objAssignment = reader.ReadToEnd();
                                    reader.Close();
                                    var resultAssignment = JToken.Parse(objAssignment);
                                    assignmentID = resultAssignment["id"].ToString();
                                    storeAssignmentDetails(assignmentID, courseID);
                                    ApplicationLog.WriteThread1Log("assignmentID " + assignmentID + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                    ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                                }
                                else
                                {
                                    try
                                    {
                                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, assignID, access_token));
                                        ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                        request.Method = "GET";
                                        request.ContentType = "application/json";
                                        request.UseDefaultCredentials = true;
                                        request.PreAuthenticate = true;
                                        request.Credentials = CredentialCache.DefaultCredentials;
                                        response = (HttpWebResponse)request.GetResponse();
                                        reader = new StreamReader(response.GetResponseStream());
                                        var objAssignment = reader.ReadToEnd();
                                        reader.Close();
                                        assignmentID = assignID;
                                        try
                                        {
                                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}&assignment[points_possible]={4}&assignment[name]={5}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, assignID, access_token, totalPoints, "Attendance Swipes"));
                                            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                            request.Method = "PUT";
                                            request.ContentType = "application/json";
                                            request.UseDefaultCredentials = true;
                                            request.PreAuthenticate = true;
                                            request.Credentials = CredentialCache.DefaultCredentials;
                                            response = (HttpWebResponse)request.GetResponse();
                                            reader = new StreamReader(response.GetResponseStream());
                                            var objEditAssignment = reader.ReadToEnd();
                                            reader.Close();
                                            ApplicationLog.WriteThread1Log("Updated Assignment ID: " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                            ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                                        }
                                        catch (Exception ex)
                                        {
                                            //  ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('Something went wrong trying connect to Canvas.');window.location='Error.aspx';", false);
                                            ApplicationLog.WriteThread1Log("Updated Assignment ID: " + ex.Message + "Assignment Data not Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                            ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
                                        ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                        request.Method = "POST";
                                        request.ContentType = "application/json";
                                        request.UseDefaultCredentials = true;
                                        request.PreAuthenticate = true;
                                        request.Credentials = CredentialCache.DefaultCredentials;
                                        response = (HttpWebResponse)request.GetResponse();
                                        reader = new StreamReader(response.GetResponseStream());
                                        var objAssignment = reader.ReadToEnd();
                                        reader.Close();
                                        var resultAssignment = JToken.Parse(objAssignment);
                                        assignmentID = resultAssignment["id"].ToString();
                                        updateAssignmentDetails(assignmentID, courseID);
                                        ApplicationLog.WriteThread1Log("Assignment Creation if its deleted " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                        ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                                    }
                                }



                                foreach (var student in stuList)
                                {

                                    if (studentAndAttndPercentge.ContainsKey(student.sis_user_id))
                                    {
                                        String pointsattained = studentAndAttndPercentge[student.sis_user_id].ToString();
                                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}/submissions/{3}?access_token={4}&submission[posted_grade]={5}", "https://nwmissouri.test.instructure.com/api/v1/courses/", courseID, assignmentID, student.id, access_token, pointsattained));
                                        ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                                        request.Method = "PUT";
                                        request.ContentType = "application/json";
                                        request.UseDefaultCredentials = true;
                                        request.PreAuthenticate = true;
                                        request.Credentials = CredentialCache.DefaultCredentials;
                                        response = (HttpWebResponse)request.GetResponse();
                                        reader = new StreamReader(response.GetResponseStream());
                                        var objSubmission = reader.ReadToEnd();
                                        reader.Close();
                                        ApplicationLog.WriteThread1Log("Submission of grades for students, points attained " + pointsattained + "Assignment submitted" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                                        ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                                    }
                                }
                            }

                        }
                    }
                    ApplicationLog.WriteThread1Log(courseCount + "." + "Gradebook with attendance information is updated for " + callNum + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                    ApplicationLog.WriteThread1Log("------------------------------------------------------------------------");
                }
                #endregion //end of updating all courses

            }//end of try
            catch (Exception ex)
            {

                ApplicationLog.WriteThread1Log(ex.Message + " " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                throw ex;
            }
        }

        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public void storeAssignmentDetails(String assignmentID, string courseID)
        {
            String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AttendanceConnectionString"].ConnectionString;
            string query = "INSERT INTO ats_mm.assignments (assignment_id,course_id) VALUES('" + assignmentID + "','" + courseID + "')";
            MySqlConnection conn = new MySqlConnection(connectionString);
            MySqlCommand cmd = new MySqlCommand("set net_write_timeout=999999999; set net_read_timeout=999999999", conn); // Setting tiimeout on mysqlServer
           // cmd.ExecuteNonQuery();
            MySqlCommand command = conn.CreateCommand();
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                command.CommandText = String.Format(query);
                int row = command.ExecuteNonQuery();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                string msg = "Fetch Error:";
                msg += ex.Message;
                throw new Exception(msg);
            }
            finally
            {
                conn.Close();
            }
        }

        public void updateAssignmentDetails(String assignmentID, string courseID)
        {
            String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AttendanceConnectionString"].ConnectionString;
            string query = "update ats_mm.assignments set assignment_id='" + assignmentID + "' where course_id= '" + courseID + "'";
            MySqlConnection conn = new MySqlConnection(connectionString);
            MySqlCommand cmd = new MySqlCommand("set net_write_timeout=999999999; set net_read_timeout=999999999", conn); // Setting tiimeout on mysqlServer
           // cmd.ExecuteNonQuery();
            MySqlCommand command = conn.CreateCommand();
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                command.CommandText = String.Format(query);
                int row = command.ExecuteNonQuery();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                string msg = "Fetch Error:";
                msg += ex.Message;
                throw new Exception(msg);
            }
            finally
            {
                conn.Close();
            }
        }

        public string getAssignmentID(String courseID)
        {
            String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AttendanceConnectionString"].ConnectionString;
            string query = "Select assignment_id from ats_mm.assignments where course_id='" + courseID + "'";
            MySqlConnection conn = new MySqlConnection(connectionString);
            MySqlCommand cmd = new MySqlCommand("set net_write_timeout=999999999; set net_read_timeout=999999999", conn); // Setting tiimeout on mysqlServer
           
            MySqlCommand command = conn.CreateCommand();
            MySqlDataReader reader;
            String Assign_ID = "";
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                command.CommandText = String.Format(query);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Assign_ID = reader["assignment_id"] is DBNull ? "" : reader.GetString("assignment_id");
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                string msg = "Fetch Error:";
                msg += ex.Message;
                throw new Exception(msg);
            }
            finally
            {
                conn.Close();
            }
            return Assign_ID;
        }

        public string getCourseID(String callNum)
        {
            String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AttendanceConnectionString"].ConnectionString;
            string query = "Select courseid from ats_mm.courseidreports where callNumber='" + callNum + "'";
            MySqlConnection conn = new MySqlConnection(connectionString);
            MySqlCommand cmd = new MySqlCommand("set net_write_timeout=999999999; set net_read_timeout=999999999", conn); // Setting tiimeout on mysqlServer

            MySqlCommand command = conn.CreateCommand();
            MySqlDataReader reader;
            String Course_ID = "";
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                command.CommandText = String.Format(query);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Course_ID = reader["courseid"] is DBNull ? "" : reader.GetString("courseid");
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                string msg = "Fetch Error:";
                msg += ex.Message;
                throw new Exception(msg);
            }
            finally
            {
                conn.Close();
            }
            return Course_ID;
        }

        //public void UpdateCourses1()
        //{
        //    ApplicationLog.WriteThread2Log("UpdateCourses1 function" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);

        //    #region VariablesDeclaration
        //    String access_token = "7438~uMPlIByL95QVeXFDlTLAXZgMC9QbALc591Bx70XjlIf1x3b7Cd88dj5pAceTElYU";
        //    Dictionary<string, List<int>> emailandattendance;
        //    List<int> studentAttendanceInfo;
        //    //variables to store unique identifiers
        //    Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, double> AttendanceDictionary = new Dictionary<string, double>();
        //    Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
        //    int present = 0;

        //    int courseCount = 0;

        //    #endregion //VariablesDeclaration
        //    //File.WriteAllText(@"C:\Thread1.txt", String.Empty);

        //    try
        //    {

        //        ApplicationLog.WriteThread2Log("Updating Gradebook in this thread started at: " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        ApplicationLog.WriteThread2Log("Total courses updating in this thread are:" + callNumbersCount);

        //        #region Updating all courses
        //        foreach (string callNum in callNumbers)
        //        {
        //            emailandattendance = new Dictionary<string, List<int>>();
        //            //courseCount variable is to log the current number of course which is updating
        //            courseCount++;
        //            ApplicationLog.WriteThread2Log(courseCount + "." + "Course updating: _" + callNum);
        //            //getting enrolled students ina course from our database
        //            using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
        //            {
        //                int count = (from stu in db.students.ToList() // Total Number of Students
        //                             join en in db.enrollments
        //                             on stu.student_id equals en.student_id
        //                             where en.call_number == callNum
        //                             select stu).Count();


        //                ApplicationLog.WriteThread2Log(courseCount + "." + "Total number of students in " + callNum + " are " + count);

        //                int totalClasses = (from at in db.attendances.ToList() // Total Number of classes
        //                                    where at.call_number == callNum
        //                                    select DateTime.Parse(at.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();

        //                ApplicationLog.WriteThread2Log(courseCount + "." + "Total classes for " + callNum + " " + totalClasses);

        //                var studentEnrolls = from stu in db.students // Fetching the student entrollments in the course
        //                                     join en in db.enrollments
        //                                     on stu.student_id equals en.student_id
        //                                     where en.call_number == callNum
        //                                     select new { stu.s_number, stu.student_id };


        //                //getting student attendance details from database and adding it to emailandattendace list
        //                #region studentattendaceInfo
        //                if (count != 0)
        //                {
        //                    foreach (var students in studentEnrolls.ToList())
        //                    {
        //                        studentAttendanceInfo = new List<int>();
        //                        present = (from att in db.attendances.ToList()
        //                                   where att.call_number == callNum && att.student_id == students.student_id
        //                                   select DateTime.Parse(att.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();
        //                        //studentAttendanceInfo.Add(present);
        //                        //missed = totalClasses - present;
        //                        //studentAttendanceInfo.Add(missed);
        //                        // AttendanceDictionary.Add(students.s_number, Convert.ToDouble(present));
        //                        AttendanceDictionary[students.s_number] = (Convert.ToDouble(present));
        //                    }
        //                #endregion

        //                    string courseID = "";
        //                    using (CourseIdReportDataContext Cid = new CourseIdReportDataContext())
        //                    {
        //                        courseID = (from ecourse in Cid.CourseIdReports.ToList()
        //                                    where ecourse.callNumber == callNum
        //                                    select ecourse.courseId).First();
        //                    }

        //                    ApplicationLog.WriteThread2Log(courseCount + "." + "Northwest Online course id for  " + callNum + " is " + courseID);
        //                    int countTotal = totalClasses;
        //                    string callNumber = callNum;

        //                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/students?access_token={2}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token));
        //                    request.Method = "GET";
        //                    request.ContentType = "application/json";
        //                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //                    List<Students> stuList = new List<Students>();
        //                    StreamReader reader = new StreamReader(response.GetResponseStream());
        //                    var objText1 = reader.ReadToEnd();
        //                    reader.Close();
        //                    var result = JToken.Parse(objText1);
        //                    if (result is JArray)
        //                    {
        //                        foreach (JObject o in result.Children<JObject>())
        //                        {
        //                            int id = (int)o["id"];
        //                            String name = o["name"].ToString();
        //                            String sis_user_id = "";
        //                            if (o["sis_user_id"] != null)
        //                            {
        //                                sis_user_id = o["sis_user_id"].ToString();
        //                            }

        //                            String sis_login_id = "";
        //                            if (o["sis_login_id"] != null)
        //                            {
        //                                sis_login_id = o["sis_login_id"].ToString();
        //                            }
        //                            Students stu = new Students(id, name, sis_user_id, sis_login_id);
        //                            stuList.Add(stu);
        //                        }
        //                        // Session["Student_list"] = stuList;
        //                    }
        //                    else
        //                    {
        //                        // Session["Student_list"] = "";
        //                    }
        //                    ApplicationLog.WriteThread2Log("Number of Students " + stuList.Count + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                    ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");

        //                    Double totalPoints = Convert.ToDouble(totalClasses);
        //                    String assignmentID = "";
        //                    String assignID = getAssignmentID(courseID);
        //                    if (assignID == "")
        //                    {
        //                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                        request.Method = "POST";
        //                        request.ContentType = "application/json";
        //                        response = (HttpWebResponse)request.GetResponse();
        //                        reader = new StreamReader(response.GetResponseStream());
        //                        var objAssignment = reader.ReadToEnd();
        //                        reader.Close();
        //                        var resultAssignment = JToken.Parse(objAssignment);
        //                        assignmentID = resultAssignment["id"].ToString();
        //                        storeAssignmentDetails(assignmentID, courseID);
        //                        ApplicationLog.WriteThread2Log("assignmentID " + assignmentID + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                        ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //                    }
        //                    else
        //                    {
        //                        try
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token));
        //                            request.Method = "GET";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            assignmentID = assignID;
        //                            try
        //                            {
        //                                request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}&assignment[points_possible]={4}&assignment[name]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token, totalPoints, "Attendance Swipes"));
        //                                request.Method = "PUT";
        //                                request.ContentType = "application/json";
        //                                response = (HttpWebResponse)request.GetResponse();
        //                                reader = new StreamReader(response.GetResponseStream());
        //                                var objEditAssignment = reader.ReadToEnd();
        //                                reader.Close();
        //                                ApplicationLog.WriteThread2Log("Updated Assignment ID: " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                //  ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('Something went wrong trying connect to Canvas.');window.location='Error.aspx';", false);
        //                                ApplicationLog.WriteThread2Log("Updated Assignment ID: " + ex.Message + "Assignment Data not Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //                            }

        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                            request.Method = "POST";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            var resultAssignment = JToken.Parse(objAssignment);
        //                            assignmentID = resultAssignment["id"].ToString();
        //                            updateAssignmentDetails(assignmentID, courseID);
        //                            ApplicationLog.WriteThread2Log("Assignment Creation if its deleted " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //                        }
        //                    }


        //                    //List<Students> stList = Session["Student_list"] as List<Students>;

        //                    foreach (var student in stuList)
        //                    {
        //                        // Dictionary<String, double> attListDict = Session["AttendanceDictionary"] as Dictionary<String, double>;
        //                        if (AttendanceDictionary.ContainsKey(student.sis_login_id))
        //                        {
        //                            String pointsattained = AttendanceDictionary[student.sis_login_id].ToString();
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}/submissions/{3}?access_token={4}&submission[posted_grade]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignmentID, student.id, access_token, pointsattained));
        //                            request.Method = "PUT";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objSubmission = reader.ReadToEnd();
        //                            reader.Close();
        //                            ApplicationLog.WriteThread2Log("Submission of grades for students, points attained " + pointsattained + "Assignment submitted" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //                        }
        //                    }

        //                }
        //            }
        //            ApplicationLog.WriteThread2Log(courseCount + "." + "Gradebook with attendance information is updated for " + callNum + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //            ApplicationLog.WriteThread2Log("------------------------------------------------------------------------");
        //        }
        //        #endregion //end of updating all courses

        //    }//end of try
        //    catch (Exception ex)
        //    {

        //        ApplicationLog.WriteThread2Log(ex.Message + " " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        throw ex;
        //    }

        //}
        //public void UpdateCourses2()
        //{

        //    ApplicationLog.WriteThread3Log("UpdateCourses2 function" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //    #region VariablesDeclaration
        //    String access_token = "7438~uMPlIByL95QVeXFDlTLAXZgMC9QbALc591Bx70XjlIf1x3b7Cd88dj5pAceTElYU";
        //    Dictionary<string, List<int>> emailandattendance;
        //    List<int> studentAttendanceInfo;
        //    //variables to store unique identifiers
        //    Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, double> AttendanceDictionary = new Dictionary<string, double>();
        //    Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
        //    int present = 0;

        //    int courseCount = 0;

        //    #endregion //VariablesDeclaration
        //    File.WriteAllText(@"C:\Thread1.txt", String.Empty);

        //    try
        //    {

        //        ApplicationLog.WriteThread3Log("Updating Gradebook in this thread started at: " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        ApplicationLog.WriteThread3Log("Total courses updating in this thread are:" + callNumbersCount);

        //        #region Updating all courses
        //        foreach (string callNum in callNumbers)
        //        {
        //            emailandattendance = new Dictionary<string, List<int>>();
        //           // courseCount variable is to log the current number of course which is updating
        //            courseCount++;
        //            ApplicationLog.WriteThread3Log(courseCount + "." + "Course updating: _" + callNum);
        //            //getting enrolled students in a course from our database
        //            using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
        //            {
        //                int count = (from stu in db.students.ToList() // Total Number of Students
        //                             join en in db.enrollments
        //                             on stu.student_id equals en.student_id
        //                             where en.call_number == callNum
        //                             select stu).Count();


        //                ApplicationLog.WriteThread3Log(courseCount + "." + "Total number of students in " + callNum + " are " + count);

        //                int totalClasses = (from at in db.attendances.ToList() // Total Number of classes
        //                                    where at.call_number == callNum
        //                                    select DateTime.Parse(at.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();

        //                ApplicationLog.WriteThread3Log(courseCount + "." + "Total classes for " + callNum + " " + totalClasses);

        //                var studentEnrolls = from stu in db.students // Fetching the student entrollments in the course
        //                                     join en in db.enrollments
        //                                     on stu.student_id equals en.student_id
        //                                     where en.call_number == callNum
        //                                     select new { stu.s_number, stu.student_id };


        //              //  getting student attendance details from database and adding it to emailandattendace list
        //                #region studentattendaceInfo
        //                if (count != 0)
        //                {
        //                    foreach (var students in studentEnrolls.ToList())
        //                    {
        //                        studentAttendanceInfo = new List<int>();
        //                        present = (from att in db.attendances.ToList()
        //                                   where att.call_number == callNum && att.student_id == students.student_id
        //                                   select DateTime.Parse(att.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();
        //                        studentAttendanceInfo.Add(present);
        //                       // missed = totalClasses - present;
        //                       // studentAttendanceInfo.Add(missed);
        //                         AttendanceDictionary.Add(students.s_number, Convert.ToDouble(present));
        //                        AttendanceDictionary[students.s_number] = (Convert.ToDouble(present));
        //                    }
        //                #endregion

        //                    string courseID = "";
        //                    using (CourseIdReportDataContext Cid = new CourseIdReportDataContext())
        //                    {
        //                        courseID = (from ecourse in Cid.CourseIdReports.ToList()
        //                                    where ecourse.callNumber == callNum
        //                                    select ecourse.courseId).First();
        //                    }

        //                    ApplicationLog.WriteThread3Log(courseCount + "." + "Northwest Online course id for  " + callNum + " is " + courseID);
        //                    int countTotal = totalClasses;
        //                    string callNumber = callNum;

        //                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/students?access_token={2}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token));
        //                    request.Method = "GET";
        //                    request.ContentType = "application/json";
        //                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //                    List<Students> stuList = new List<Students>();
        //                    StreamReader reader = new StreamReader(response.GetResponseStream());
        //                    var objText1 = reader.ReadToEnd();
        //                    reader.Close();
        //                    var result = JToken.Parse(objText1);
        //                    if (result is JArray)
        //                    {
        //                        foreach (JObject o in result.Children<JObject>())
        //                        {

        //                            int id = (int)o["id"];
        //                            String name = o["name"].ToString();
        //                            String sis_user_id = "";
        //                            if (o["sis_user_id"] != null)
        //                            {
        //                                sis_user_id = o["sis_user_id"].ToString();
        //                            }

        //                            String sis_login_id = "";
        //                            if (o["sis_login_id"] != null)
        //                            {
        //                                sis_login_id = o["sis_login_id"].ToString();
        //                            }
        //                            Students stu = new Students(id, name, sis_user_id, sis_login_id);
        //                            stuList.Add(stu);

        //                        }
        //                        // Session["Student_list"] = stuList;
        //                    }
        //                    else
        //                    {
        //                        // Session["Student_list"] = "";
        //                    }

        //                    ApplicationLog.WriteThread3Log("Number of Students " + stuList.Count + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                    ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");

        //                    Double totalPoints = Convert.ToDouble(totalClasses);
        //                    String assignmentID = "";
        //                    String assignID = getAssignmentID(courseID);
        //                    if (assignID == "")
        //                    {
        //                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                        request.Method = "POST";
        //                        request.ContentType = "application/json";
        //                        response = (HttpWebResponse)request.GetResponse();
        //                        reader = new StreamReader(response.GetResponseStream());
        //                        var objAssignment = reader.ReadToEnd();
        //                        reader.Close();
        //                        var resultAssignment = JToken.Parse(objAssignment);
        //                        assignmentID = resultAssignment["id"].ToString();
        //                        storeAssignmentDetails(assignmentID, courseID);
        //                        ApplicationLog.WriteThread3Log("assignmentID " + assignmentID + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                        ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //                    }
        //                    else
        //                    {
        //                        try
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token));
        //                            request.Method = "GET";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            assignmentID = assignID;
        //                            try
        //                            {
        //                                request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}&assignment[points_possible]={4}&assignment[name]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token, totalPoints, "Attendance Swipes"));
        //                                request.Method = "PUT";
        //                                request.ContentType = "application/json";
        //                                response = (HttpWebResponse)request.GetResponse();
        //                                reader = new StreamReader(response.GetResponseStream());
        //                                var objEditAssignment = reader.ReadToEnd();
        //                                reader.Close();
        //                                ApplicationLog.WriteThread3Log("Updated Assignment ID: " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                  //ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('Something went wrong trying connect to Canvas.');window.location='Error.aspx';", false);
        //                                ApplicationLog.WriteThread3Log("Updated Assignment ID: " + ex.Message + "Assignment Data not Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //                            }

        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                            request.Method = "POST";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            var resultAssignment = JToken.Parse(objAssignment);
        //                            assignmentID = resultAssignment["id"].ToString();
        //                            updateAssignmentDetails(assignmentID, courseID);
        //                            ApplicationLog.WriteThread3Log("Assignment Creation if its deleted " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //                        }
        //                    }


        //                    //List<Students> stList = Session["Student_list"] as List<Students>;

        //                    foreach (var student in stuList)
        //                    {
        //                        // Dictionary<String, double> attListDict = Session["AttendanceDictionary"] as Dictionary<String, double>;
        //                        if (AttendanceDictionary.ContainsKey(student.sis_login_id))
        //                        {
        //                            String pointsattained = AttendanceDictionary[student.sis_login_id].ToString();
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}/submissions/{3}?access_token={4}&submission[posted_grade]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignmentID, student.id, access_token, pointsattained));
        //                            request.Method = "PUT";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objSubmission = reader.ReadToEnd();
        //                            reader.Close();

        //                            ApplicationLog.WriteThread3Log("Submission of grades for students, points attained " + pointsattained + "Assignment submitted" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //                        }
        //                    }

        //                }
        //            }
        //            ApplicationLog.WriteThread3Log(courseCount + "." + "Gradebook with attendance information is updated for " + callNum + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //            ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
        //        }
        //        #endregion //end of updating all courses

        //    }//end of try
        //    catch (Exception ex)
        //    {

        //        ApplicationLog.WriteThread3Log(ex.Message + " " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        throw ex;
        //    }


        //}
        //public void UpdateCourses3()
        //{

        //    ApplicationLog.WriteThread4Log("UpdateCourses3 function" + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //    #region VariablesDeclaration
        //    String access_token = "7438~uMPlIByL95QVeXFDlTLAXZgMC9QbALc591Bx70XjlIf1x3b7Cd88dj5pAceTElYU";
        //    Dictionary<string, List<int>> emailandattendance;
        //    List<int> studentAttendanceInfo;
        //    //variables to store unique identifiers
        //    Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();
        //    Dictionary<string, double> AttendanceDictionary = new Dictionary<string, double>();
        //    Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
        //    int present = 0;

        //    int courseCount = 0;

        //    #endregion //VariablesDeclaration
        //    //File.WriteAllText(@"C:\Thread1.txt", String.Empty);

        //    try
        //    {

        //        ApplicationLog.WriteThread4Log("Updating Gradebook in this thread started at: " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        ApplicationLog.WriteThread4Log("Total courses updating in this thread are:" + callNumbersCount);

        //        #region Updating all courses
        //        foreach (string callNum in callNumbers)
        //        {
        //            emailandattendance = new Dictionary<string, List<int>>();
        //            //courseCount variable is to log the current number of course which is updating
        //            courseCount++;
        //            ApplicationLog.WriteThread4Log(courseCount + "." + "Course updating: _" + callNum);
        //            //getting enrolled students ina course from our database
        //            using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
        //            {
        //                int count = (from stu in db.students.ToList() // Total Number of Students
        //                             join en in db.enrollments
        //                             on stu.student_id equals en.student_id
        //                             where en.call_number == callNum
        //                             select stu).Count();


        //                ApplicationLog.WriteThread4Log(courseCount + "." + "Total number of students in " + callNum + " are " + count);

        //                int totalClasses = (from at in db.attendances.ToList() // Total Number of classes
        //                                    where at.call_number == callNum
        //                                    select DateTime.Parse(at.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();

        //                ApplicationLog.WriteThread4Log(courseCount + "." + "Total classes for " + callNum + " " + totalClasses);

        //                var studentEnrolls = from stu in db.students // Fetching the student entrollments in the course
        //                                     join en in db.enrollments
        //                                     on stu.student_id equals en.student_id
        //                                     where en.call_number == callNum
        //                                     select new { stu.s_number, stu.student_id };


        //                //getting student attendance details from database and adding it to emailandattendace list
        //                #region studentattendaceInfo
        //                if (count != 0)
        //                {
        //                    foreach (var students in studentEnrolls.ToList())
        //                    {
        //                        studentAttendanceInfo = new List<int>();
        //                        present = (from att in db.attendances.ToList()
        //                                   where att.call_number == callNum && att.student_id == students.student_id
        //                                   select DateTime.Parse(att.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();
        //                        //studentAttendanceInfo.Add(present);
        //                        //missed = totalClasses - present;
        //                        //studentAttendanceInfo.Add(missed);
        //                        //AttendanceDictionary.Add(students.s_number, Convert.ToDouble(present));
        //                        AttendanceDictionary[students.s_number] = (Convert.ToDouble(present));
        //                    }
        //                #endregion

        //                    string courseID = "";
        //                    using (CourseIdReportDataContext Cid = new CourseIdReportDataContext())
        //                    {
        //                        courseID = (from ecourse in Cid.CourseIdReports.ToList()
        //                                    where ecourse.callNumber == callNum
        //                                    select ecourse.courseId).First();
        //                    }

        //                    ApplicationLog.WriteThread4Log(courseCount + "." + "Northwest Online course id for  " + callNum + " is " + courseID);
        //                    int countTotal = totalClasses;
        //                    string callNumber = callNum;

        //                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/students?access_token={2}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token));
        //                    request.Method = "GET";
        //                    request.ContentType = "application/json";
        //                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //                    List<Students> stuList = new List<Students>();
        //                    StreamReader reader = new StreamReader(response.GetResponseStream());
        //                    var objText1 = reader.ReadToEnd();
        //                    reader.Close();
        //                    var result = JToken.Parse(objText1);
        //                    if (result is JArray)
        //                    {
        //                        foreach (JObject o in result.Children<JObject>())
        //                        {
        //                            int id = (int)o["id"];
        //                            String name = o["name"].ToString();
        //                            String sis_user_id = "";
        //                            if (o["sis_user_id"] != null)
        //                            {
        //                                sis_user_id = o["sis_user_id"].ToString();
        //                            }

        //                            String sis_login_id = "";
        //                            if (o["sis_login_id"] != null)
        //                            {
        //                                sis_login_id = o["sis_login_id"].ToString();
        //                            }
        //                            Students stu = new Students(id, name, sis_user_id, sis_login_id);
        //                            stuList.Add(stu);
        //                        }
        //                        // Session["Student_list"] = stuList;
        //                    }
        //                    else
        //                    {
        //                        // Session["Student_list"] = "";
        //                    }

        //                    ApplicationLog.WriteThread4Log("Number of Students " + stuList.Count + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                    ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");

        //                    Double totalPoints = Convert.ToDouble(totalClasses);
        //                    String assignmentID = "";
        //                    String assignID = getAssignmentID(courseID);
        //                    if (assignID == "")
        //                    {
        //                        request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                        request.Method = "POST";
        //                        request.ContentType = "application/json";
        //                        response = (HttpWebResponse)request.GetResponse();
        //                        reader = new StreamReader(response.GetResponseStream());
        //                        var objAssignment = reader.ReadToEnd();
        //                        reader.Close();
        //                        var resultAssignment = JToken.Parse(objAssignment);
        //                        assignmentID = resultAssignment["id"].ToString();
        //                        storeAssignmentDetails(assignmentID, courseID);
        //                        ApplicationLog.WriteThread4Log("assignmentID " + assignmentID + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                        ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //                    }
        //                    else
        //                    {
        //                        try
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token));
        //                            request.Method = "GET";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            assignmentID = assignID;
        //                            try
        //                            {
        //                                request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}?access_token={3}&assignment[points_possible]={4}&assignment[name]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignID, access_token, totalPoints, "Attendance Swipes"));
        //                                request.Method = "PUT";
        //                                request.ContentType = "application/json";
        //                                response = (HttpWebResponse)request.GetResponse();
        //                                reader = new StreamReader(response.GetResponseStream());
        //                                var objEditAssignment = reader.ReadToEnd();
        //                                reader.Close();
        //                                ApplicationLog.WriteThread4Log("Updated Assignment ID: " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                //  ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('Something went wrong trying connect to Canvas.');window.location='Error.aspx';", false);
        //                                ApplicationLog.WriteThread4Log("Updated Assignment ID: " + ex.Message + "Assignment Data not Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                                ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //                            }

        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments?access_token={2}&assignment[name]={3}&assignment[published]=true&assignment[grading_type]=points&assignment[points_possible]={4}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, access_token, "Attendance Swipes", totalPoints));
        //                            request.Method = "POST";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objAssignment = reader.ReadToEnd();
        //                            reader.Close();
        //                            var resultAssignment = JToken.Parse(objAssignment);
        //                            assignmentID = resultAssignment["id"].ToString();
        //                            updateAssignmentDetails(assignmentID, courseID);
        //                            ApplicationLog.WriteThread4Log("Assignment Creation if its deleted " + assignmentID + "Assignment Data Updated" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //                        }
        //                    }


        //                    //List<Students> stList = Session["Student_list"] as List<Students>;

        //                    foreach (var student in stuList)
        //                    {
        //                        // Dictionary<String, double> attListDict = Session["AttendanceDictionary"] as Dictionary<String, double>;
        //                        if (AttendanceDictionary.ContainsKey(student.sis_login_id))
        //                        {
        //                            String pointsattained = AttendanceDictionary[student.sis_login_id].ToString();
        //                            request = (HttpWebRequest)WebRequest.Create(String.Format("{0}{1}/assignments/{2}/submissions/{3}?access_token={4}&submission[posted_grade]={5}", "https://nwmissouri.instructure.com/api/v1/courses/", courseID, assignmentID, student.id, access_token, pointsattained));
        //                            request.Method = "PUT";
        //                            request.ContentType = "application/json";
        //                            response = (HttpWebResponse)request.GetResponse();
        //                            reader = new StreamReader(response.GetResponseStream());
        //                            var objSubmission = reader.ReadToEnd();
        //                            reader.Close();
        //                            ApplicationLog.WriteThread4Log("Submission of grades for students, points attained " + pointsattained + "Assignment submitted" + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //                            ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //                        }
        //                    }

        //                }
        //            }
        //            ApplicationLog.WriteThread4Log(courseCount + "." + "Gradebook with attendance information is updated for " + callNum + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //            ApplicationLog.WriteThread4Log("------------------------------------------------------------------------");
        //        }
        //        #endregion //end of updating all courses

        //    }//end of try
        //    catch (Exception ex)
        //    {

        //        ApplicationLog.WriteThread4Log(ex.Message + " " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
        //        throw ex;
        //    }


        //}
        #region connectinAPI
        public static Response connectingAPI(string httpMethod, Uri url, string body)
        {
            //Sand Box details
            //string appID = "2e4cfa6e-4985-421a-9197-04ea69b44e6c";
            //string consumerKey = "04b11650-b3bb-41d6-91a6-c19936aaf4e5";
            //string secret = "8a72063be6d0409da86a5c239e39fd10";

            //API details of the Northwest Online
            string appID = "2e4cfa6e-4985-421a-9197-04ea69b44e6c";
            string consumerKey = "3929bb45-ea22-4066-b30f-08583c5dbbf1";
            string secret = "6bc16d844619476080c64a0e25dc5184";
            string signatureMethod = "CMAC-AES";
            MemoryStream requestBody = null;
            HttpWebResponse httpWebResponse = null;
            Response response = new Response();
            HttpWebRequest request = null;
            // Set the Nonce and Timestamp parameters
            string nonce = getNonce();
            string timestamp = getTimestamp();

            // Set the request body if making a POST or PUT request
            if (httpMethod == "POST" || httpMethod == "PUT")
            {
                requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
            }


            // Create the OAuth parameter name/value pair dictionary
            Dictionary<string, string> oauthParams = new Dictionary<string, string>
      {
        { "oauth_consumer_key", consumerKey },
        { "application_id", appID },
        { "oauth_signature_method", signatureMethod },
        { "oauth_timestamp", timestamp },
        { "oauth_nonce", nonce },
      };

            // Get the OAuth 1.0 Signature
            string signature = generateSignature(httpMethod, url, oauthParams, requestBody, secret);


            // Add the oauth_signature parameter to the set of OAuth Parameters
            IEnumerable<KeyValuePair<string, string>> allParams = oauthParams.Union(new[]
      {
        new KeyValuePair<string, string>("oauth_signature", signature)
      });

            // Defines a query that produces a set of: keyname="URL-encoded(value)"
            IEnumerable<string> encodedParams = from param in allParams
                                                select param.Key + "=\"" + Uri.EscapeDataString(param.Value) + "\"";

            // Join all encoded parameters with a comma delimiter and convert to a string
            string stringParams = String.Join(",", encodedParams);

            // Build the X-Authorization request header
            string xauth = String.Format("X-Authorization: OAuth realm=\"{0}\",{1}", url, stringParams);


            //Console.WriteLine(xauth);

            WebClient wc = new WebClient();

            try
            {
                // Setup the Request
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpMethod;

                request.Headers.Add(xauth);

                // Set the request body if making a POST or PUT request
                if (httpMethod == "POST" || httpMethod == "PUT")
                {
                    //Console.WriteLine("Testing Body...." + body);
                    byte[] dataArray = Encoding.UTF8.GetBytes(body);

                    request.ContentLength = dataArray.Length;


                    Stream requestStream = request.GetRequestStream();


                    requestStream.Write(dataArray, 0, dataArray.Length);

                    requestStream.Close();
                }

                // Send Request & Get Response
                httpWebResponse = (HttpWebResponse)request.GetResponse();


                response.Method = httpMethod.ToString();
                response.Url = url.ToString();
                response.StatusCode = httpWebResponse.StatusCode;
                response.StatusMessage = httpWebResponse.StatusDescription;
                response.ContentType = httpWebResponse.ContentType;
                response.Headers = request.Headers;

                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response.Content = streamReader.ReadToEnd().Trim();

                    if (string.IsNullOrWhiteSpace(response.Content))
                        response.Content = null;
                }
            }
            catch (WebException e)
            {
                // This exception will be raised if the server didn't return 200 - OK
                // Retrieve more information about the error
                if (e.Response != null)
                {
                    using (HttpWebResponse err = (HttpWebResponse)e.Response)
                    {
                        //Console.WriteLine("The server returned '{0}' with the status code '{1} ({2:d})'.",
                        //err.StatusDescription, err.StatusCode, err.StatusCode);
                    }
                }

            }
            finally
            {
                if (httpWebResponse != null)
                    httpWebResponse.Close();
            }
            return response;
        }
        #endregion
        #region Helper Functions

        /// <summary>
        /// Generates a random nonce.
        /// </summary>
        /// <returns>A unique identifier for the request.</returns>
        private static string getNonce()
        {
            string rtn = Path.GetRandomFileName() + Path.GetRandomFileName() + Path.GetRandomFileName();
            rtn = rtn.Replace(".", "");
            if (rtn.Length > 32)
                return rtn.Substring(0, 32);
            else
                return rtn;
        }
        /// <summary>
        /// Generates an integer representing the number of seconds since the unix epoch using the 
        /// UTC date/time of the request.
        /// </summary>
        /// <returns>A timestamp for the request.</returns>
        private static string getTimestamp()
        {
            return ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        }
        /// <summary>
        /// Generates an OAuth 1.0 signature.
        /// </summary>
        /// <param name="httpMethod">The HTTP method of the request.</param>
        /// <param name="url">The URI of the request.</param>
        /// <param name="oauthParams">The associative set of signable oauth parameters.</param>
        /// <param name="requestBody">A stream containing the serialized message body.</param>
        /// <param name="secret">Alphanumeric string used to validate the identity of the education partner (Private Key).</param>
        /// <returns>A string containing the BASE64-encoded signature digest.</returns>
        private static string generateSignature(
          string httpMethod,
          Uri url,
          IDictionary<string, string> oauthParams,
          Stream requestBody,
          string secret
        )
        {
            // Ensure the HTTP Method is upper-cased
            httpMethod = httpMethod.ToUpper();

            // Construct the URL-encoded OAuth parameter portion of the signature base string
            string encodedParams = normalizeParams(httpMethod, url, oauthParams, requestBody);

            // URL-encode the relative URL
            string encodedUri = Uri.EscapeDataString(url.AbsolutePath);

            // Build the signature base string to be signed with the Consumer Secret
            string baseString = String.Format("{0}&{1}&{2}", httpMethod, encodedUri, encodedParams);

            return generateCmac(secret, baseString);
        }
        /// <summary>
        /// Normalizes all oauth signable parameters and url query parameters according to OAuth 1.0.
        /// </summary>
        /// <param name="httpMethod">The upper-cased HTTP method.</param>
        /// <param name="url">The request URL.</param>
        /// <param name="oauthParams">The associative set of signable oauth parameters.</param>
        /// <param name="requestBody">A stream containing the serialized message body.</param>
        /// <returns>A string containing normalized and encoded OAuth parameters.</returns>
        private static string normalizeParams(
          string httpMethod,
          Uri url,
          IEnumerable<KeyValuePair<string, string>> oauthParams,
          Stream requestBody
        )
        {
            IEnumerable<KeyValuePair<string, string>> kvpParams = oauthParams;

            // Place any Query String parameters into a key value pair using equals ("=") to mark
            // the key/value relationship and join each paramter with an ampersand ("&")
            if (!String.IsNullOrWhiteSpace(url.Query))
            {
                IEnumerable<KeyValuePair<string, string>> queryParams =
                  from p in url.Query.Substring(1).Split('&').AsEnumerable()
                  let key = Uri.EscapeDataString(p.Substring(0, p.IndexOf("=")))
                  let value = Uri.EscapeDataString(p.Substring(p.IndexOf("=") + 1))
                  select new KeyValuePair<string, string>(key, value);

                kvpParams = kvpParams.Union(queryParams);
            }

            // Include the body parameter if dealing with a POST or PUT request
            if (httpMethod == "POST" || httpMethod == "PUT")
            {
                MemoryStream ms = new MemoryStream();
                requestBody.CopyTo(ms);
                byte[] bodyBytes = ms.ToArray();

                string body = Convert.ToBase64String(bodyBytes, Base64FormattingOptions.None);
                body = Uri.EscapeDataString(body);

                kvpParams = kvpParams.Union(new[]
        {
          new KeyValuePair<string, string>("body", Uri.EscapeDataString(body))
        });
            }

            // Sort the parameters in lexicographical order, 1st by Key then by Value; separate with ("=")
            IEnumerable<string> sortedParams =
              from p in kvpParams
              orderby p.Key ascending, p.Value ascending
              select p.Key + "=" + p.Value;

            // Add the ampersand delimiter and then URL-encode the equals ("%3D") and ampersand ("%26")
            string stringParams = String.Join("&", sortedParams);
            string encodedParams = Uri.EscapeDataString(stringParams);

            return encodedParams;
        }
        /// <summary>
        /// Generates a BASE64-encoded CMAC-AES digest.
        /// </summary>
        /// <param name="key">The secret key used to sign the data.</param>
        /// <param name="msg">The data to be signed.</param>
        /// <returns>A CMAC-AES digest.</returns>
        private static string generateCmac(string key, string msg)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] msgBytes = Encoding.UTF8.GetBytes(msg);

            CMac macProvider = new CMac(new AesFastEngine());
            macProvider.Init(new KeyParameter(keyBytes));
            macProvider.Reset();

            macProvider.BlockUpdate(msgBytes, 0, msgBytes.Length);
            byte[] output = new byte[macProvider.GetMacSize()];
            macProvider.DoFinal(output, 0);

            return Convert.ToBase64String(output);
        }
        #endregion
        #region HelpMethods
        public static Dictionary<int, List<int>> UserIdAndGrades(Dictionary<string, List<int>> emailandattendance, Dictionary<string, int> emailanduserId)
        {
            Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
            foreach (string gradebookemail in emailanduserId.Keys)
            {
                foreach (string databaseemail in emailandattendance.Keys)
                {
                    if (gradebookemail.Substring(0, gradebookemail.IndexOf('@')).Equals(databaseemail))
                    {
                        userIdAndgrades.Add(emailanduserId[gradebookemail], emailandattendance[databaseemail]);
                    }
                }
            }
            return userIdAndgrades;
        }
        public static string getCourseId(string httpMethod, Uri url, string body)
        {
            string courseId = "";
            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> courseDetails = data.userGradebookItems;
                foreach (dynamic item in courseDetails)
                {
                    courseId = item.id;
                }

            }
            catch (Exception ex)
            {
            }
            return courseId;
        }
        public static Dictionary<string, string> getGradebookCategory(string httpMethod, Uri url, string body)
        {

            Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();

            // Send Request & Get Response
            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> customGradebookcategories = data.customCategories;
                foreach (dynamic item in customGradebookcategories)
                {
                    customGradebookCategoryAndGuid.Add(item.guid, item.title);
                }

            }
            catch (Exception ex)
            {

            }
            return customGradebookCategoryAndGuid;
        }
        public static Dictionary<string, int> getStudents(string httpMethod, Uri url, string body)
        {
            Dictionary<string, int> emailanduserId = new Dictionary<string, int>();

            try
            {
                Response response = connectingAPI(httpMethod, url, body);

                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> students = data.students;
                foreach (dynamic item in students)
                {
                    emailanduserId.Add(item.emailAddress, (int)item.id);
                }
            }
            catch (Exception ex)
            {

            }

            return emailanduserId;
        }
        public static string getCoureHomeId(string httpMethod, Uri url, string body)
        {
            string courseHomeId = "";


            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> courseItems = data.items;
                foreach (dynamic item in courseItems)
                {
                    string title = item.title;
                    if (title == "Course Home")
                    {
                        courseHomeId = Convert.ToString(item.id);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return courseHomeId;
        }
        public static Dictionary<string, string> getGradebookItems(string httpMethod, Uri url, string body)
        {
            Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();


            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> gradebookItems = data.gradebookItems;
                foreach (dynamic item in gradebookItems)
                {
                    customGradebookItemAndGuid.Add(item.id, item.title);
                }
            }
            catch (Exception ex)
            {

            }

            return customGradebookItemAndGuid;
        }
        public static int getGradeId(string httpMethod, Uri url, string body)
        {
            int gradeId = 0;

            try
            {
                Response response = connectingAPI(httpMethod, url, body);

                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                dynamic grades = data.grade;

                gradeId = (int)grades.id;

            }
            catch (Exception ex)
            {

            }

            return gradeId;
        }
        public bool getAllGradebookItemIdsOfUser(string httpMethod, Uri url, string body)
        {

            bool isAttendedMissedattendedPercentageExists = false;
            int count = 0;
            // Send Request & Get Response
            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> gradebookItems = data.userGradebookItems;
                foreach (dynamic item in gradebookItems)
                {
                    if (item.gradebookItem.title == "Attended")
                    {
                        count++;
                    }
                    if (count == 1)
                    {
                        isAttendedMissedattendedPercentageExists = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return isAttendedMissedattendedPercentageExists;
        }

        public bool getMissedGradebookItemIdOfUser(string httpMethod, Uri url, string body)
        {

            bool isAttendedMissedattendedPercentageExists = false;
            int count = 0;
            // Send Request & Get Response
            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> gradebookItems = data.userGradebookItems;
                foreach (dynamic item in gradebookItems)
                {
                    if (item.gradebookItem.title == "Missed")
                    {
                        count++;
                    }
                    if (count == 1)
                    {
                        isAttendedMissedattendedPercentageExists = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return isAttendedMissedattendedPercentageExists;
        }

        public static bool[] getIncludedAndextraCreditForItem(string httpMethod, Uri url, string body)
        {
            bool[] includedAndExtraCreditFlags = new bool[2];


            try
            {
                Response response = connectingAPI(httpMethod, url, body);
                dynamic data = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                List<dynamic> gradebookItem = data.gradebookItems;
                foreach (dynamic item in gradebookItem)
                {
                    includedAndExtraCreditFlags[0] = item.isIncludedInGrade;
                    includedAndExtraCreditFlags[1] = item.isExtraCredit;
                }

            }
            catch (Exception ex)
            {

            }

            return includedAndExtraCreditFlags;
        }
        #endregion
    }
}
