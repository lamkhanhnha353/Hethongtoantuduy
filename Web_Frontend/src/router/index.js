import { createRouter, createWebHistory } from 'vue-router'
import authService from '@/services/auth.service'
import Layout from '@/layouts/Layout.vue'
import DashboardView from '@/views/admin/DashboardView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import LoginView from '@/views/LoginView.vue'
import TeacherDashboardView from '@/views/teacher/TeacherDashboardView.vue'

// User Magement
import ParentStudentView from '@/views/admin/manage/ParentStudentView.vue'
import TeacherManagementView from '@/views/admin/manage/TeacherManagementView.vue'


// Training
import CoursesView      from '@/views/admin/training/CoursesView.vue'
import CourseDetailView from '@/views/admin/training/CourseDetailView.vue'
import ClassesView      from '@/views/admin/training/ClassesView.vue'
import ClassDetailView  from '@/views/admin/training/ClassDetailView.vue'
import ScheduleView     from '@/views/admin/training/ScheduleView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    {
      path: '/teacher',
      name: 'teacher-dashboard',
      component: TeacherDashboardView,
      meta: { requiresAuth: true, teacherOnly: true },
    },
    {
      path: '/',
      component: Layout,
      meta: { requiresAuth: true, adminOnly: true },
      children: [
        { path: '', name: 'dashboard', component: DashboardView, meta: { title: 'Tổng quan' } },

        // Users
        { path: 'users/students', component: ParentStudentView, meta: { title: 'Phụ huynh & Học sinh' } },
        { path: 'users/teachers', component: TeacherManagementView, meta: { title: 'Giáo viên' } },
        { path: 'users/roles', component: PlaceholderView, meta: { title: 'Phân quyền & Bảo mật' } },

        // Training — Courses
        { path: 'training/courses', name: 'courses', component: CoursesView, meta: { title: 'Danh mục Khóa học' } },
        { path: 'training/courses/:id', name: 'course-detail', component: CourseDetailView, meta: { title: 'Chi tiết Khóa học' } },

        // Training — Classes
        { path: 'training/classes', name: 'classes', component: ClassesView, meta: { title: 'Danh sách Lớp học' } },
        { path: 'training/classes/:id', name: 'class-detail', component: ClassDetailView, meta: { title: 'Chi tiết Lớp học' } },

        // Training — Schedule
        { path: 'training/schedule', name: 'schedule', component: ScheduleView, meta: { title: 'Lịch học & Thời khóa biểu' } },

        // Learning
        { path: 'learning/questions', component: PlaceholderView, meta: { title: 'Ngân hàng câu hỏi' } },
        { path: 'learning/gamification', component: PlaceholderView, meta: { title: 'Gamification' } },

        // Finance
        { path: 'finance/invoices', component: PlaceholderView, meta: { title: 'Hóa đơn & Biên lai' } },
        { path: 'finance/transactions', component: PlaceholderView, meta: { title: 'Lịch sử giao dịch' } },

        // Communication
        { path: 'communication/notifications', component: PlaceholderView, meta: { title: 'Quản lý Thông báo' } },

        // Reports
        { path: 'reports/revenue', component: PlaceholderView, meta: { title: 'Báo cáo Doanh thu' } },
        { path: 'reports/growth', component: PlaceholderView, meta: { title: 'Tăng trưởng học viên' } },
        { path: 'reports/attendance', component: PlaceholderView, meta: { title: 'Thống kê chuyên cần' } },

        // Account
        { path: 'account/profile', component: PlaceholderView, meta: { title: 'Thông tin cá nhân' } },
        { path: 'account/password', component: PlaceholderView, meta: { title: 'Đổi mật khẩu / PIN' } },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const isAuthenticated = authService.isAuthenticated()
  const role = authService.getRole()?.toLowerCase()
  const isAdmin = role === 'admin'
  const isTeacher = role === 'teacher'
  const homeRoute = isAdmin ? 'dashboard' : 'teacher-dashboard'

  if (to.name !== 'login' && !isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'login' && isAuthenticated) {
    return { name: homeRoute }
  }

  if (to.meta.adminOnly && !isAdmin) {
    return { name: 'teacher-dashboard' }
  }

  if (to.meta.teacherOnly && !isTeacher) {
    return { name: 'dashboard' }
  }

  if (!to.matched.length) {
    return { name: homeRoute }
  }
})

export default router
