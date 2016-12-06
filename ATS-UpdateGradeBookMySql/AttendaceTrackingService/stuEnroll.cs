using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for stuEnroll
/// </summary>
public class stuEnroll
{
	public stuEnroll()
	{

	}
    public String student_id { get; set; }
    public String name { get; set; }
    public string s_number { get; set; }

    public stuEnroll(string id, string name,string s_number)
    {
        this.student_id = id;
        this.name = name;
        this.s_number = s_number;
    }

}