import lessonService from "./lesson.service";
import classService from "./class.service";

/**
 * Tính toán danh sách các buổi học (slots) theo lịch tuần,
 * đảm bảo KHÔNG vượt quá totalSessions của cấp độ khóa học.
 *
 * @param {string|Date} startDate - Ngày khai giảng của lớp
 * @param {Array} classSchedules - Danh sách lịch học theo thứ trong tuần (dayOfWeek: 0..6, startTime, endTime)
 * @param {number} totalSessions - Tổng số buổi tối đa của khóa học (CourseLevel.totalSessions)
 * @returns {{ expectedSlots: Array, calculatedEndDate: Date|null, totalGenerated: number }}
 */
export function calculateScheduleSessions(startDate, classSchedules, totalSessions) {
  if (!startDate || !classSchedules || classSchedules.length === 0 || !totalSessions || totalSessions <= 0) {
    return {
      expectedSlots: [],
      calculatedEndDate: null,
      totalGenerated: 0,
    };
  }

  // Sắp xếp lịch theo thứ (0 = CN, 1 = T2, ..., 6 = T7) và giờ bắt đầu
  const sortedSchedules = [...classSchedules].sort((a, b) => {
    if (a.dayOfWeek !== b.dayOfWeek) return a.dayOfWeek - b.dayOfWeek;
    return String(a.startTime).localeCompare(String(b.startTime));
  });

  const slots = [];
  const start = new Date(startDate);
  // Đưa về 00:00:00 theo ngày địa phương
  const curr = new Date(start.getFullYear(), start.getMonth(), start.getDate());

  let count = 0;
  const maxDays = 365 * 2; // Giới hạn an toàn duyệt tối đa 2 năm
  let daysIterated = 0;

  while (count < totalSessions && daysIterated < maxDays) {
    const dow = curr.getDay(); // 0 = CN, 1 = T2, ..., 6 = T7 (khớp hệ thống)
    const matchingSchedules = sortedSchedules.filter((s) => Number(s.dayOfWeek) === dow);

    for (const sch of matchingSchedules) {
      if (count >= totalSessions) break;

      const dateStr = formatDateToYMD(curr);
      const timeStartStr = normalizeTimeString(sch.startTime);
      const timeEndStr = normalizeTimeString(sch.endTime);

      const startDateTimeStr = `${dateStr}T${timeStartStr}:00`;
      const endDateTimeStr = `${dateStr}T${timeEndStr}:00`;

      slots.push({
        sessionIndex: count + 1,
        lessonDate: `${dateStr}T00:00:00`,
        startTime: startDateTimeStr,
        endTime: endDateTimeStr,
        dayOfWeek: dow,
        room: sch.room || "",
        scheduleId: sch.id,
      });

      count++;
    }

    curr.setDate(curr.getDate() + 1);
    daysIterated++;
  }

  let calculatedEndDate = null;
  if (slots.length > 0) {
    // Ngày kết thúc là ngày/giờ của buổi học cuối cùng
    const lastSlot = slots[slots.length - 1];
    calculatedEndDate = new Date(lastSlot.endTime);
  }

  return {
    expectedSlots: slots,
    calculatedEndDate,
    totalGenerated: slots.length,
  };
}

/**
 * Xử lý ngầm đồng bộ:
 * 1. Tính toán lại ngày kết thúc (endDate) dựa trên totalSessions và cập nhật lại thông tin lớp.
 * 2. Gửi các API điều chỉnh lesson (POST tạo mới, PUT cập nhật giờ/ngày, DELETE các buổi dư thừa vượt quá totalSessions).
 */
export async function syncClassLessonsAndEndDate(classId, { classData, courseLevel, schedules, currentLessons = null }) {
  if (!classId) return { success: false, message: "Thiếu ID lớp học" };

  try {
    // Nếu chưa truyền sẵn, tự fetch
    let cls = classData;
    if (!cls) {
      cls = await classService.get(classId);
    }

    const totalSessions = courseLevel?.totalSessions ?? 24;
    const { expectedSlots, calculatedEndDate } = calculateScheduleSessions(
      cls.startDate,
      schedules,
      totalSessions
    );

    // 1. Cập nhật ngày kết thúc của lớp học nếu có sự chênh lệch
    let updatedClass = cls;
    if (calculatedEndDate) {
      const calculatedEndDateIso = calculatedEndDate.toISOString();
      const currentEndIso = cls.endDate ? new Date(cls.endDate).toISOString() : null;

      if (!currentEndIso || Math.abs(new Date(currentEndIso) - calculatedEndDate) > 60000) {
        const payload = {
          courseLevelId: cls.courseLevelId,
          teacherId: cls.teacherId,
          name: cls.name,
          maxCapacity: cls.maxCapacity || 20,
          startDate: new Date(cls.startDate).toISOString(),
          endDate: calculatedEndDateIso,
          status: cls.status,
        };
        await classService.update(classId, payload);
        updatedClass = { ...cls, endDate: calculatedEndDateIso };
      }
    }

    // 2. Lấy danh sách lessons hiện có
    let lessons = currentLessons;
    if (!lessons) {
      const res = await lessonService.getByClass(classId);
      lessons = Array.isArray(res) ? res : (res?.items ?? res?.data ?? []);
    }

    // Sắp xếp lessons hiện có theo ngày
    lessons.sort((a, b) => new Date(a.lessonDate) - new Date(b.lessonDate));

    let createdCount = 0;
    let updatedCount = 0;
    let deletedCount = 0;

    // 3. Điều chỉnh các buổi học khớp với expectedSlots (tối đa totalSessions)
    for (let i = 0; i < expectedSlots.length; i++) {
      const slot = expectedSlots[i];

      if (i < lessons.length) {
        const existing = lessons[i];
        // Không sửa ngày của buổi đã COMPLETED
        if (existing.status !== "COMPLETED") {
          const slotDateStr = slot.lessonDate.split("T")[0];
          const existDateStr = existing.lessonDate ? existing.lessonDate.split("T")[0] : "";

          const slotStart = slot.startTime;
          const existStart = existing.startTime ? new Date(existing.startTime).toISOString() : "";

          // Kiểm tra xem có cần cập nhật ngày/giờ không
          const needsUpdate = slotDateStr !== existDateStr || !existStart.includes(slotDateStr);

          if (needsUpdate) {
            try {
              await lessonService.update(existing.id, {
                classId: Number(classId),
                lessonDate: slot.lessonDate,
                startTime: slot.startTime,
                endTime: slot.endTime,
                status: existing.status || "SCHEDULED",
                isImportan: Boolean(existing.isImportan),
              });
              updatedCount++;
            } catch (err) {
              console.warn(`Không thể cập nhật buổi học #${existing.id}:`, err);
            }
          }
        }
      } else {
        // Chưa có buổi học ở index này -> POST tạo mới
        try {
          await lessonService.create({
            classId: Number(classId),
            lessonDate: slot.lessonDate,
            startTime: slot.startTime,
            endTime: slot.endTime,
            status: "SCHEDULED",
            isImportan: false,
          });
          createdCount++;
        } catch (err) {
          console.warn(`Không thể tạo buổi học thứ ${i + 1}:`, err);
        }
      }
    }

    // 4. Nếu số buổi học hiện có vượt quá totalSessions -> Xóa các buổi dư thừa
    if (lessons.length > expectedSlots.length) {
      for (let i = expectedSlots.length; i < lessons.length; i++) {
        const excess = lessons[i];
        // Chỉ xóa nếu chưa điểm danh và chưa có đơn nghỉ phép
        if (!excess.attendancesCount && !excess.leaveRequestsCount && excess.status !== "COMPLETED") {
          try {
            await lessonService.delete(excess.id);
            deletedCount++;
          } catch (err) {
            console.warn(`Không thể xóa buổi học dư thừa #${excess.id}:`, err);
          }
        }
      }
    }

    return {
      success: true,
      updatedClass,
      calculatedEndDate,
      expectedTotal: expectedSlots.length,
      createdCount,
      updatedCount,
      deletedCount,
    };
  } catch (err) {
    console.error("Lỗi trong quá trình đồng bộ lịch học & buổi học:", err);
    return {
      success: false,
      error: err?.message || "Đồng bộ thất bại",
    };
  }
}

// ─── Utility Helpers ──────────────────────────────────────────────────────────
function formatDateToYMD(d) {
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function normalizeTimeString(t) {
  if (!t) return "17:30";
  const s = String(t).trim();
  const parts = s.split(":");
  const h = parts[0].padStart(2, "0");
  const m = (parts[1] || "00").padStart(2, "0");
  return `${h}:${m}`;
}
