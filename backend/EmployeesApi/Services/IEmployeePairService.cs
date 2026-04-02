using EmployeesApi.Models;

namespace EmployeesApi.Services;

public interface IEmployeePairService
{
    List<EmployeePairResult> FindLongestCollaboratingPair(List<EmployeeRecord> records);
}
