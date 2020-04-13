using System;
using System.Collections.Generic;
using System.Linq;

namespace SexprForLinq.Demo1
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Employee
    {
        public string Name { get; set; }
        public Department Department { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var deptAdmin = new Department() { Id = 1, Name = "Administration" };
            var deptSales = new Department() { Id = 2, Name = "Sales" };
            var deptCS = new Department() { Id = 2, Name = "CS" };

            var employees = new Employee[]
            {
                  new Employee() { Name="Michael Scott", Department = deptAdmin },
                  new Employee() { Name="Jim Halpert", Department = deptSales },
                  new Employee() { Name="Dwight Schrute", Department = deptSales },
                  new Employee() { Name="Karen Filippelli", Department = deptCS },
            };

            // Your variables
            var symbols = new Dictionary<string, object>()
            {
                { "deptName", "Sales" }
            };

            // Write a S-Expression to filter
            var exp = "(= Department.Name $deptName)";

            // Do the LINQ
            var filteredEmployees = employees.AsQueryable().Where(exp, symbols).ToList();

            foreach(var e in filteredEmployees)
            {
                Console.WriteLine("Filtered employee: {0}", e.Name);
            }


            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Done");
        }
    }
}
