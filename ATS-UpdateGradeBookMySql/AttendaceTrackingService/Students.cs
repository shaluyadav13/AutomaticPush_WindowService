using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Students
/// </summary>
public class Students
{
	public Students()
	{
	}

    public int id { get; set; }
    public String name { get; set; }
    public String sis_user_id { get; set; }
    public String sis_login_id { get; set; }

    public Students(int a, String b,String c,String d)
    {
        this.id = a;
        this.name = b;
        this.sis_user_id = c;
        this.sis_login_id = d;
    }
}