using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace UniversityFaculties
{
    public static class FacultiesHistoryApi
    {
        [FunctionName("FacultiesHistory")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var cnnString = config.GetConnectionString("SqlConnectionString");
            string name = req.Query["name"];

            var facultyHistoryList = new List<FacultyHistory>();
            try
            {
                using (var connection = new SqlConnection(cnnString))
                {
                    connection.Open();
                    try
                    {
                        facultyHistoryList = connection.Query<FacultyHistory>("SELECT  * FROM [dbo].[FacultiesHistory]").ToList();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var resultString = new StringBuilder();
            if(name != null)
            {
                var selectedFaculty = facultyHistoryList.Where(f => f.Name == name).FirstOrDefault();
                resultString.AppendFormat("|{0,5}|{1,5}|{2,5}|", $"{selectedFaculty.Name}", $"{selectedFaculty.History}", $"{selectedFaculty.YearOfCreation}");
            }
            else
            {
                foreach (var item in facultyHistoryList)
                {
                    resultString.AppendFormat("|{0,5}|{1,5}|{2,5}|", $"{item.Name}", $"{item.History}", $"{item.YearOfCreation}");
                    resultString.Append("\n");
                }
            }

            return (ActionResult)new OkObjectResult(resultString.ToString());      
        }
    }
}
