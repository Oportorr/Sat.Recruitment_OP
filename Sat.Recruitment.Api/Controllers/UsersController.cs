
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat.Recruitment.Api.Controllers
{

    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Errors { get; set; }
        public string Message { get; set; }
    }

   
    [ApiController]
    [Route("[controller]")]
    public partial class UsersController : ControllerBase
    {

        private readonly List<User> _users = new List<User>();
        User newUser = new User();

        public UsersController()
        {
        }

        [HttpPost]
        [Route("/CreateUser")]
        public async Task<Result> CreateUser(string name, string email, string address, string phone, string userType, string money)
        {

            StringBuilder errors = new StringBuilder();

            if (ChequeRequiredFields(name, email, address, phone, ref errors))
            {

                if (errors != null && errors.ToString() != "")
                    return new Result()
                    {
                        IsSuccess = false,
                        Errors = errors.ToString()
                    };
            }

            else
            {
                newUser = new User
                {
                    Name = name,
                    Email = email,
                    Address = address,
                    Phone = phone,
                    UserType = userType,
                    Money = 0 // Initialize Money to 0
                };

                if (decimal.TryParse(money, out decimal moneyValue))
                {
                    newUser.Money = moneyValue; // Assign parsed money value to newUser.Money
                }

                switch (newUser.UserType)
                {
                    case "Normal":
                        if (newUser.Money > 100)
                        {
                            var percentage = Convert.ToDecimal(0.12);
                            // If new user is normal and has more than USD100
                            var gif = newUser.Money * percentage;
                            newUser.Money += gif;
                        }
                        else if (newUser.Money > 10)
                        {
                            var percentage = Convert.ToDecimal(0.8);
                            var gif = newUser.Money * percentage;
                            newUser.Money += gif;
                        }
                        break;

                    case "SuperUser":
                        if (newUser.Money > 100)
                        {
                            var percentage = Convert.ToDecimal(0.20);
                            var gif = newUser.Money * percentage;
                            newUser.Money += gif;
                        }
                        break;

                    case "Premium":
                        if (newUser.Money > 100)
                        {
                            newUser.Money *= 2;
                        }
                        break;

                    default:
                        // Handle default case here
                        break;
                }






                try
                {
                    var reader = ReadUsersFromFile();


                    string UnNormalizeEmail = newUser.Email;
                    newUser.Email = NormalizeEmail(UnNormalizeEmail);




                    while (reader.Peek() >= 0)
                    {
                        var line = await reader.ReadLineAsync();

                        var user = new User
                        {
                            Name = line.Split(',')[0].ToString(),
                            Email = line.Split(',')[1].ToString(),
                            Phone = line.Split(',')[2].ToString(),
                            Address = line.Split(',')[3].ToString(),
                            UserType = line.Split(',')[4].ToString(),
                            Money = decimal.Parse(line.Split(',')[5].ToString()),
                        };
                        _users.Add(user);
                    }
                    reader.Close();





                    bool isDuplicated = false;
                    isDuplicated = _users.Any(user => user.Email == newUser.Email ||
                      user.Phone == newUser.Phone || (user.Name == newUser.Name || user.Address == newUser.Address));



                    if (isDuplicated)
                    {
                        Debug.WriteLine("The user is duplicated");

                        return new Result()
                        {
                            IsSuccess = false,
                            Errors = "The user is duplicated",
                            Message = "User Name, Email, Phone and Address need to be unike. Please try again"
                        };

                    }





                }

                catch (IOException ex)
                {
                    // Handle I/O error
                    Debug.WriteLine("Error reading the file: " + ex.Message);
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Debug.WriteLine("Error: " + ex.Message);
                }

            }

            if (SaveUser())

            {
                return new Result()
                {
                    IsSuccess = true,
                    Errors = "",
                    Message = "User Created Succefully"
                };

            }
            else

            {
                return new Result()
                {
                    IsSuccess = false,
                    Errors = "Exceptions Saving the user information",
                    Message = "Make sure the file exists and you have permission to access it"
                };

            }
        }

        [HttpGet]
        [Route("/GetEmailByName")]
        [Produces("application/json")]
        public IActionResult GetEmailByName(string emailName)
        {
            string filePath = Directory.GetCurrentDirectory() + "/Files/Users.txt";

            var lines = System.IO.File.ReadAllLines(filePath);

            // Search for the line that contains the given email name
            var emailLine = lines.FirstOrDefault(line => line.Contains(emailName));

            if (emailLine != null)
            {

                // If the line is found, split it by commas to get the email fields
                var emailFields = emailLine.Split(',');

                var emailObject = new
                {
                    Name = emailFields[0],
                    Email = emailFields[1],
                    Phone = emailFields[2],
                    Address = emailFields[3],
                    Status = emailFields[4],
                    Amount = emailFields[5]
                };

                // Return the email object as a JSON response
                return new JsonResult(emailObject);




            }
            else
            {
                // If the email name is not found, return a not found response
                return NotFound();
            }
        }




        private bool SaveUser()
        {
            bool IsUserCreate = false;

            string filePath = Directory.GetCurrentDirectory() + "/Files/Users.txt";

            try

            {
                // Create or append to the text file
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    // Write the values separated by commas in a single line
                    writer.WriteLineAsync($"{newUser.Name},{newUser.Email},{newUser.Address},{newUser.Phone},{newUser.UserType},{newUser.Money}");
                    IsUserCreate = true;
                }
            }
            catch (IOException ex)
            {
                // Handle I/O error
                Debug.WriteLine("Error writing to the file: " + ex.Message);

            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Debug.WriteLine("Error: " + ex.Message);
            }

            return IsUserCreate;
        }


        private string NormalizeEmail(string email)
        {
            // Convert the email to lowercase
            email = email.ToLowerInvariant();

            // Remove any leading or trailing whitespaces
            email = email.Trim();

            // If the email starts with a dot before the "@" symbol, remove it
            var atIndex = email.IndexOf('@');
            if (atIndex >= 0 && email.Length > atIndex + 1 && email[atIndex - 1] == '.')
            {
                email = email.Remove(atIndex - 1, 1);
            }

            return email;
        }





        private bool ChequeRequiredFields(string name, string email, string address, string phone, ref StringBuilder errors)
        {
            bool IsError;


            IsError = string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(phone);


            if (IsError)
            {
                GetErrorMessages(name, email, address, phone, errors);
            }

            return IsError;
        }

        private static void GetErrorMessages(string name, string email, string address, string phone, StringBuilder errors)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                // Validate if Name is null, empty, or whitespace
                errors.Append("The name is required. ");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                // Validate if Email is null, empty, or whitespace
                errors.Append("The email is required. ");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                // Validate if Address is null, empty, or whitespace
                errors.Append("The address is required. ");
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                // Validate if Phone is null, empty, or whitespace
                errors.Append("The phone is required. ");
            }
        }




    }

    public class User
    {
        [Required(ErrorMessage = "The name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The email is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The address is required.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "The phone is required.")]
        public string Phone { get; set; }


        [Required(ErrorMessage = "The UserType is required.")]
        public string UserType { get; set; }


        [Required(ErrorMessage = "The Money is required.")]
        public decimal Money { get; set; }

    }







}
