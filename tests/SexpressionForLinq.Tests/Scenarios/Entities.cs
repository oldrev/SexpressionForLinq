using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SexpressionForLinq.Tests.Scenarios
{
    public class Company
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ICollection<Department> Departments { get; set; }
    }

    public class Department
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual Company Company { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }

    public class Employee
    {
        public virtual int Id { get; set; }
        public virtual Company Company { get; set; }
        public virtual Department Department { get; set; }
        public virtual string Name { get; set; }
        public virtual int Age { get; set; }
        public virtual DateTime Birthtime { get; set; }
    }

    public static class ScenariosMaker
    {
        public static readonly Company TestObjectGraph = MakeCompanyGraph();

        public static Company MakeCompanyGraph()
        {
            var company = new Company
            {
                Name = "DunderMiffilin",
            };

            var departments = new Department[]
            {
                new Department{ Name = "HR", Company = company},
                new Department { Name = "Accounting", Company = company}
            };
            company.Departments = departments;

            var employees = new Employee[]
            {
                new Employee{ Name = "Pam", Department = departments[0], Age=37, Company = company},
                new Employee{ Name = "Angela", Department = departments[1], Age=40, Company = company},
            };
            departments[0].Employees = new[] { employees[0] };
            departments[1].Employees = new[] { employees[1] };

            return company;

        }
    }

}
