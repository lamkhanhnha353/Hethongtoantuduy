
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class ClassScheduleService : IClassScheduleService
    {
        private readonly ApplicationDbContext _context;

        public ClassScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClassScheduleResponse>> GetSchedulesByClassIdAsync(int classId)
        {
            return await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new ClassScheduleResponse
                {
                    Id = cs.Id,
                    ClassId = cs.ClassId,
                    ClassName = cs.Class.Name,
                    DayOfWeek = cs.DayOfWeek,
                    StartTime = cs.StartTime,
                    EndTime = cs.EndTime,
                    Room = cs.Room,
                    CreatedAt = cs.CreatedAt,
                    UpdatedAt = cs.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<ClassScheduleResponse> GetScheduleByIdAsync(int id)
        {
            var cs = await _context.ClassSchedules.Include(c => c.Class).FirstOrDefaultAsync(c => c.Id == id);
            if (cs == null) return null;

            return new ClassScheduleResponse
            {
                Id = cs.Id,
                ClassId = cs.ClassId,
                ClassName = cs.Class.Name,
                DayOfWeek = cs.DayOfWeek,
                StartTime = cs.StartTime,
                EndTime = cs.EndTime,
                Room = cs.Room,
                CreatedAt = cs.CreatedAt,
                UpdatedAt = cs.UpdatedAt
            };
        }

        public async Task<ClassScheduleResponse> CreateScheduleAsync(ClassScheduleRequest request)
        {
            // 1. Kiểm tra: Giờ kết thúc phải lớn hơn Giờ bắt đầu
            if (request.EndTime <= request.StartTime)
                throw new Exception("Giờ kết thúc phải muộn hơn Giờ bắt đầu!");

            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId);
            if (!classExists) throw new Exception("Lớp học không tồn tại!");

            // 2. Kiểm tra trùng lịch (Cùng ngày, cùng giờ cho cùng 1 lớp)
            var isDuplicate = await _context.ClassSchedules.AnyAsync(cs =>
                cs.ClassId == request.ClassId &&
                cs.DayOfWeek == request.DayOfWeek &&
                cs.StartTime == request.StartTime);

            if (isDuplicate)
                throw new Exception("Lớp học này đã có lịch học vào khung giờ này rồi!");

            var schedule = new ClassSchedule
            {
                ClassId = request.ClassId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Room = request.Room,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClassSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // 3. thuật toán tự tính Ngày bế giảng (EndDate)
            await CalculateAndSaveClassEndDateAsync(request.ClassId);

            return await GetScheduleByIdAsync(schedule.Id);
        }

        public async Task<bool> UpdateScheduleAsync(int id, ClassScheduleRequest request)
        {
            if (request.EndTime <= request.StartTime)
                throw new Exception("Giờ kết thúc phải muộn hơn Giờ bắt đầu!");

            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return false;

            // Kiểm tra trùng lịch với id khác
            var isDuplicate = await _context.ClassSchedules.AnyAsync(cs =>
                cs.Id != id &&
                cs.ClassId == request.ClassId &&
                cs.DayOfWeek == request.DayOfWeek &&
                cs.StartTime == request.StartTime);

            if (isDuplicate) throw new Exception("Lớp học này đã có lịch học vào khung giờ này rồi!");

            schedule.ClassId = request.ClassId;
            schedule.DayOfWeek = request.DayOfWeek;
            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;
            schedule.Room = request.Room;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await CalculateAndSaveClassEndDateAsync(request.ClassId); // Tính lại lịch
            return true;
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.ClassSchedules.FindAsync(id);
            if (schedule == null) return false;

            int classId = schedule.ClassId;
            _context.ClassSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            await CalculateAndSaveClassEndDateAsync(classId); // Tính lại lịch (có thể thành null nếu xóa hết)
            return true;
        }

       
        // THUẬT TOÁN TÍNH TỰ ĐỘNG END_DATE CỦA LỚP
      
        private async Task CalculateAndSaveClassEndDateAsync(int classId)
        {
            var targetClass = await _context.Classes
                .Include(c => c.CourseLevel)
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null || targetClass.CourseLevel == null) return;

            var totalSessions = targetClass.CourseLevel.TotalSessions;
            var schedules = targetClass.ClassSchedules.ToList();

            // Nếu không có lịch học nào hoặc số buổi = 0 -> không tính được
            if (totalSessions <= 0 || !schedules.Any())
            {
                targetClass.EndDate = null;
                await _context.SaveChangesAsync();
                return;
            }

            // Lấy ra danh sách các Thứ có lịch (ví dụ: Thứ 3 và Thứ 5 -> [2, 4])
            var activeDaysOfWeek = schedules.Select(s => s.DayOfWeek).Distinct().ToList();

            var currentDate = targetClass.StartDate;
            int count = 0;

            // Duyệt từ ngày bắt đầu, cộng dồn lịch cho đến khi đủ số buổi
            while (count < totalSessions)
            {
                // DayOfWeek của C# tự trả về số từ 0 (CN) đến 6 (T7)
                if (activeDaysOfWeek.Contains((int)currentDate.DayOfWeek))
                {
                    count++;
                }

                // Tránh cộng lố ngày ở buổi học cuối cùng
                if (count < totalSessions)
                {
                    currentDate = currentDate.AddDays(1);
                }
            }

            // Cập nhật lại ngày kết thúc cho Lớp học
            targetClass.EndDate = currentDate;
            await _context.SaveChangesAsync();
        }
    }
}