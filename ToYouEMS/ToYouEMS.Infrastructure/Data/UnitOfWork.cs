using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.Entities;
using ToYouEMS.ToYouEMS.Infrastructure.Data.Repositories;

namespace ToYouEMS.ToYouEMS.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IRepository<User> Users { get; private set; }
        public IRepository<Profile> Profiles { get; private set; }
        public IRepository<Case> Cases { get; private set; }
        public IRepository<Question> Questions { get; private set; }
        public IRepository<QuestionRevision> QuestionRevisions { get; private set; }
        public IRepository<Resume> Resumes { get; private set; }
        public IRepository<Attendance> Attendances { get; private set; }
        public IRepository<Log> Logs { get; private set; }
        public IRepository<Stat> Stats { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // 初始化所有仓储
            Users = new Repository<User>(context);
            Profiles = new Repository<Profile>(context);
            Cases = new Repository<Case>(context);
            Questions = new Repository<Question>(context);
            QuestionRevisions = new Repository<QuestionRevision>(context);
            Resumes = new Repository<Resume>(context);
            Attendances = new Repository<Attendance>(context);
            Logs = new Repository<Log>(context);
            Stats = new Repository<Stat>(context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
