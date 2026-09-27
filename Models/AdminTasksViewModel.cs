using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class AdminTasksViewModel
    {
        public List<TaskItemDto> Tasks { get; set; } = new();
        public List<EmployeeSelectDto> Employees { get; set; } = new();
    }

    public class TaskModel
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Pending";
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class TaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime DueDate { get; set; }
    }

    public class EmployeeSelectDto
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
    }
}