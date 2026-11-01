<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header & Title ──────────────────────────────────────────────── -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>Điểm danh học viên</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Điểm danh & Quản lý chuyên cần</h1>
        <p class="text-xs md:text-sm text-gray-500 mt-0.5">
          Theo dõi chuyên cần từng buổi học, xem đơn xin nghỉ phép và tra cứu lịch sử học sinh
        </p>
      </div>

      <!-- Action buttons -->
      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="reloadCurrentLesson"
          :disabled="isLoading || !selectedLessonId"
          class="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl border border-orange-200 bg-white hover:bg-orange-50 text-orange-800 text-xs font-semibold shadow-xs transition-colors cursor-pointer disabled:opacity-50"
          title="Tải lại dữ liệu buổi học"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          @click="handleSaveBatchAttendance"
          :disabled="isSavingBatch || !selectedLessonId || students.length === 0"
          class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-sm transition-all cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon v-if="isSavingBatch" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>Lưu tất cả điểm danh (Batch)</span>
        </button>
      </div>
    </div>

    <!-- ─── Notification / Feedback Alerts ──────────────────────────────── -->
    <transition name="slide-fade">
      <div
        v-if="feedback.message"
        :class="[
          'flex items-center justify-between p-4 rounded-2xl shadow-xs border text-sm',
          feedback.type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
            : 'bg-rose-50 border-rose-200 text-rose-800'
        ]"
      >
        <div class="flex items-center gap-3">
          <CheckCircleIcon v-if="feedback.type === 'success'" class="w-5 h-5 text-emerald-600 flex-shrink-0" />
          <ExclamationCircleIcon v-else class="w-5 h-5 text-rose-600 flex-shrink-0" />
          <span class="font-medium">{{ feedback.message }}</span>
        </div>
        <button
          type="button"
          @click="feedback.message = ''"
          class="text-gray-400 hover:text-gray-600 p-1 rounded-lg transition-colors cursor-pointer"
        >
          <XMarkIcon class="w-4 h-4" />
        </button>
      </div>
    </transition>

    <!-- ─── Session & Class Selector Card ───────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-5 shadow-sm space-y-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Select Class -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
            1. Chọn lớp học <span class="text-rose-500">*</span>
          </label>
          <div class="relative">
            <select
              v-model="selectedClassId"
              @change="onClassChange"
              class="w-full text-xs font-semibold bg-orange-50/50 border border-orange-200 text-gray-900 rounded-xl px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 transition-all"
            >
              <option :value="null" disabled>-- Chọn lớp học --</option>
              <option v-for="cls in classes" :key="cls.id" :value="cls.id">
                {{ cls.name }} {{ cls.room ? `(${cls.room})` : '' }}
              </option>
            </select>
          </div>
        </div>

        <!-- Select Lesson -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
            2. Chọn buổi học <span class="text-rose-500">*</span>
          </label>
          <div class="relative">
            <select
              v-model="selectedLessonId"
              @change="onLessonChange"
              :disabled="isLoadingLessons || lessons.length === 0"
              class="w-full text-xs font-semibold bg-orange-50/50 border border-orange-200 text-gray-900 rounded-xl px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 transition-all disabled:opacity-50"
            >
              <option :value="null" disabled>
                {{ isLoadingLessons ? 'Đang tải buổi học...' : (lessons.length === 0 ? '-- Lớp chưa có buổi học --' : '-- Chọn buổi học --') }}
              </option>
              <option v-for="les in lessons" :key="les.id" :value="les.id">
                {{ formatDate(les.lessonDate) }} &bull; {{ les.title || `Buổi #${les.id}` }}
              </option>
            </select>
          </div>
        </div>

        <!-- Quick Lesson ID Search / Direct Jump -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
            Tìm trực tiếp theo ID buổi
          </label>
          <div class="flex items-center gap-1.5">
            <input
              type="number"
              v-model.number="directLessonIdInput"
              placeholder="VD: 15"
              @keyup.enter="jumpToDirectLesson"
              class="w-full text-xs font-semibold bg-white border border-gray-200 text-gray-800 rounded-xl px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
            <button
              type="button"
              @click="jumpToDirectLesson"
              class="px-3.5 py-2.5 rounded-xl bg-orange-100 hover:bg-orange-200 text-orange-900 text-xs font-bold transition-colors cursor-pointer flex-shrink-0"
            >
              Tìm
            </button>
          </div>
        </div>

        <!-- Batch API Mode Selection -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5">
            Phương thức lưu Batch
          </label>
          <select
            v-model="batchApiMethod"
            class="w-full text-xs font-semibold bg-white border border-gray-200 text-gray-700 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="lesson">POST /api/attendance/lesson/{id}</option>
            <option value="batch">POST /api/attendance/batch</option>
          </select>
        </div>
      </div>

      <!-- Current Lesson Metadata Banner -->
      <div
        v-if="lessonDetail"
        class="mt-3 p-4 rounded-2xl bg-gradient-to-r from-orange-50 via-amber-50 to-orange-50/40 border border-orange-200 flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs"
      >
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-xl bg-orange-500 text-white flex items-center justify-center font-bold text-sm shadow-xs flex-shrink-0">
            <AcademicCapIcon class="w-5 h-5" />
          </div>
          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <span class="font-bold text-base text-gray-900">{{ lessonDetail.className || 'Lớp học' }}</span>
              <span class="px-2 py-0.5 rounded-md font-semibold text-[11px] bg-orange-200/80 text-orange-900">
                Buổi học #{{ lessonDetail.lessonId }}
              </span>
              <span
                :class="[
                  'px-2 py-0.5 rounded-md font-semibold text-[11px]',
                  lessonDetail.lessonStatus === 'COMPLETED' ? 'bg-emerald-100 text-emerald-800' :
                  lessonDetail.lessonStatus === 'IN_PROGRESS' ? 'bg-blue-100 text-blue-800' :
                  'bg-gray-100 text-gray-700'
                ]"
              >
                {{ formatLessonStatus(lessonDetail.lessonStatus) }}
              </span>
            </div>
            <p class="text-gray-600 mt-1 flex items-center gap-2 flex-wrap">
              <span class="flex items-center gap-1 text-gray-700">
                <CalendarDaysIcon class="w-3.5 h-3.5 text-orange-600" />
                Ngày: {{ formatDate(lessonDetail.lessonDate) }}
              </span>
              <span v-if="lessonDetail.startTime" class="flex items-center gap-1 text-gray-700">
                <ClockIcon class="w-3.5 h-3.5 text-orange-600" />
                Giờ: {{ formatTime(lessonDetail.startTime) }} - {{ formatTime(lessonDetail.endTime) }}
              </span>
            </p>
          </div>
        </div>

        <!-- Attendance Rate Badge -->
        <div class="flex items-center gap-3 self-start md:self-auto bg-white/80 p-2.5 rounded-xl border border-orange-200">
          <div class="text-right">
            <p class="text-[11px] text-gray-500 font-medium">Tỷ lệ có mặt</p>
            <p class="text-base font-bold text-emerald-700">{{ attendanceRate }}%</p>
          </div>
          <div class="w-10 h-10 rounded-full border-2 border-emerald-400 flex items-center justify-center font-bold text-xs text-emerald-800 bg-emerald-50">
            {{ lessonDetail.presentCount }}/{{ lessonDetail.totalStudents }}
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Realtime Attendance Stats Cards ─────────────────────────────── -->
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
      <!-- Total -->
      <div class="bg-white rounded-2xl border border-orange-100 p-4 text-center shadow-xs">
        <p class="text-[11px] text-gray-500 font-bold uppercase tracking-wider">Tổng sĩ số</p>
        <p class="text-2xl font-bold text-gray-900 mt-1">{{ stats.total }}</p>
        <p class="text-[10px] text-gray-400 mt-0.5">học sinh trong lớp</p>
      </div>

      <!-- Present -->
      <div class="bg-emerald-50/70 rounded-2xl border border-emerald-200 p-4 text-center shadow-xs">
        <p class="text-[11px] text-emerald-700 font-bold uppercase tracking-wider">Có mặt</p>
        <p class="text-2xl font-bold text-emerald-800 mt-1">{{ stats.present }}</p>
        <p class="text-[10px] text-emerald-600 font-medium mt-0.5">đang tham gia</p>
      </div>

      <!-- Late -->
      <div class="bg-amber-50/70 rounded-2xl border border-amber-200 p-4 text-center shadow-xs">
        <p class="text-[11px] text-amber-700 font-bold uppercase tracking-wider">Đi muộn</p>
        <p class="text-2xl font-bold text-amber-800 mt-1">{{ stats.late }}</p>
        <p class="text-[10px] text-amber-600 font-medium mt-0.5">đến sau giờ học</p>
      </div>

      <!-- Excused -->
      <div class="bg-sky-50/70 rounded-2xl border border-sky-200 p-4 text-center shadow-xs">
        <p class="text-[11px] text-sky-700 font-bold uppercase tracking-wider">Có phép</p>
        <p class="text-2xl font-bold text-sky-800 mt-1">{{ stats.excused }}</p>
        <p class="text-[10px] text-sky-600 font-medium mt-0.5">có đơn xin nghỉ</p>
      </div>

      <!-- Absent -->
      <div class="bg-rose-50/70 rounded-2xl border border-rose-200 p-4 text-center shadow-xs">
        <p class="text-[11px] text-rose-700 font-bold uppercase tracking-wider">Vắng mặt</p>
        <p class="text-2xl font-bold text-rose-800 mt-1">{{ stats.absent }}</p>
        <p class="text-[10px] text-rose-600 font-medium mt-0.5">không có phép</p>
      </div>

      <!-- Unmarked -->
      <div class="bg-gray-50 rounded-2xl border border-gray-200 p-4 text-center shadow-xs">
        <p class="text-[11px] text-gray-500 font-bold uppercase tracking-wider">Chưa điểm danh</p>
        <p class="text-2xl font-bold text-gray-700 mt-1">{{ stats.unmarked }}</p>
        <p class="text-[10px] text-gray-400 mt-0.5">cần xác nhận</p>
      </div>
    </div>

    <!-- ─── Controls Toolbar & Filters ──────────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <!-- Quick Batch Actions -->
        <div class="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            @click="markAllPresent"
            :disabled="students.length === 0"
            class="px-3.5 py-2 rounded-xl text-xs font-bold bg-emerald-100/80 hover:bg-emerald-200 text-emerald-900 transition-colors cursor-pointer disabled:opacity-50 flex items-center gap-1.5"
          >
            <CheckIcon class="w-3.5 h-3.5" />
            <span>Tất cả CÓ MẶT</span>
          </button>

          <button
            type="button"
            @click="autoApplyExcusedForLeaveRequests"
            :disabled="students.length === 0 || countWithLeaveRequest === 0"
            class="px-3.5 py-2 rounded-xl text-xs font-bold bg-sky-100/80 hover:bg-sky-200 text-sky-900 transition-colors cursor-pointer disabled:opacity-50 flex items-center gap-1.5"
            :title="`Có ${countWithLeaveRequest} học sinh gửi đơn xin nghỉ phép`"
          >
            <DocumentTextIcon class="w-3.5 h-3.5 text-sky-700" />
            <span>Duyệt đơn nghỉ phép ({{ countWithLeaveRequest }})</span>
          </button>

          <button
            type="button"
            @click="resetAllToUnmarked"
            :disabled="students.length === 0"
            class="px-3 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer disabled:opacity-50"
          >
            Đặt lại
          </button>
        </div>

        <!-- Search & Status Filter -->
        <div class="flex items-center gap-2 flex-wrap">
          <div class="relative min-w-[200px]">
            <input
              type="text"
              v-model="searchQuery"
              placeholder="Tìm theo tên học sinh..."
              class="w-full pl-8 pr-3 py-2 rounded-xl border border-gray-200 text-xs focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
            <MagnifyingGlassIcon class="w-3.5 h-3.5 text-gray-400 absolute left-2.5 top-2.5" />
          </div>

          <select
            v-model="statusFilter"
            class="text-xs font-semibold bg-white border border-gray-200 text-gray-700 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="ALL">Tất cả trạng thái ({{ students.length }})</option>
            <option value="PRESENT">Có mặt</option>
            <option value="LATE">Đi muộn</option>
            <option value="EXCUSED">Có phép</option>
            <option value="ABSENT">Vắng mặt</option>
            <option value="UNMARKED">Chưa điểm danh</option>
            <option value="HAS_LEAVE">Có đơn xin nghỉ ({{ countWithLeaveRequest }})</option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Main Attendance Table ───────────────────────────────────────── -->
    <div class="bg-white rounded-3xl border border-orange-100 shadow-sm overflow-hidden">
      <!-- Loading state -->
      <div v-if="isLoading" class="p-12 text-center text-orange-600 space-y-3">
        <ArrowPathIcon class="w-8 h-8 animate-spin mx-auto text-orange-500" />
        <p class="text-sm font-semibold">Đang tải danh sách điểm danh buổi học...</p>
      </div>

      <!-- No lesson selected state -->
      <div v-else-if="!selectedLessonId" class="p-12 text-center text-gray-500 space-y-3">
        <div class="w-14 h-14 rounded-2xl bg-orange-100 text-orange-600 mx-auto flex items-center justify-center">
          <CalendarDaysIcon class="w-7 h-7" />
        </div>
        <h3 class="text-base font-bold text-gray-800">Chưa chọn buổi học để điểm danh</h3>
        <p class="text-xs text-gray-500 max-w-sm mx-auto">
          Vui lòng chọn Lớp học và Buổi học ở thanh công cụ phía trên để tải danh sách học sinh và ghi nhận chuyên cần.
        </p>
      </div>

      <!-- Empty list state -->
      <div v-else-if="filteredStudents.length === 0" class="p-12 text-center text-gray-500 space-y-2">
        <p class="font-bold text-gray-800">Không tìm thấy học sinh nào</p>
        <p class="text-xs text-gray-400">Không có học sinh phù hợp với bộ lọc hiện tại.</p>
      </div>

      <!-- Students Table -->
      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-xs">
          <thead>
            <tr class="bg-orange-50/80 border-b border-orange-100 text-orange-950 font-bold uppercase tracking-wider text-[11px]">
              <th class="py-4 px-4 w-12 text-center">STT</th>
              <th class="py-4 px-4 min-w-[220px]">Học sinh & Đơn xin nghỉ</th>
              <th class="py-4 px-4 text-center min-w-[340px]">Trạng thái điểm danh</th>
              <th class="py-4 px-4 min-w-[220px]">Ghi chú giáo viên</th>
              <th class="py-4 px-4 text-center min-w-[150px]">Thao tác lẻ</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-orange-50">
            <tr
              v-for="(st, index) in filteredStudents"
              :key="st.studentId"
              :class="[
                'transition-colors',
                st.status === 'ABSENT' ? 'bg-rose-50/40' :
                st.status === 'LATE' ? 'bg-amber-50/30' :
                st.status === 'EXCUSED' ? 'bg-sky-50/30' :
                st.status === 'PRESENT' ? 'hover:bg-emerald-50/20' :
                'hover:bg-orange-50/20'
              ]"
            >
              <!-- Index -->
              <td class="py-3.5 px-4 text-center text-gray-400 font-mono font-medium">
                {{ index + 1 }}
              </td>

              <!-- Student Profile & Leave Info -->
              <td class="py-3.5 px-4">
                <div class="flex items-center gap-3">
                  <!-- Avatar -->
                  <div class="w-9 h-9 rounded-full bg-orange-100 border border-orange-200 text-orange-800 font-bold text-xs flex items-center justify-center flex-shrink-0 shadow-2xs">
                    <img
                      v-if="st.avatar"
                      :src="st.avatar"
                      :alt="st.studentName"
                      class="w-full h-full rounded-full object-cover"
                    />
                    <span v-else>{{ getInitials(st.studentName) }}</span>
                  </div>

                  <!-- Name and Details -->
                  <div class="min-w-0">
                    <div class="flex items-center gap-1.5 flex-wrap">
                      <p class="font-bold text-sm text-gray-900 truncate">{{ st.studentName }}</p>
                      <span class="text-[10px] font-mono text-gray-400">#{{ st.studentId }}</span>
                      <span
                        v-if="st.gender"
                        class="text-[10px] px-1.5 py-0.2 rounded bg-gray-100 text-gray-600 font-medium"
                      >
                        {{ st.gender === 'Female' ? 'Nữ' : 'Nam' }}
                      </span>
                    </div>

                    <!-- Leave Request Indicator (If student submitted a leave request) -->
                    <div v-if="st.hasLeaveRequest" class="mt-1 flex items-center gap-1.5 flex-wrap">
                      <span
                        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 cursor-pointer hover:bg-indigo-100 transition-colors"
                        @click="openLeaveDetailModal(st)"
                        title="Bấm để xem nội dung chi tiết đơn xin nghỉ phép"
                      >
                        <DocumentTextIcon class="w-3 h-3 text-indigo-600" />
                        Đơn xin nghỉ: {{ st.leaveReason || 'Phụ huynh xin phép' }}
                      </span>
                      <button
                        v-if="st.status !== 'EXCUSED'"
                        type="button"
                        @click="applyLeaveRequest(st)"
                        class="text-[10px] font-semibold text-sky-700 hover:text-sky-900 underline cursor-pointer"
                      >
                        Duyệt có phép
                      </button>
                    </div>
                  </div>
                </div>
              </td>

              <!-- Status Buttons (PRESENT, LATE, EXCUSED, ABSENT) -->
              <td class="py-3.5 px-4">
                <div class="flex items-center justify-center gap-1.5">
                  <!-- Có mặt -->
                  <button
                    type="button"
                    @click="setStudentStatus(st, 'PRESENT')"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all cursor-pointer flex items-center gap-1',
                      st.status === 'PRESENT'
                        ? 'bg-emerald-500 text-white shadow-xs ring-2 ring-emerald-300'
                        : 'bg-gray-100/80 text-gray-600 hover:bg-emerald-50 hover:text-emerald-700'
                    ]"
                  >
                    <CheckIcon v-if="st.status === 'PRESENT'" class="w-3.5 h-3.5" />
                    <span>Có mặt</span>
                  </button>

                  <!-- Đi muộn -->
                  <button
                    type="button"
                    @click="setStudentStatus(st, 'LATE')"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all cursor-pointer flex items-center gap-1',
                      st.status === 'LATE'
                        ? 'bg-amber-500 text-white shadow-xs ring-2 ring-amber-300'
                        : 'bg-gray-100/80 text-gray-600 hover:bg-amber-50 hover:text-amber-700'
                    ]"
                  >
                    <ClockIcon v-if="st.status === 'LATE'" class="w-3.5 h-3.5" />
                    <span>Đi muộn</span>
                  </button>

                  <!-- Có phép -->
                  <button
                    type="button"
                    @click="setStudentStatus(st, 'EXCUSED')"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all cursor-pointer flex items-center gap-1',
                      st.status === 'EXCUSED'
                        ? 'bg-sky-500 text-white shadow-xs ring-2 ring-sky-300'
                        : 'bg-gray-100/80 text-gray-600 hover:bg-sky-50 hover:text-sky-700'
                    ]"
                  >
                    <span>Có phép</span>
                  </button>

                  <!-- Vắng mặt -->
                  <button
                    type="button"
                    @click="setStudentStatus(st, 'ABSENT')"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all cursor-pointer flex items-center gap-1',
                      st.status === 'ABSENT'
                        ? 'bg-rose-500 text-white shadow-xs ring-2 ring-rose-300'
                        : 'bg-gray-100/80 text-gray-600 hover:bg-rose-50 hover:text-rose-700'
                    ]"
                  >
                    <span>Vắng</span>
                  </button>
                </div>
              </td>

              <!-- Teacher Notes Input -->
              <td class="py-3.5 px-4">
                <input
                  type="text"
                  v-model="st.teacherNotes"
                  :placeholder="st.status === 'ABSENT' ? 'Lý do vắng mặt...' : 'Ghi chú thêm...'"
                  :class="[
                    'w-full text-xs rounded-xl px-3 py-2 border transition-all focus:outline-none focus:ring-2 focus:ring-orange-300',
                    st.status === 'ABSENT' ? 'border-rose-300 bg-rose-50/20 text-rose-900 placeholder-rose-400' :
                    st.status === 'LATE' ? 'border-amber-300 bg-amber-50/20 text-amber-900' :
                    'border-gray-200 bg-white'
                  ]"
                />
              </td>

              <!-- Single Row Actions (PUT Single + View History) -->
              <td class="py-3.5 px-4 text-center">
                <div class="flex items-center justify-center gap-2">
                  <!-- Quick Save Single (PUT /api/attendance/lesson/{lessonId}/student/{studentId}) -->
                  <button
                    type="button"
                    @click="handleSaveSingle(st)"
                    :disabled="st.isSavingSingle || st.status === 'UNMARKED'"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1',
                      st.justSavedSingle
                        ? 'bg-emerald-100 text-emerald-800'
                        : 'bg-orange-100/70 hover:bg-orange-200 text-orange-900'
                    ]"
                    :title="st.status === 'UNMARKED' ? 'Vui lòng chọn trạng thái trước khi lưu lẻ' : 'Lưu riêng học sinh này'"
                  >
                    <ArrowPathIcon v-if="st.isSavingSingle" class="w-3.5 h-3.5 animate-spin text-orange-600" />
                    <CheckIcon v-else-if="st.justSavedSingle" class="w-3.5 h-3.5 text-emerald-600" />
                    <span v-if="st.justSavedSingle">Đã lưu</span>
                    <span v-else>Lưu lẻ</span>
                  </button>

                  <!-- View Student History (GET /api/attendance/student/{studentId}?classId={classId}) -->
                  <button
                    type="button"
                    @click="openStudentHistory(st)"
                    class="p-1.5 rounded-lg text-gray-500 hover:text-orange-600 hover:bg-orange-50 transition-colors cursor-pointer"
                    title="Xem lịch sử điểm danh của học sinh"
                  >
                    <ClockIcon class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Table Footer Save Bar -->
      <div v-if="filteredStudents.length > 0" class="p-4 bg-gray-50/60 border-t border-orange-100 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p class="text-xs text-gray-500">
          Hiển thị <strong class="text-gray-900">{{ filteredStudents.length }}</strong> / {{ students.length }} học sinh
        </p>

        <button
          type="button"
          @click="handleSaveBatchAttendance"
          :disabled="isSavingBatch || !selectedLessonId"
          class="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-sm transition-all cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon v-if="isSavingBatch" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>Lưu tất cả điểm danh (Batch)</span>
        </button>
      </div>
    </div>

    <!-- ─── Modal 1: Student Attendance History ──────────────────────────── -->
    <div
      v-if="showHistoryModal"
      class="fixed inset-0 bg-black/40 backdrop-blur-xs z-50 flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-3xl w-full max-w-2xl overflow-hidden shadow-2xl border border-orange-100 flex flex-col max-h-[90vh]">
        <!-- Modal Header -->
        <div class="px-6 py-4 bg-gradient-to-r from-orange-50 to-amber-50 border-b border-orange-100 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-orange-500 text-white flex items-center justify-center font-bold text-sm shadow-xs">
              {{ getInitials(selectedStudentHistory?.studentName || 'HS') }}
            </div>
            <div>
              <h3 class="font-bold text-gray-900 text-base">Lịch sử điểm danh học sinh</h3>
              <p class="text-xs text-gray-600">
                {{ selectedStudentHistory?.studentName }} &bull; Mã: #{{ selectedStudentHistory?.studentId }}
              </p>
            </div>
          </div>
          <button
            type="button"
            @click="showHistoryModal = false"
            class="text-gray-400 hover:text-gray-600 p-1 rounded-xl transition-colors cursor-pointer"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- Modal Body Content -->
        <div class="p-6 overflow-y-auto space-y-4 flex-1">
          <!-- History Stats Ribbon -->
          <div class="grid grid-cols-4 gap-2 text-center text-xs">
            <div class="p-2.5 rounded-xl bg-gray-50 border border-gray-100">
              <p class="text-gray-500">Tổng buổi</p>
              <p class="text-base font-bold text-gray-900 mt-0.5">{{ studentHistoryList.length }}</p>
            </div>
            <div class="p-2.5 rounded-xl bg-emerald-50 border border-emerald-100">
              <p class="text-emerald-700">Có mặt</p>
              <p class="text-base font-bold text-emerald-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'PRESENT').length }}
              </p>
            </div>
            <div class="p-2.5 rounded-xl bg-amber-50 border border-amber-100">
              <p class="text-amber-700">Đi muộn</p>
              <p class="text-base font-bold text-amber-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'LATE').length }}
              </p>
            </div>
            <div class="p-2.5 rounded-xl bg-rose-50 border border-rose-100">
              <p class="text-rose-700">Vắng</p>
              <p class="text-base font-bold text-rose-800 mt-0.5">
                {{ studentHistoryList.filter(h => h.status === 'ABSENT' || h.status === 'EXCUSED').length }}
              </p>
            </div>
          </div>

          <!-- History Table -->
          <div v-if="isLoadingHistory" class="py-8 text-center text-orange-500 space-y-2">
            <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto" />
            <p class="text-xs font-semibold">Đang tải lịch sử điểm danh...</p>
          </div>

          <div v-else-if="studentHistoryList.length === 0" class="py-8 text-center text-gray-400 space-y-1">
            <CalendarDaysIcon class="w-8 h-8 mx-auto text-gray-300" />
            <p class="text-xs font-medium">Chưa có bản ghi điểm danh nào trước đây của học sinh này.</p>
          </div>

          <div v-else class="border border-gray-100 rounded-2xl overflow-hidden shadow-2xs">
            <table class="w-full text-left text-xs">
              <thead class="bg-gray-50 border-b border-gray-100 text-gray-700 font-bold uppercase text-[10px]">
                <tr>
                  <th class="py-2.5 px-3">Ngày học</th>
                  <th class="py-2.5 px-3">Lớp học</th>
                  <th class="py-2.5 px-3 text-center">Trạng thái</th>
                  <th class="py-2.5 px-3">Ghi chú</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="h in studentHistoryList" :key="h.attendanceId || h.lessonId" class="hover:bg-gray-50/50">
                  <td class="py-2.5 px-3 font-medium text-gray-900">
                    {{ formatDate(h.lessonDate) }}
                  </td>
                  <td class="py-2.5 px-3 text-gray-700">
                    {{ h.className || 'Toán tư duy' }}
                  </td>
                  <td class="py-2.5 px-3 text-center">
                    <span
                      :class="[
                        'px-2 py-0.5 rounded-full font-bold text-[10px] inline-block',
                        h.status === 'PRESENT' ? 'bg-emerald-100 text-emerald-800' :
                        h.status === 'LATE' ? 'bg-amber-100 text-amber-800' :
                        h.status === 'EXCUSED' ? 'bg-sky-100 text-sky-800' :
                        'bg-rose-100 text-rose-800'
                      ]"
                    >
                      {{ formatStatusLabel(h.status) }}
                    </span>
                  </td>
                  <td class="py-2.5 px-3 text-gray-500 italic">
                    {{ h.teacherNotes || '—' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3 bg-gray-50 border-t border-gray-100 flex justify-end">
          <button
            type="button"
            @click="showHistoryModal = false"
            class="px-4 py-2 rounded-xl text-xs font-semibold bg-white border border-gray-200 text-gray-700 hover:bg-gray-100 transition-colors cursor-pointer"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>

    <!-- ─── Modal 2: Leave Request Detail ───────────────────────────────── -->
    <div
      v-if="showLeaveModal"
      class="fixed inset-0 bg-black/40 backdrop-blur-xs z-50 flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-3xl w-full max-w-md overflow-hidden shadow-2xl border border-orange-100">
        <div class="px-6 py-4 bg-indigo-50/80 border-b border-indigo-100 flex items-center justify-between">
          <div class="flex items-center gap-2">
            <DocumentTextIcon class="w-5 h-5 text-indigo-700" />
            <h3 class="font-bold text-indigo-950 text-sm">Chi tiết đơn xin nghỉ phép</h3>
          </div>
          <button
            type="button"
            @click="showLeaveModal = false"
            class="text-gray-400 hover:text-gray-600 p-1 rounded-lg transition-colors cursor-pointer"
          >
            <XMarkIcon class="w-4 h-4" />
          </button>
        </div>

        <div class="p-6 space-y-4 text-xs">
          <div>
            <p class="text-gray-500 font-medium">Học sinh:</p>
            <p class="font-bold text-gray-900 text-sm mt-0.5">{{ selectedLeaveStudent?.studentName }}</p>
          </div>

          <div>
            <p class="text-gray-500 font-medium">Lý do xin nghỉ từ phụ huynh:</p>
            <div class="mt-1 p-3 rounded-xl bg-indigo-50/50 border border-indigo-100 text-indigo-950 font-medium leading-relaxed">
              {{ selectedLeaveStudent?.leaveReason || 'Phụ huynh xin phép vắng mặt trong buổi học này.' }}
            </div>
          </div>

          <div>
            <p class="text-gray-500 font-medium">Trạng thái đơn:</p>
            <span class="inline-block mt-1 px-2.5 py-0.5 rounded-full font-bold text-[11px] bg-amber-100 text-amber-800">
              {{ selectedLeaveStudent?.leaveRequestStatus || 'Chờ duyệt' }}
            </span>
          </div>

          <div class="pt-3 border-t border-gray-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showLeaveModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer"
            >
              Đóng
            </button>
            <button
              type="button"
              @click="applyLeaveRequestFromModal"
              class="px-4 py-2 rounded-xl text-xs font-bold bg-sky-600 hover:bg-sky-700 text-white transition-colors cursor-pointer shadow-xs"
            >
              Duyệt nghỉ có phép (EXCUSED)
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import attendanceService from '@/services/attendance.service'
import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import {
  ChevronRightIcon,
  ArrowPathIcon,
  CheckCircleIcon,
  ExclamationCircleIcon,
  XMarkIcon,
  AcademicCapIcon,
  CalendarDaysIcon,
  ClockIcon,
  DocumentTextIcon,
  MagnifyingGlassIcon,
  CheckIcon,
} from '@heroicons/vue/24/outline'

const route = useRoute()
const router = useRouter()

// ─── Component State ─────────────────────────────────────────────────────────
const classes = ref([])
const lessons = ref([])

const selectedClassId = ref(null)
const selectedLessonId = ref(null)
const directLessonIdInput = ref(null)
const batchApiMethod = ref('lesson') // 'lesson' = POST /lesson/{id} | 'batch' = POST /batch

const lessonDetail = ref(null)
const students = ref([])

const isLoading = ref(false)
const isLoadingLessons = ref(false)
const isSavingBatch = ref(false)

const searchQuery = ref('')
const statusFilter = ref('ALL') // 'ALL', 'PRESENT', 'LATE', 'EXCUSED', 'ABSENT', 'UNMARKED', 'HAS_LEAVE'

// Modal states
const showHistoryModal = ref(false)
const selectedStudentHistory = ref(null)
const studentHistoryList = ref([])
const isLoadingHistory = ref(false)

const showLeaveModal = ref(false)
const selectedLeaveStudent = ref(null)

const feedback = reactive({
  message: '',
  type: 'success', // 'success' | 'error'
})

function showFeedback(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) {
      feedback.message = ''
    }
  }, 4000)
}

// ─── Computed Statistics ─────────────────────────────────────────────────────
const stats = computed(() => {
  const list = students.value
  return {
    total: list.length,
    present: list.filter(s => s.status === 'PRESENT').length,
    late: list.filter(s => s.status === 'LATE').length,
    excused: list.filter(s => s.status === 'EXCUSED').length,
    absent: list.filter(s => s.status === 'ABSENT').length,
    unmarked: list.filter(s => s.status === 'UNMARKED' || !s.status).length,
  }
})

const attendanceRate = computed(() => {
  if (stats.value.total === 0) return 0
  return Math.round((stats.value.present / stats.value.total) * 100)
})

const countWithLeaveRequest = computed(() => {
  return students.value.filter(s => s.hasLeaveRequest).length
})

// ─── Filtered Students ───────────────────────────────────────────────────────
const filteredStudents = computed(() => {
  let list = students.value

  // Filter by status
  if (statusFilter.value === 'HAS_LEAVE') {
    list = list.filter(s => s.hasLeaveRequest)
  } else if (statusFilter.value !== 'ALL') {
    list = list.filter(s => s.status === statusFilter.value)
  }

  // Filter by query
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(s =>
      s.studentName.toLowerCase().includes(q) ||
      String(s.studentId).includes(q)
    )
  }

  return list
})

// ─── Lifecycle & Initial Load ────────────────────────────────────────────────
onMounted(async () => {
  await loadClasses()

  // Handle URL query parameters if navigated from schedule or class view
  if (route.query.lessonId) {
    const lId = parseInt(route.query.lessonId, 10)
    if (!isNaN(lId)) {
      selectedLessonId.value = lId
      directLessonIdInput.value = lId
      await fetchAttendance(lId)
    }
  } else if (route.query.classId) {
    const cId = parseInt(route.query.classId, 10)
    if (!isNaN(cId)) {
      selectedClassId.value = cId
      await loadLessonsByClass(cId)
    }
  }
})

// ─── Load Classes & Lessons ──────────────────────────────────────────────────
async function loadClasses() {
  try {
    const data = await classService.getAll()
    classes.value = Array.isArray(data) ? data : (data.items || [])
    if (classes.value.length > 0 && !selectedClassId.value) {
      selectedClassId.value = classes.value[0].id
      await loadLessonsByClass(selectedClassId.value)
    }
  } catch (err) {
    console.warn('Lỗi khi tải danh sách lớp học:', err)
  }
}

async function loadLessonsByClass(classId) {
  if (!classId) return
  isLoadingLessons.value = true
  lessons.value = []
  try {
    const data = await lessonService.getByClass(classId)
    lessons.value = Array.isArray(data) ? data : (data.items || [])
    if (lessons.value.length > 0 && !selectedLessonId.value) {
      selectedLessonId.value = lessons.value[0].id
      await fetchAttendance(selectedLessonId.value)
    }
  } catch (err) {
    console.warn('Lỗi khi tải buổi học của lớp:', err)
  } finally {
    isLoadingLessons.value = false
  }
}

async function onClassChange() {
  selectedLessonId.value = null
  lessonDetail.value = null
  students.value = []
  await loadLessonsByClass(selectedClassId.value)
}

async function onLessonChange() {
  if (selectedLessonId.value) {
    await fetchAttendance(selectedLessonId.value)
  }
}

async function jumpToDirectLesson() {
  if (!directLessonIdInput.value) return
  selectedLessonId.value = directLessonIdInput.value
  await fetchAttendance(directLessonIdInput.value)
}

function reloadCurrentLesson() {
  if (selectedLessonId.value) {
    fetchAttendance(selectedLessonId.value)
  }
}

// ─── 1. GET /api/attendance/lesson/{lessonId} ────────────────────────────────
async function fetchAttendance(lessonId) {
  if (!lessonId) return
  isLoading.value = true
  try {
    const res = await attendanceService.getByLesson(lessonId)
    lessonDetail.value = res
    if (res.classId && selectedClassId.value !== res.classId) {
      selectedClassId.value = res.classId
    }

    // Map student items with extra UI state
    students.value = (res.students || []).map(st => ({
      ...st,
      status: st.status || 'UNMARKED',
      teacherNotes: st.teacherNotes || '',
      isSavingSingle: false,
      justSavedSingle: false,
    }))
  } catch (err) {
    const msg = err.response?.data?.message || `Không thể tải thông tin điểm danh buổi #${lessonId}.`
    showFeedback(msg, 'error')
  } finally {
    isLoading.value = false
  }
}

// ─── Status change helpers ───────────────────────────────────────────────────
function setStudentStatus(student, status) {
  student.status = status
  student.justSavedSingle = false
}

function markAllPresent() {
  students.value.forEach(st => {
    st.status = 'PRESENT'
    st.justSavedSingle = false
  })
  showFeedback('Đã đặt tất cả học sinh là CÓ MẶT.', 'success')
}

function autoApplyExcusedForLeaveRequests() {
  let count = 0
  students.value.forEach(st => {
    if (st.hasLeaveRequest) {
      st.status = 'EXCUSED'
      if (!st.teacherNotes) {
        st.teacherNotes = st.leaveReason ? `Đơn xin nghỉ: ${st.leaveReason}` : 'Có đơn xin nghỉ phép'
      }
      st.justSavedSingle = false
      count++
    }
  })
  showFeedback(`Đã áp dụng CÓ PHÉP cho ${count} học sinh có đơn xin nghỉ.`, 'success')
}

function resetAllToUnmarked() {
  students.value.forEach(st => {
    st.status = 'UNMARKED'
    st.justSavedSingle = false
  })
}

// ─── 2. POST /api/attendance/lesson/{lessonId} & POST /api/attendance/batch ──
async function handleSaveBatchAttendance() {
  if (!selectedLessonId.value) return

  // Validate that all students have a status
  const unassigned = students.value.filter(s => !s.status || s.status === 'UNMARKED')
  if (unassigned.length > 0) {
    if (!confirm(`Còn ${unassigned.length} học sinh chưa được đánh dấu điểm danh. Bạn có muốn tiếp tục lưu? (Các em này sẽ được giữ trạng thái hoặc đặt mặc định)`)) {
      return
    }
  }

  isSavingBatch.value = true
  const items = students.value.map(s => ({
    studentId: s.studentId,
    status: (s.status === 'UNMARKED' || !s.status) ? 'ABSENT' : s.status,
    teacherNotes: s.teacherNotes?.trim() || null,
  }))

  try {
    let res
    if (batchApiMethod.value === 'lesson') {
      // POST /api/attendance/lesson/{lessonId}
      res = await attendanceService.saveBatchByLesson(selectedLessonId.value, items)
    } else {
      // POST /api/attendance/batch
      res = await attendanceService.saveBatch({
        lessonId: selectedLessonId.value,
        attendances: items,
      })
    }

    const updatedDetail = res?.data || res
    if (updatedDetail && updatedDetail.lessonId) {
      lessonDetail.value = updatedDetail
      students.value = (updatedDetail.students || []).map(st => ({
        ...st,
        isSavingSingle: false,
        justSavedSingle: false,
      }))
    }

    showFeedback('Đã lưu điểm danh toàn bộ lớp học thành công!', 'success')
  } catch (err) {
    const msg = err.response?.data?.message || err.message || 'Lỗi khi lưu điểm danh lớp học.'
    showFeedback(msg, 'error')
  } finally {
    isSavingBatch.value = false
  }
}

// ─── 3. PUT /api/attendance/lesson/{lessonId}/student/{studentId} ───────────
async function handleSaveSingle(student) {
  if (!selectedLessonId.value || !student.studentId) return
  if (student.status === 'UNMARKED') {
    showFeedback('Vui lòng chọn trạng thái điểm danh trước khi lưu.', 'error')
    return
  }

  student.isSavingSingle = true
  try {
    const payload = {
      studentId: student.studentId,
      status: student.status,
      teacherNotes: student.teacherNotes?.trim() || null,
    }

    await attendanceService.updateSingle(selectedLessonId.value, student.studentId, payload)
    student.justSavedSingle = true
    setTimeout(() => {
      student.justSavedSingle = false
    }, 2500)
    showFeedback(`Đã cập nhật điểm danh cho ${student.studentName}`, 'success')
  } catch (err) {
    const msg = err.response?.data?.message || `Lỗi khi cập nhật cho ${student.studentName}`
    showFeedback(msg, 'error')
  } finally {
    student.isSavingSingle = false
  }
}

// ─── 4. GET /api/attendance/student/{studentId}?classId={classId} ───────────
async function openStudentHistory(student) {
  selectedStudentHistory.value = student
  studentHistoryList.value = []
  showHistoryModal.value = true
  isLoadingHistory.value = true

  try {
    const history = await attendanceService.getStudentHistory(
      student.studentId,
      selectedClassId.value || null
    )
    studentHistoryList.value = Array.isArray(history) ? history : []
  } catch (err) {
    console.warn('Lỗi khi tải lịch sử điểm danh học sinh:', err)
  } finally {
    isLoadingHistory.value = false
  }
}

// ─── Leave Request Modals ────────────────────────────────────────────────────
function openLeaveDetailModal(student) {
  selectedLeaveStudent.value = student
  showLeaveModal.value = true
}

function applyLeaveRequest(student) {
  student.status = 'EXCUSED'
  if (!student.teacherNotes) {
    student.teacherNotes = student.leaveReason ? `Đơn xin nghỉ: ${student.leaveReason}` : 'Có đơn xin nghỉ phép'
  }
  student.justSavedSingle = false
  showFeedback(`Đã chuyển trạng thái của ${student.studentName} thành CÓ PHÉP.`, 'success')
}

function applyLeaveRequestFromModal() {
  if (selectedLeaveStudent.value) {
    applyLeaveRequest(selectedLeaveStudent.value)
    showLeaveModal.value = false
  }
}

// ─── Formatting Helpers ──────────────────────────────────────────────────────
function formatDate(dateStr) {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

function formatTime(timeStr) {
  if (!timeStr) return ''
  const d = new Date(timeStr)
  return d.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  })
}

function formatStatusLabel(st) {
  switch (st) {
    case 'PRESENT': return 'Có mặt'
    case 'LATE': return 'Đi muộn'
    case 'EXCUSED': return 'Có phép'
    case 'ABSENT': return 'Vắng mặt'
    case 'UNMARKED': return 'Chưa điểm danh'
    default: return st || '—'
  }
}

function formatLessonStatus(s) {
  switch (s) {
    case 'COMPLETED': return 'Đã kết thúc'
    case 'IN_PROGRESS': return 'Đang diễn ra'
    case 'SCHEDULED': return 'Sắp diễn ra'
    case 'CANCELLED': return 'Đã hủy'
    default: return s || 'Đang diễn ra'
  }
}

function getInitials(name) {
  if (!name) return 'HS'
  const parts = name.trim().split(/\s+/)
  if (parts.length >= 2) {
    return (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return name.slice(0, 2).toUpperCase()
}
</script>

<style scoped>
.slide-fade-enter-active {
  transition: all 0.25s ease-out;
}
.slide-fade-leave-active {
  transition: all 0.2s cubic-bezier(1, 0.5, 0.8, 1);
}
.slide-fade-enter-from,
.slide-fade-leave-to {
  transform: translateY(-8px);
  opacity: 0;
}
</style>
