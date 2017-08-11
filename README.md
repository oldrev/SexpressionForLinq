# SexpressionForLinq

SexpressionForLinq is a LISP-style S-Expression parser to generate dynamic LINQ Expression Tree.


# Usage

```csharp
var employees = ... // Load from EF Entities or anything else.

// Write a S-Expression to filter
var exp = "(= Department.Company.Name 'Dunder Miffilin Paper Company')";

// Do the LINQ
var filteredEmployees = employees.Where(exp).ToList();

```
