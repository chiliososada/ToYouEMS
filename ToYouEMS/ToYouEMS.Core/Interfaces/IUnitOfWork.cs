using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Profile> Profiles { get; }
        IRepository<Case> Cases { get; }
        IRepository<Question> Questions { get; }
        IRepository<Resume> Resumes { get; }
        IRepository<Attendance> Attendances { get; }
        IRepository<Log> Logs { get; }
        IRepository<Stat> Stats { get; }
        IRepository<QuestionRevision> QuestionRevisions { get; }
       
        Task<int> CompleteAsync();
    }
}
