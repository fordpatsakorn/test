using System.Threading.Tasks;

namespace test_app.TestCodeAnalyzer;

public class TaskBlocking
{
    async void Example()
    {
        Task task1 = Task.CompletedTask;
        Task<int> task2 = Task.FromResult(42);
        await task1;
        var result = task2.Result; // Should have warning 
    }
}
