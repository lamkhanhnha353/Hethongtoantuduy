import { createRouter, createWebHistory } from 'vue-router'
import Layout from '@/layouts/Layout.vue'
import DashboardView from '@/views/DashboardView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'

// Training
import CoursesView      from '@/views/training/CoursesView.vue'
import CourseDetailView from '@/views/training/CourseDetailView.vue'
import ClassesView      from '@/views/training/ClassesView.vue'
import ScheduleView     from '@/views/training/ScheduleView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: Layout,
      children: [
        { path: '', name: 'dashboard', component: DashboardView, meta: { title: 'Tổng quan' } },

        // Users
        { path: 'users/students', component: PlaceholderView, meta: { title: 'Phụ huynh & Học sinh' } },
        { path: 'users/teachers', component: PlaceholderView, meta: { title: 'Giáo viên' } },
        { path: 'users/roles', component: PlaceholderView, meta: { title: 'Phân quyền & Bảo mật' } },

        // Training — Courses
        { path: 'training/courses', name: 'courses', component: CoursesView, meta: { title: 'Danh mục Khóa học' } },
        { path: 'training/courses/:id', name: 'course-detail', component: CourseDetailView, meta: { title: 'Chi tiết Khóa học' } },

        // Training — Classes
        { path: 'training/classes', name: 'classes', component: ClassesView, meta: { title: 'Danh sách Lớp học' } },

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

export default router
