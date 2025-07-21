using System.Linq.Expressions;

namespace FlowAccount.Core.Attributes
{
    public class NotOptionalAttribute : Attribute
    {
    }
}
namespace Flowaccount.Data
{
    using FlowAccount.Core.Attributes;
    public interface IDataHandler<T> where T : class
    {
        IList<T> FindList(Expression<Func<T, bool>> where);
    }

    public class Model
    {
        [NotOptional]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DataHandler<T> : IDataHandler<T> where T : class
    {
        public IList<T> FindList(Expression<Func<T, bool>> where)
        {

            return new List<T>().Where(where.Compile()).ToList();
        }
    }

    public class SampleService
    {
        private readonly IDataHandler<Model> _mockDataHandler = new DataHandler<Model>();
        public void SampleMethod()
        {
            _mockDataHandler.FindList(m => m.Id == 1 && m.Name == "Test");
        }
    }
}
