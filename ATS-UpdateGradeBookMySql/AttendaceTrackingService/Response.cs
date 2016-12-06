using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;

/// <summary>
/// Summary description for Response
/// </summary>
public class Response
{
    private sbyte[] binaryContent;

    public Response()
    {
    }

    public string Method
    {
        get;
        set;
    }

    public string Url
    {
        get;
        set;
    }

    public string Content
    {
        get;
        set;
    }

    public string ContentType
    {
        get;
        set;
    }

    public HttpStatusCode StatusCode
    {
        get;
        set;
    }

    public string StatusMessage
    {
        get;
        set;
    }

    public sbyte[] BinaryContent
    {
        get;
        set;
    }

    public WebHeaderCollection Headers
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates if an error occurred during last operation
    /// False indicates the operation completed successfully. True indicates the operation was aborted.
    /// </summary>
    public bool Error
    {
        get
        {
            return (int)StatusCode < 200 || (int)StatusCode >= 300; // only if outside 200 range
        }
    }

    /// <summary>
    /// Implements the toString method for use in debugging
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Method: ").Append(Method).Append(", ");
        sb.Append("URL: ").Append(Url).Append(", ");
        sb.Append("Code: ").Append(StatusCode).Append(", ");
        sb.Append("Message: ").Append(StatusMessage).Append(", ");
        sb.Append("Content-Type: ").Append(ContentType).Append(", ");
        sb.Append("Content: ").Append(Content);

        return sb.ToString();
    }
}

public class Attendance
{
    private int _id;

    public int Id
    {
        get { return _id; }
        set { _id = value; }
    }

    private string _student_id;

    public string Student_id
    {
        get { return _student_id; }
        set { _student_id = value; }
    }

    private string _call_number;

    public string Call_number
    {
        get { return _call_number; }
        set { _call_number = value; }
    }

    private string _code;

    public string Code
    {
        get { return _code; }
        set { _code = value; }
    }

    private DateTime _created_at;

    public DateTime Created_at
    {
        get { return _created_at; }
        set { _created_at = value; }
    }

    private DateTime _updated_at;

    public DateTime Updated_at
    {
        get { return _updated_at; }
        set { _updated_at = value; }
    }
}

public class SectionInformation
{
    private String _room;

    public String room {
        get { return _room; }
        set { _room = value; }
    }

    private String _days;

    public String days
    {
        get { return _days; }
        set { _days = value; }
    }

    private TimeSpan _startTime;

    public TimeSpan startTime
    {
        get { return _startTime; }
        set { _startTime = value; }
    }
}