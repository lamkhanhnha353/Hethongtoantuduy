import { createRouter, createWebHistory } from "vue-router";
import authService from "@/services/auth.service";
import Layout from "@/layouts/Layout.vue";
import PlaceholderView from "@/views/PlaceholderView.vue";

// Authentication
import LoginView from "@/views/LoginView.vue";

// Admin Views
import DashboardView from "@/views/admin/DashboardView.vue";

// Admin - User Management
import ParentStudentView from "@/views/admin/manage/ParentStudentView.vue";
import TeacherManagementView from "@/views/admin/manage/TeacherManagementView.vue";
import AccountSecurityView from "@/views/admin/manage/AccountSecurityView.vue";

// Admin - Training
import CoursesView from "@/views/admin/training/CoursesView.vue";
import CourseDetailView from "@/views/admin/training/CourseDetailView.vue";
import ClassesView from "@/views/admin/training/ClassesView.vue";
import ClassDetailView from "@/views/admin/training/ClassDetailView.vue";
import ScheduleView from "@/views/admin/training/ScheduleView.vue";

// Admin - Reports
// import AdminAttendanceReportView from "@/views/admin/reports/AttendanceReportView.vue";
// Financial Management
import InvoicesView from "@/views/admin/finance/InvoicesView.vue";
import AdminPayrollView from "@/views/admin/finance/AdminPayrollView.vue";
import AdminNotificationsView from "@/views/admin/communication/AdminNotificationsView.vue";

// Account Module
import AdminProfileView from '@/views/admin/account/AdminProfileView.vue'


// Teacher Views
import TeacherDashboardView from "@/views/teacher/TeacherDashboardView.vue";
import TeacherScheduleView from "@/views/teacher/TeacherScheduleView.vue";
import TeacherClassesView from "@/views/teacher/TeacherClassesView.vue";
import TeacherStudentsView from "@/views/teacher/TeacherStudentsView.vue";
import TeacherAttendanceView from "@/views/teacher/TeacherAttendanceView.vue";
import TeacherQuestionsView from "@/views/teacher/TeacherQuestionsView.vue";
import TeacherAssignmentsView from "@/views/teacher/TeacherAssignmentsView.vue";
import TeacherGradingView from "@/views/teacher/TeacherGradingView.vue";
import TeacherSubmissionDetailView from "@/views/teacher/TeacherSubmissionDetailView.vue";
import TeacherAttendanceReportView from "@/views/teacher/TeacherAttendanceReportView.vue";
import TeacherNoticesView from "@/views/teacher/TeacherNoticesView.vue";
import TeacherChatView from "@/views/teacher/TeacherChatView.vue";
import TeacherProfileView from "@/views/teacher/TeacherProfileView.vue";
import TeacherPasswordView from "@/views/teacher/TeacherPasswordView.vue";
import TeacherPayslipView from "@/views/teacher/TeacherPayslipView.vue";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/login", name: "login", component: LoginView },

    // ─── Teacher Portal Routes ─────────────────────────────────────────────
    {
      path: "/teacher",
      component: Layout,
      meta: { requiresAuth: true, teacherOnly: true },
      children: [
        {
          path: "",
          name: "teacher-dashboard",
          component: TeacherDashboardView,
          meta: { title: "Tổng quan Giáo viên" },
        },
        // Quản lý Lớp học & Thời khóa biểu
        {
          path: "schedule",
          name: "teacher-schedule",
          component: TeacherScheduleView,
          meta: { title: "Thời khóa biểu cá nhân" },
        },
        {
          path: "payslip",
          name: "teacher-payslip",
          component: TeacherPayslipView,
          meta: { title: "Phiếu lương cá nhân" },
        },
        {
          path: "classes",
          name: "teacher-classes",
          component: TeacherClassesView,
          meta: { title: "Lớp học đang phụ trách" },
        },
        {
          path: "students",
          name: "teacher-students",
          component: TeacherStudentsView,
          meta: { title: "Danh sách học sinh theo lớp" },
        },
        {
          path: "attendance",
          name: "teacher-attendance",
          component: TeacherAttendanceView,
          meta: { title: "Điểm danh học viên" },
        },

        // Không gian Học tập & Đánh giá
        {
          path: "questions",
          name: "teacher-questions",
          component: TeacherQuestionsView,
          meta: { title: "Ngân hàng câu hỏi & Bài tập" },
        },
        {
          path: "assignments",
          name: "teacher-assignments",
          component: TeacherAssignmentsView,
          meta: { title: "Giao bài tập về nhà" },
        },
        {
          path: "grading",
          name: "teacher-grading",
          component: TeacherGradingView,
          meta: { title: "Chấm điểm & Nhập nhận xét" },
        },
        {
          path: "grading/submission/:assignmentId/:studentId",
          name: "teacher-submission-detail",
          component: TeacherSubmissionDetailView,
          meta: { title: "Chi tiết bài làm học sinh" },
        },

        // Giao tiếp & Thống kê
        {
          path: "reports/attendance",
          name: "teacher-attendance-report",
          component: TeacherAttendanceReportView,
          meta: { title: "Thống kê chuyên cần lớp học" },
        },
        {
          path: "communication/notices",
          name: "teacher-notices",
          component: TeacherNoticesView,
          meta: { title: "Gửi thông báo & Nhắc nhở" },
        },
        {
          path: "communication/chat",
          name: "teacher-chat",
          component: TeacherChatView,
          meta: { title: "Chat với phụ huynh" },
        },

        // Tài khoản của tôi
        {
          path: "account/profile",
          name: "teacher-profile",
          component: TeacherProfileView,
          meta: { title: "Hồ sơ cá nhân & Chuyên môn" },
        },
        {
          path: "account/password",
          name: "teacher-password",
          component: TeacherPasswordView,
          meta: { title: "Đổi mật khẩu / Mã PIN" },
        },
      ],
    },

    // ─── Admin Portal Routes ───────────────────────────────────────────────
    {
      path: "/",
      component: Layout,
      meta: { requiresAuth: true, adminOnly: true },
      children: [
        {
          path: "",
          name: "dashboard",
          component: DashboardView,
          meta: { title: "Tổng quan" },
        },

        // Users
        {
          path: "users/students",
          component: ParentStudentView,
          meta: { title: "Phụ huynh & Học sinh" },
        },
        {
          path: "users/teachers",
          component: TeacherManagementView,
          meta: { title: "Giáo viên" },
        },
        {
          path: "users/roles",
          component: AccountSecurityView,
          meta: { title: "Tài khoản & Bảo mật" },
        },

        // Training — Courses
        {
          path: "training/courses",
          name: "courses",
          component: CoursesView,
          meta: { title: "Danh mục Khóa học" },
        },
        {
          path: "training/courses/:id",
          name: "course-detail",
          component: CourseDetailView,
          meta: { title: "Chi tiết Khóa học" },
        },

        // Training — Classes
        {
          path: "training/classes",
          name: "classes",
          component: ClassesView,
          meta: { title: "Danh sách Lớp học" },
        },
        {
          path: "training/classes/:id",
          name: "class-detail",
          component: ClassDetailView,
          meta: { title: "Chi tiết Lớp học" },
        },

        // Training — Schedule
        {
          path: "training/schedule",
          name: "schedule",
          component: ScheduleView,
          meta: { title: "Lịch học & Thời khóa biểu" },
        },

        // Learning
        {
          path: "learning/questions",
          component: PlaceholderView,
          meta: { title: "Ngân hàng câu hỏi" },
        },
        {
          path: "learning/gamification",
          component: PlaceholderView,
          meta: { title: "Gamification" },
        },

        // Finance
        {
          path: "finance/invoices",
          component: InvoicesView,
          meta: { title: "Hóa đơn & Biên lai" },
        },
        {
          path: "finance/payroll",
          name: "admin-payroll",
          component: AdminPayrollView,
          meta: { title: "Quản lý Lương Giáo viên" },
        },

        // Communication
        {
          path: "communication/notifications",
          component: AdminNotificationsView,
          meta: { title: "Quản lý Thông báo" },
        },

        // Reports
        {
          path: "reports/revenue",
          component: PlaceholderView,
          meta: { title: "Báo cáo Doanh thu" },
        },
        {
          path: "reports/growth",
          component: PlaceholderView,
          meta: { title: "Tăng trưởng học viên" },
        },
        {
          path: "reports/attendance",
          component: PlaceholderView,
          meta: { title: "Thống kê chuyên cần" },
        },

        // Account
        { path: 'account/profile',
        name: "account-profile",
        component: AdminProfileView, 
        meta: { title: 'Thông tin cá nhân' } }
      ],
    },
  ],
});

router.beforeEach((to) => {
  const isAuthenticated = authService.isAuthenticated();
  const role = authService.getRole()?.toLowerCase();
  const isAdmin = role === "admin";
  const isTeacher = role === "teacher";
  const homeRoute = isAdmin ? "dashboard" : "teacher-dashboard";

  if (to.name !== "login" && !isAuthenticated) {
    return { name: "login", query: { redirect: to.fullPath } };
  }

  if (to.name === "login" && isAuthenticated) {
    return { name: homeRoute };
  }

  if (to.meta.adminOnly && !isAdmin) {
    return { name: "teacher-dashboard" };
  }

  if (to.meta.teacherOnly && !isTeacher && !isAdmin) {
    return { name: "dashboard" };
  }

  if (!to.matched.length) {
    return { name: homeRoute };
  }
});

export default router;
