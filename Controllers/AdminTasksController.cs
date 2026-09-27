using Microsoft.AspNetCore.Mvc;
using CS66_WebApplication1.Filters;
using CS66_WebApplication1.Models;
using CS66_WebApplication1.Services;
using System.Linq;
using System;

namespace CS66_WebApplication1.Controllers
{
    [AdminAuthorize]
    public class AdminTasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminTasksController(ApplicationDbContext context) => _context = context;

        public IActionResult Index()
        {
            var viewModel = new AdminTasksViewModel
            {
                Tasks = _context.Tasks.OrderByDescending(t => t.Id)
                    .Select(t => new TaskItemDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        EmployeeName = _context.Customers
                            .Where(c => c.Customer_Id.ToString() == t.EmployeeId)
                            .Select(c => c.Customer_Name)
                            .FirstOrDefault() ?? "ไม่ระบุ",
                        Priority = t.Priority,
                        Status = t.Status,
                        DueDate = t.DueDate
                    }).ToList(),

                Employees = _context.Customers.Where(c => c.Role == 2).Select(c => new EmployeeSelectDto
                {
                    Id = c.Customer_Id.ToString(),
                    FullName = c.Customer_Name
                }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TaskModel model)
        {
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;
            _context.Tasks.Add(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TaskModel model)
        {
            var task = _context.Tasks.Find(model.Id);
            if (task != null)
            {
                task.Title = model.Title;
                task.Description = model.Description;
                task.EmployeeId = model.EmployeeId;
                task.Priority = model.Priority;
                task.DueDate = model.DueDate;
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var task = _context.Tasks.Find(id);
            if (task != null) { _context.Tasks.Remove(task); _context.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }
    }
}