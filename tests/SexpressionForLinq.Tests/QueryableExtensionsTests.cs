using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using SexpressionForLinq;
using SexpressionForLinq.Tests.Scenarios;

namespace SexpressionForLinq.Tests
{

    public class QueryableExtensionsTests
    {

        [Fact]
        public void WhereShouldWorkWithIEnumerable()
        {
            var company = Scenarios.ScenariosMaker.MakeCompanyGraph();
            var expectedEmployee = company.Departments.First().Employees.First();
            var employees = company.Departments.First().Employees.AsQueryable();

            var exp = "(= Department.Company.Name 'DunderMiffilin')";
            var filteredEmployee = employees.Where(exp).First();

            Assert.Same(expectedEmployee, filteredEmployee);
        }

        [Fact]
        public void UntypedWhereShouldWorkWithIEnumerable()
        {
            var company = Scenarios.ScenariosMaker.MakeCompanyGraph();
            var expectedEmployee = company.Departments.First().Employees.First();
            var employees = company.Departments.First().Employees.AsQueryable();

            var exp = "(= Department.Company.Name 'DunderMiffilin')";
            var filteredEmployees = employees.Where(typeof(Employee), exp).Cast<Employee>();
            var filteredEmployee = filteredEmployees.First();
            Assert.Same(expectedEmployee, filteredEmployee);
        }

    }
}
