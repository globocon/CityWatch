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
    public class AddUserDetailsModel : PageModel
    {
        private readonly IUserRepository _userRepository;

        public AddUserDetailsModel(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Email { get; set; }

        public IList<UserDemo> Users { get; set; } // List to hold all users

        [BindProperty]
        public IFormFile ExcelFile { get; set; }

        public string ErrorMessage { get; set; } // To hold error messages

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get all users asynchronously, then order them by Id descending
                Users = (await _userRepository.GetAllUsersAsync())
                    .OrderByDescending(u => u.Id) // Order by Id descending (most recent first)
                    .ToList();
                return Page(); // Return the PageResult, rendering the page
            }
            catch (Exception ex)
            {
                // Log error and display a general message to the user
                ErrorMessage = "An error occurred while loading the users. Please try again later.";
                // Optionally, log the exception for debugging purposes (e.g., via a logging service)
                Console.Error.WriteLine(ex.Message); // Replace with proper logging in production
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                if (action == "upload")
                {
                    if (ExcelFile == null || ExcelFile.Length == 0)
                    {
                        ModelState.AddModelError("ExcelFile", "Please upload a valid Excel file.");
                        return Page();
                    }

                    // Read and upload users from the Excel file
                    var users = await ReadUsersFromExcelAsync(ExcelFile);
                    if (users.Count == 0)
                    {
                        ModelState.AddModelError("ExcelFile", "No valid users found in the Excel file.");
                        return Page();
                    }

                    foreach (var user in users)
                    {
                        try
                        {
                            // Ensure user is added successfully
                            await _userRepository.AddUserAsync(user);
                        }
                        catch (Exception ex)
                        {
                            // Log or handle specific errors related to adding users to the database
                            Console.Error.WriteLine($"Error adding user: {ex.Message}");
                            ModelState.AddModelError("ExcelFile", $"Failed to add user {user.Name}.");
                            return Page();
                        }
                    }

                    TempData["SuccessMessage"] = "Users have been successfully uploaded!";
                    return RedirectToPage(); // Redirect to the page to show success message
                }

                if (action == "submit")
                {
                    ModelState.Remove(nameof(ExcelFile));
                    // Handle the user submission form
                    if (!ModelState.IsValid)
                    {
                        ErrorMessage = "Please make sure all fields are filled out correctly.";
                        return Page();
                    }

                    var user = new UserDemo { Name = Name, Email = Email };
                    try
                    {
                        await _userRepository.AddUserAsync(user);
                        TempData["SuccessMessage"] = "User successfully added!";
                        return RedirectToPage();
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors while adding the user
                        ErrorMessage = "An error occurred while adding the user. Please try again later.";
                        Console.Error.WriteLine($"Error adding user: {ex.Message}");
                        return Page();
                    }
                }

                return Page(); // Default return if no action matched
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions and log them
                ErrorMessage = "An unexpected error occurred. Please try again later.";
                Console.Error.WriteLine(ex.Message); // Replace with proper logging in production
                return Page();
            }
        }

        private async Task<List<UserDemo>> ReadUsersFromExcelAsync(IFormFile file)
        {
            var users = new List<UserDemo>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheets.First();
                        var rowCount = worksheet.RowsUsed().Count();

                        for (int row = 2; row <= rowCount; row++) // Start from row 2 (assuming row 1 is headers)
                        {
                            var name = worksheet.Cell(row, 1).GetValue<string>(); // Name in column 1
                            var email = worksheet.Cell(row, 2).GetValue<string>(); // Email in column 2

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email))
                            {
                                users.Add(new UserDemo
                                {
                                    Name = name,
                                    Email = email
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle errors while reading Excel file
                ErrorMessage = "An error occurred while reading the Excel file. Please try again.";
                Console.Error.WriteLine($"Error reading Excel file: {ex.Message}"); // Replace with logging
            }

            return users;
        }
    }

}
