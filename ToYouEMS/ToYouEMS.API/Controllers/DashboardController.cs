using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToYouEMS.ToYouEMS.Core.Interfaces;
using ToYouEMS.ToYouEMS.Core.Models.Entities;

namespace ToYouEMS.ToYouEMS.API.Controllers
{
   
        [Authorize]
        [Route("api/[controller]")]
        [ApiController]
        public class DashboardController : ControllerBase
        {
            private readonly IUnitOfWork _unitOfWork;

            public DashboardController(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            [HttpGet]
            public async Task<IActionResult> GetDashboardData()
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value);
                var userType = User.FindFirst("userType")?.Value;

                // 获取面试问题统计
                var totalQuestions = await _unitOfWork.Questions.Find(q => true).CountAsync();
                var personalQuestions = await _unitOfWork.Questions.Find(q => q.UserID == userId).CountAsync();
                var companyQuestions = await _unitOfWork.Questions.Find(q => q.Source == QuestionSource.Company).CountAsync();

                // 获取当前活跃的案件数
                var activeCases = await _unitOfWork.Cases.Find(c => c.Status == CaseStatus.Active).CountAsync();

                // 获取简历统计
                var pendingResumes = 0;
                var approvedResumes = 0;
                var rejectedResumes = 0;

                if (userType == UserType.Teacher.ToString() || userType == UserType.Admin.ToString())
                {
                    pendingResumes = await _unitOfWork.Resumes.Find(r => r.Status == ResumeStatus.Pending).CountAsync();
                    approvedResumes = await _unitOfWork.Resumes.Find(r => r.Status == ResumeStatus.Approved).CountAsync();
                    rejectedResumes = await _unitOfWork.Resumes.Find(r => r.Status == ResumeStatus.Rejected).CountAsync();
                }
                else // Student
                {
                    pendingResumes = await _unitOfWork.Resumes.Find(r => r.UserID == userId && r.Status == ResumeStatus.Pending).CountAsync();
                    approvedResumes = await _unitOfWork.Resumes.Find(r => r.UserID == userId && r.Status == ResumeStatus.Approved).CountAsync();
                    rejectedResumes = await _unitOfWork.Resumes.Find(r => r.UserID == userId && r.Status == ResumeStatus.Rejected).CountAsync();
                }

                // 获取用户统计（仅对管理员可见）
                var userStats = new object();
                if (userType == UserType.Admin.ToString())
                {
                    var totalUsers = await _unitOfWork.Users.Find(u => true).CountAsync();
                    var activeUsers = await _unitOfWork.Users.Find(u => u.IsActive).CountAsync();
                    var studentCount = await _unitOfWork.Users.Find(u => u.UserType == UserType.Student).CountAsync();
                    var teacherCount = await _unitOfWork.Users.Find(u => u.UserType == UserType.Teacher).CountAsync();
                    var adminCount = await _unitOfWork.Users.Find(u => u.UserType == UserType.Admin).CountAsync();

                    userStats = new
                    {
                        totalUsers,
                        activeUsers,
                        studentCount,
                        teacherCount,
                        adminCount
                    };
                }

                return Ok(new
                {
                    questionStats = new
                    {
                        totalQuestions,
                        personalQuestions,
                        companyQuestions
                    },
                    caseStats = new
                    {
                        activeCases
                    },
                    resumeStats = new
                    {
                        pendingResumes,
                        approvedResumes,
                        rejectedResumes,
                        totalResumes = pendingResumes + approvedResumes + rejectedResumes
                    },
                    userStats
                });
            }
        }
    }

