﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AttendaceTrackingService
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[global::System.Data.Linq.Mapping.DatabaseAttribute(Name="Attendance")]
	public partial class CourseIdReportDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    #endregion
		
		public CourseIdReportDataContext() : 
				base(global::AttendaceTrackingService.Properties.Settings.Default.AttendanceConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public CourseIdReportDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public CourseIdReportDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public CourseIdReportDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public CourseIdReportDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<CourseIdReport> CourseIdReports
		{
			get
			{
				return this.GetTable<CourseIdReport>();
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.CourseIdReports")]
	public partial class CourseIdReport
	{
		
		private string _callNumber;
		
		private string _courseId;
		
		public CourseIdReport()
		{
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_callNumber", DbType="NVarChar(50) NOT NULL", CanBeNull=false)]
		public string callNumber
		{
			get
			{
				return this._callNumber;
			}
			set
			{
				if ((this._callNumber != value))
				{
					this._callNumber = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_courseId", DbType="NVarChar(50) NOT NULL", CanBeNull=false)]
		public string courseId
		{
			get
			{
				return this._courseId;
			}
			set
			{
				if ((this._courseId != value))
				{
					this._courseId = value;
				}
			}
		}
	}
}
#pragma warning restore 1591