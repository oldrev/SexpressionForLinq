using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Xunit;
using SexpressionForLinq.Tests.Scenarios;

namespace SexpressionForLinq.Tests
{

    public class SexprParserTests
    {
        [Fact]
        public void EmptyFilterShouldBeTrue()
        {
            var expr = "()";
            var result = EvalulateDirectly(null, expr);
            Assert.True(result);
        }

        [Fact]
        public void CanSplitTokens()
        {
            var expr = "(and,\t true, true)";
            var result = EvalulateDirectly(null, expr);
            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        public void AndOrNotTest(bool a, bool b, bool c)
        {
            var expectedResult = !(a && b) || c;
            var astr = a.ToString().ToLowerInvariant();
            var bstr = b.ToString().ToLowerInvariant();
            var cstr = c.ToString().ToLowerInvariant();
            var expr = $"(or (not (and {astr} {bstr})) {cstr})";
            var result = EvalulateDirectly(null, expr);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void BuiltinOperatorsShouldWorks()
        {
            var emp1 = new Employee()
            {
                Age = 30,
                Birthtime = DateTime.Now,
                Department = null,
                Name = "John"
            };
            Assert.True(EvalulateDirectly(emp1, "(>= Age 30)"));
            Assert.False(EvalulateDirectly(emp1, "(= Name 'Jane')"));
        }


        [Fact]
        public void CanUseComparsionList()
        {
            var emp1 = new Employee()
            {
                Age = 30,
                Birthtime = DateTime.Now,
                Department = null,
                Name = "John"
            };
            Assert.True(EvalulateDirectly(emp1, "(>= Age 30)"));
            Assert.False(EvalulateDirectly(emp1, "(= Name 'Jane')"));
        }

        [Fact]
        public void CanWorkWithComparsionOperators()
        {
            var emp1 = new Employee()
            {
                Age = 30,
                Birthtime = DateTime.Now,
                Department = null,
                Name = "John"
            };
            var expression = "(and (>= Age 30) (= Name 'John'))";
            var result = EvalulateDirectly(emp1, expression);
            Assert.True(result);
        }

        [Fact]
        public void CanUseInAndNotInOperator()
        {
            var emp1 = new Employee { Age = 30 };
            var exp = "(and (in Age [2  30  4  5]) true)";

            var result = EvalulateDirectly(emp1, exp);
            Assert.True(result);

            exp = "(and (!in Age [2  3  4  5]) true)";
            result = EvalulateDirectly(emp1, exp);
            Assert.True(result);
        }

        [Fact]
        public void CanUseDifferentTypeListOperator()
        {
            var emp1 = new Employee { Age = 30 };
            var exp = "(and (in Age [2  30  4  5]) true)";

            var result = EvalulateDirectly(emp1, exp);
            Assert.True(result);

            exp = "(and (!in Age [2  3  4  5]) true)";
            result = EvalulateDirectly(emp1, exp);
            Assert.True(result);
        }

        [Fact]
        public void CanAccessObjectGraph()
        {
            var company = Scenarios.ScenariosMaker.MakeCompanyGraph();
            var employee = company.Departments.First().Employees.First();
            var exp = "(and (= Department.Company.Name 'DunderMiffilin') true)";
            var result = EvalulateDirectly(employee, exp);
            Assert.True(result);
        }

        [Fact]
        public void CanWorkWithSymbols()
        {
            var company = Scenarios.ScenariosMaker.MakeCompanyGraph();
            var employee = company.Departments.First().Employees.First();
            var symbols = new Dictionary<string, object>
            {
                ["department_name"] = "HR"
            };
            var exp = "(= Department.Name $department_name)";
            var result = EvalulateDirectly(employee, exp, symbols);
            Assert.True(result);
        }

        [Fact]
        public void CanUseUserDefinedOperator()
        {
            //用户自定义函数
            var containsFunc = new Func<string, string, bool>((x, y) => x.Contains(y));

            var company = Scenarios.ScenariosMaker.MakeCompanyGraph();
            var employee = company.Departments.First().Employees.First();
            var symbols = new Dictionary<string, object>
            {
                ["contains"] = containsFunc
            };
            var exp = "(contains Department.Name 'H')";
            var result = EvalulateDirectly(employee, exp, symbols);
            Assert.True(result);

        }

        private static bool EvalulateDirectly(object target, string expr, IReadOnlyDictionary<string, object> symbols = null)
        {
            var linqExpr = SexprParser.ParseAndBind(target, expr, symbols);
            var func = Expression.Lambda<Func<bool>>(linqExpr).Compile();
            return func();
        }

    }
}
