using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Check
/// </summary>
public class Check
{
	public Check()
	{
        //
		// TODO: Add constructor logic here
		//
        public void UpdateCourses2()
        {
            #region VariablesDeclaration

            Dictionary<string, List<int>> emailandattendance;
            List<int> studentAttendanceInfo;
            //variables to store unique identifiers
            Dictionary<string, string> customGradebookCategoryAndGuid = new Dictionary<string, string>();
            Dictionary<string, string> customGradebookItemAndGuid = new Dictionary<string, string>();
            Dictionary<string, int> emailanduserId = new Dictionary<string, int>();
            Dictionary<int, List<int>> userIdAndgrades = new Dictionary<int, List<int>>();
            int gradeId = 0, present = 0, missed = 0;
            double percentage = 0;
            string courseHomeUnitId = "";
            string customCategoryGuidOfAttended = "", gradebookItemGuidOfAttended = "";
            string customCategoryGuidOfMissed = "", gradebookItemGuidOfMissed = "";
            string customCategoryGuidOfPercentage = "", gradebookItemGuidOfPercentage = "";

            bool isAttendedAttendancePercentageExists = false;
            bool isMissedAttendancePercentageExists = false;
            int courseCount = 0;
            bool isAttendedMissedAttendancePercentageExists = false;
            #endregion //VariablesDeclaration
            // File.WriteAllText(@"C:\Thread3.txt", String.Empty);

            try
            {

                ApplicationLog.WriteThread3Log("Updating Gradebook in this thread started at: " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                ApplicationLog.WriteThread3Log("Total courses updating in this thread are:" + callNumbersCount);
                #region Updating all courses
                foreach (string callNum in callNumbers)
                {
                    emailandattendance = new Dictionary<string, List<int>>();
                    //courseCount variable is to log the current number of course which is updating
                    courseCount++;
                    ApplicationLog.WriteThread3Log(courseCount + "." + "Course updating: _" + callNum);
                    //getting enrolled students ina course from our database
                    using (AttendanceTrackingDBDataContext db = new AttendanceTrackingDBDataContext())
                    {
                        int count = (from stu in db.students.ToList()
                                     join en in db.enrollments
                                     on stu.student_id equals en.student_id
                                     where en.call_number == callNum
                                     select stu).Count();


                        ApplicationLog.WriteThread1Log(courseCount + "." + "Total number of students in " + callNum + " are " + count);
                        //getting total number of classes for that course


                        //var totalClasses1 = (from at in db.attendances.ToList()
                        //                     where at.call_number == callNum
                        //                     select DateTime.Parse(at.created_at.ToString("MM-dd-yyyy")));

                        int totalClasses = (from at in db.attendances.ToList()
                                            where at.call_number == callNum
                                            select DateTime.Parse(at.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();

                        ApplicationLog.WriteThread1Log(courseCount + "." + "Total classes for " + callNum + " " + totalClasses);
                        //int count = studentEnrolls2.Count();
                        var studentEnrolls = from stu in db.students
                                             join en in db.enrollments
                                             on stu.student_id equals en.student_id
                                             where en.call_number == callNum
                                             select new { stu.s_number, stu.student_id };


                        //getting student attendance details from database and adding it to emailandattendace list
                        #region studentattendaceInfo
                        if (count != 0)
                        {
                            foreach (var students in studentEnrolls.ToList())
                            {
                                studentAttendanceInfo = new List<int>();
                                present = (from att in db.attendances.ToList()
                                           where att.call_number == callNum && att.student_id == students.student_id
                                           select DateTime.Parse(att.created_at.Date.ToString("MM-dd-yyyy"))).Distinct().Count();
                                studentAttendanceInfo.Add(present);
                                missed = totalClasses - present;
                                studentAttendanceInfo.Add(missed);
                                emailandattendance.Add(students.s_number, studentAttendanceInfo);
                            }
                        #endregion

                            //"8969167";
                            string httpMethod = "GET";
                            string body = "";
                            //to get the Northwest Online courseId for a course
                            Uri url = new Uri(String.Format("{0}ccn={1}", "https://api.learningstudio.com/courses/", callNum));
                            string courseID = "";
                            using (CourseIdReportDataContext Cid = new CourseIdReportDataContext())
                            {
                                courseID = (from ecourse in Cid.CourseIdReports.ToList()
                                            where ecourse.callNumber == callNum
                                            select ecourse.courseId).First();
                            }

                            ApplicationLog.WriteThread3Log(courseCount + "." + "Northwest Online course id for  " + callNum + " is " + courseID);
                            httpMethod = "GET";
                            body = "";

                            //to get all the existing gradebook categories in a course and save them in customGradebookCategoryAndGuid
                            url = new Uri(String.Format("{0}{1}/gradebook/customCategories", "https://api.learningstudio.com/courses/", courseID));
                            customGradebookCategoryAndGuid = getGradebookCategory(httpMethod, url, body);

                            //Gets the list of students registered in course     
                            httpMethod = "GET";
                            url = new Uri(String.Format("{0}{1}/students", "https://api.learningstudio.com/courses/", courseID));
                            body = "";
                            emailanduserId = getStudents(httpMethod, url, body);
                            userIdAndgrades = UserIdAndGrades(emailandattendance, emailanduserId);

                            //to get unit ID for course home gradebook item
                            httpMethod = "GET";
                            url = new Uri(String.Format("{0}{1}/items", "https://api.learningstudio.com/courses/", courseID));
                            courseHomeUnitId = getCoureHomeId(httpMethod, url, body);


                            #region secondTime
                            //creates custom gradebook categories if they are not present in course
                            if (customGradebookCategoryAndGuid.ContainsValue("Attended") && customGradebookCategoryAndGuid.ContainsValue("Missed"))
                            {

                                //check if custom gradebook items are created for custom gradebook categories
                                httpMethod = "GET";
                                url = new Uri(String.Format("{0}{1}/gradebookItems", "https://api.learningstudio.com/courses/", courseID));
                                customGradebookItemAndGuid = getGradebookItems(httpMethod, url, body);

                                //getting the GUID values of custome gradebook items
                                gradebookItemGuidOfAttended = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attended").Key;
                                gradebookItemGuidOfMissed = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "missed").Key;
                                //gradebookItemGuidOfPercentage = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attendance percentage").Key;

                                //getting isIncludedingrade or extra credit values for gradebook item attended
                                httpMethod = "GET";
                                url = new Uri(String.Format("{0}{1}/gradebookItems/{2}", "https://api.learningstudio.com/courses/", courseID, gradebookItemGuidOfAttended));
                                bool[] isIncludedAndExtraCredit = getIncludedAndextraCreditForItem(httpMethod, url, body);

                                //updating the gradebook item attended for the course with possible points
                                httpMethod = "PUT";
                                url = new Uri(String.Format("{0}{1}/gradebook/gradebookItems/{2}", "https://api.learningstudio.com/courses/", courseID, gradebookItemGuidOfAttended));
                                body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": " + isIncludedAndExtraCredit[0].ToString().ToLower() + ",\"isExtraCredit\": " + isIncludedAndExtraCredit[1].ToString().ToLower() + ",\"pointsPossible\": " + totalClasses + "}}";
                                connectingAPI(httpMethod, url, body);

                                //to update the registered students grades for the gradebook items in the course
                                foreach (int userId in userIdAndgrades.Keys)
                                {
                                    percentage = ((((double)userIdAndgrades[userId][0]) / totalClasses) * 100);
                                    httpMethod = "GET";
                                    url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebook/userGradebookItems?expand=grade"));
                                    body = "";
                                    isAttendedAttendancePercentageExists = getAllGradebookItemIdsOfUser(httpMethod, url, body);
                                    if (isAttendedAttendancePercentageExists)
                                    {
                                        //to get the grade Id's of user for attended
                                        httpMethod = "GET";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfAttended + "/grade"));
                                        body = "";
                                        gradeId = getGradeId(httpMethod, url, body);
                                        //to update the grade of the user for item attended
                                        httpMethod = "PUT";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfAttended + "/grade"));
                                        body = "{\"grade\": {\"id\":" + gradeId + ", \"points\": " + userIdAndgrades[userId][0] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                        connectingAPI(httpMethod, url, body);
                                    }
                                    else
                                    {
                                        httpMethod = "POST";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfAttended + "/grade"));
                                        body = "{\"grade\": {\"points\": " + userIdAndgrades[userId][0] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                        connectingAPI(httpMethod, url, body);
                                    }
                                    percentage = ((((double)userIdAndgrades[userId][0]) / totalClasses) * 100);
                                    httpMethod = "GET";
                                    url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebook/userGradebookItems?expand=grade"));
                                    body = "";
                                    isMissedAttendancePercentageExists = getMissedGradebookItemIdOfUser(httpMethod, url, body);
                                    if (isMissedAttendancePercentageExists)
                                    {
                                        //to get the grade Id's of user for missed
                                        httpMethod = "GET";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfMissed + "/grade"));
                                        body = "";
                                        gradeId = getGradeId(httpMethod, url, body);
                                        //to update the grade of the user for item missed
                                        httpMethod = "PUT";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfMissed + "/grade"));
                                        body = "{\"grade\": {\"id\":" + gradeId + ", \"points\": " + userIdAndgrades[userId][1] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                        connectingAPI(httpMethod, url, body);
                                    }
                                    else
                                    {
                                        httpMethod = "POST";
                                        url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfMissed + "/grade"));
                                        body = "{\"grade\": {\"points\": " + userIdAndgrades[userId][1] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                        connectingAPI(httpMethod, url, body);

                                    }
                                }


                            }
                            #endregion
                            #region firstTime
                            else
                            {
                                url = new Uri(String.Format("{0}{1}/gradebook/customCategories", "https://api.learningstudio.com/courses/", courseID));
                                httpMethod = "POST";

                                //creates a custom gradebook category in course with name attended
                                body = "{\"customCategories\": {\"title\":\"Attended\",\"isAssignable\": true}}";
                                connectingAPI(httpMethod, url, body);

                                //creates a custom gradebook category in course with name Missed
                                body = "{\"customCategories\": {\"title\":\"Missed\",\"isAssignable\": true}}";
                                connectingAPI(httpMethod, url, body);

                                //creates a custom gradebook category in course with name Attendance Percentage
                                //body = "{\"customCategories\": {\"title\":\"Attendance Percentage\",\"isAssignable\": true}}";
                                //connectingAPI(httpMethod, url, body);

                                //to get GUID's of created gradebook categories in a course and save them in customGradebookCategoryAndGuid to create gradebook items
                                httpMethod = "GET";
                                body = "";
                                url = new Uri(String.Format("{0}{1}/gradebook/customCategories", "https://api.learningstudio.com/courses/", courseID));
                                customGradebookCategoryAndGuid = getGradebookCategory(httpMethod, url, body);

                                //getting the GUID values of custom gradebook  categories
                                //customCategoryGuidOfPercentage = customGradebookCategoryAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attendance percentage").Key;
                                customCategoryGuidOfMissed = customGradebookCategoryAndGuid.FirstOrDefault(x => x.Value.ToLower() == "missed").Key;
                                customCategoryGuidOfAttended = customGradebookCategoryAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attended").Key;

                                //creates custom gradebook items  in course

                                //Creating a custom gradebook item for gradebook category attended
                                httpMethod = "POST";
                                url = new Uri(String.Format("{0}{1}/gradebook/customCategories/{2}/customItems", "https://api.learningstudio.com/courses/", courseID, customCategoryGuidOfAttended));

                                body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\": " + totalClasses + "}}";
                                connectingAPI(httpMethod, url, body);

                                //Creating a custom gradebook item for gradebook category missed
                                url = new Uri(String.Format("{0}{1}/gradebook/customCategories/{2}/customItems", "https://api.learningstudio.com/courses/", courseID, customCategoryGuidOfMissed));

                                body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\": " + 0 + "}}";
                                connectingAPI(httpMethod, url, body);

                                //Creating a custom gradebook item for gradebook category Attendane Percentage
                                //url = new Uri(String.Format("{0}{1}/gradebook/customCategories/{2}/customItems", "https://api.learningstudio.com/courses/", courseID, customCategoryGuidOfPercentage));

                                //body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\": " + 0 + "}}";
                                //connectingAPI(httpMethod, url, body);

                                //to get the GUID'S of newly created gradebook items
                                httpMethod = "GET";
                                url = new Uri(String.Format("{0}{1}/gradebookItems", "https://api.learningstudio.com/courses/", courseID));
                                customGradebookItemAndGuid = getGradebookItems(httpMethod, url, body);

                                //getting the GUID values of custome gradebook items
                                gradebookItemGuidOfAttended = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attended").Key;
                                gradebookItemGuidOfMissed = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "missed").Key;
                                //gradebookItemGuidOfPercentage = customGradebookItemAndGuid.FirstOrDefault(x => x.Value.ToLower() == "attendance percentage").Key;

                                //updating the gradebook item attended for the course with possible points
                                httpMethod = "PUT";
                                url = new Uri(String.Format("{0}{1}/gradebook/gradebookItems/{2}", "https://api.learningstudio.com/courses/", courseID, gradebookItemGuidOfAttended));
                                body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\": " + totalClasses + "}}";
                                connectingAPI(httpMethod, url, body);

                                url = new Uri(String.Format("{0}{1}/gradebook/gradebookItems/{2}", "https://api.learningstudio.com/courses/", courseID, gradebookItemGuidOfMissed));
                                body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\":0}}";

                                connectingAPI(httpMethod, url, body);

                                //url = new Uri(String.Format("{0}{1}/gradebook/gradebookItems/{2}", "https://api.learningstudio.com/courses/", courseID, gradebookItemGuidOfPercentage));
                                //body = "{\"customItem\": {\"unitID\":" + courseHomeUnitId + ",\"isIncludedInGrade\": false,\"isExtraCredit\": false,\"pointsPossible\": 0}}";

                                //connectingAPI(httpMethod, url, body);

                                foreach (int userId in userIdAndgrades.Keys)
                                {
                                    httpMethod = "POST";
                                    url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfMissed + "/grade"));
                                    body = "{\"grade\": {\"points\": " + userIdAndgrades[userId][1] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                    connectingAPI(httpMethod, url, body);

                                    url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfAttended + "/grade"));
                                    body = "{\"grade\": {\"points\": " + userIdAndgrades[userId][0] + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                    connectingAPI(httpMethod, url, body);

                                    ////calculates attendance percentage of a student
                                    //percentage = ((((double)userIdAndgrades[userId][0]) / totalClasses) * 100);


                                    //url = new Uri(String.Format("https://api.learningstudio.com/users/" + userId + "/courses/" + courseID + "/gradebookItems/" + gradebookItemGuidOfPercentage + "/grade"));
                                    //body = "{\"grade\": {\"points\": " + percentage + ",\"letterGrade\": \"\",\"comments\":\"\", }}";
                                    //connectingAPI(httpMethod, url, body);
                                }

                            }
                            #endregion //end of updating second time
                        }
                    }
                    ApplicationLog.WriteThread3Log(courseCount + "." + "Gradebook with attendance information is updated for " + callNum + " at " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                    ApplicationLog.WriteThread3Log("------------------------------------------------------------------------");
                }
                #endregion //end of updating all courses

            }//end of try
            catch (Exception ex)
            {

                ApplicationLog.WriteThread3Log(ex.Message + " " + DateTime.Now + "_" + DateTime.Now.DayOfWeek);
                throw ex;
            }

        }
	}
}