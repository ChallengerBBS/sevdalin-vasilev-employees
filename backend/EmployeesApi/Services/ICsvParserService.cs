using EmployeesApi.Models;

namespace EmployeesApi.Services;

public interface ICsvParserService
{
    List<EmployeeRecord> Parse(Stream csvStream);
}

