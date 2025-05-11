using System.Runtime.Serialization;

namespace OA.Core.VModels
{
    public class TimekeepingCreateVModel
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string? CheckInIP { get; set; }
        public string? UserAgent { get; set; }
    }

    public class TimekeepingUpdateVModel : TimekeepingCreateVModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    public class TimekeepingGetByIdVModel : TimekeepingUpdateVModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? AvatarPath { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public bool Status { get; set; }
    }

    public class TimekeepingGetAllVModel : TimekeepingGetByIdVModel
    {
        public string Department { get; set; } = string.Empty;
    }

    public class TimekeepingCreateUserVModel
    {
        public string IPAddress { get; set; } = string.Empty;
    }

    public class CheckOutVModel
    {
        public int Id { get; set; }
    }

    public class FilterTimekeepingGetByDateVModel
    {
        public DateTime Date { get; set; }
    }

    public class FilterTimekeepingForUserVModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsInvalid { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarly { get; set; }
        public bool IsOnTime { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }

    public class FilterTimekeepingVModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
        public string? FullName { get; set; }
        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = true;
        public bool? IsActive { get; set; }
    }

    [DataContract]
    public class TimekeepingExportVModel
    {

    }
}
