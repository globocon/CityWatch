using ClosedXML.Excel;
using CityWatch.Data.Models;
using FortescueWebApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;


namespace FortescueWebApp.Pages.Admin
{
    public class AddWorkOrderModel : PageModel
    {
        private readonly IWorkOrderRepository _workOrderRepository;

        public AddWorkOrderModel(IWorkOrderRepository workOrderRepository)
        {
            _workOrderRepository = workOrderRepository;
        }

        [BindProperty]
        public WorkOrder WorkOrder { get; set; } = new WorkOrder();

        public List<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

        public string ErrorMessage { get; set; }

        [BindProperty]
        public IFormFile ExcelFile { get; set; }

        // On GET, retrieve all work orders and display the page
        public async Task<IActionResult> OnGetAsync()
        {
            var workOrders = await _workOrderRepository.GetAllAsync();
            WorkOrders = workOrders.ToList();
            return Page();
        }

        // On POST, handle the file upload or form submission
        public async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                if (action == "upload")
                {
                    // Validate the uploaded file
                    if (ExcelFile == null || ExcelFile.Length == 0)
                    {
                        ModelState.AddModelError("ExcelFile", "Please upload a valid Excel file.");
                        return Page();
                    }

                    // Process the uploaded Excel file and read the work orders
                    var workOrders = await ReadExcelFileAsync(ExcelFile);
                    if (!workOrders.Any())
                    {
                        ModelState.AddModelError("ExcelFile", "No valid work orders found in the Excel file.");
                        return Page();
                    }

                    // Attempt to add each work order to the database
                    foreach (var workOrder in workOrders)
                    {
                        try
                        {
                            // Add each work order to the repository
                            await _workOrderRepository.AddAsync(workOrder);
                        }
                        catch (Exception ex)
                        {
                            // Handle specific error for a failed work order
                            ModelState.AddModelError("ExcelFile", $"Failed to add work order {workOrder.WorkOrderNumber}: {ex.Message}");
                            return Page();
                        }
                    }

                    TempData["SuccessMessage"] = "Work orders have been successfully uploaded!";
                    WorkOrder = new WorkOrder(); // Reset the WorkOrder property
                    return RedirectToPage(); // Redirect after successful upload
                                             
                   
                }

                if (action == "submit")
                {
                    ModelState.Remove(nameof(ExcelFile));
                    // Validate the form fields
                    if (!ModelState.IsValid)
                    {
                        ErrorMessage = "Please fill out all required fields correctly.";
                        var workOrders = await _workOrderRepository.GetAllAsync();
                        WorkOrders = workOrders.ToList();
                        return Page();
                    }

                    // Add the submitted work order to the repository
                    await _workOrderRepository.AddAsync(WorkOrder);
                    TempData["SuccessMessage"] = "Work Order successfully added!";
                    return RedirectToPage(); // Redirect after successful upload
                }

                // Refresh the work orders list after adding a new work order
                var allWorkOrders = await _workOrderRepository.GetAllAsync();
                WorkOrders = allWorkOrders.ToList(); // Make sure the list is updated

                return Page(); // Return the page if no action matches
            }
            catch (Exception ex)
            {
                // General error handler
                ErrorMessage = $"An error occurred while processing the work orders: {ex.Message}";
                var workOrders = await _workOrderRepository.GetAllAsync();
                WorkOrders = workOrders.ToList();
                return Page();
            }
        }

        // Helper function to read and process the Excel file and return a list of WorkOrders
        private async Task<List<WorkOrder>> ReadExcelFileAsync(IFormFile excelFile)
        {
            var workOrders = new List<WorkOrder>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1); // Assume data is in the first worksheet
                    var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                    foreach (var row in rows)
                    {
                        var workOrder = new WorkOrder
                        {
                            WorkOrderNumber = row.Cell(1).GetValue<string>(),
                            EngLine = row.Cell(2).GetValue<string>(),
                            EngLeg = row.Cell(3).GetValue<string>(),
                            EngStart = row.Cell(4).GetValue<decimal>(),
                            EngEnd = row.Cell(5).GetValue<decimal>(),
                            EngDescription = row.Cell(6).GetValue<string>(),
                            CreatedAt = DateTime.UtcNow
                        };

                        workOrders.Add(workOrder);
                    }
                }
            }

            return workOrders;
        }
    }





}
