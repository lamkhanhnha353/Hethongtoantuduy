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
          Theo dõi chuyên cần từng buổi học, duyệt đơn xin nghỉ phép và tra cứu lịch sử học sinh
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
          :disabled="isSavingBatch || !selectedLessonId || students.length === 0 || lockState.isLocked"
          :class="[
            'inline-flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-bold shadow-sm transition-all cursor-pointer',
            lockState.isLocked
              ? 'bg-gray-200 text-gray-400 cursor-not-allowed shadow-none'
              : 'bg-orange-500 hover:bg-orange-600 text-white disabled:opacity-50'
          ]"
          :title="lockState.isLocked ? 'Đã hết hạn điểm danh (Chỉ được điểm danh trong 1 ngày sau khi kết thúc buổi học)' : 'Lưu điểm danh cả lớp'"
        >
          <LockClosedIcon v-if="lockState.isLocked" class="w-4 h-4" />
          <ArrowPathIcon v-else-if="isSavingBatch" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>{{ lockState.isLocked ? 'Đã khóa điểm danh' : 'Lưu tất cả điểm danh' }}</span>
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
            : feedback.type === 'warning'
            ? 'bg-amber-50 border-amber-200 text-amber-800'
            : 'bg-rose-50 border-rose-200 text-rose-800'
        ]"
      >
        <div class="flex items-center gap-3">
          <CheckCircleIcon v-if="feedback.type === 'success'" class="w-5 h-5 text-emerald-600 flex-shrink-0" />
          <ExclamationTriangleIcon v-else-if="feedback.type === 'warning'" class="w-5 h-5 text-amber-600 flex-shrink-0" />
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

    <!-- ─── Session & Class Selector Card (Only assigned classes & sorted lessons) ─── -->
    <div class="bg-white rounded-3xl border border-orange-100 p-5 shadow-sm space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <!-- 1. Select Class (Only Assigned Classes) -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5 flex items-center justify-between">
            <span>1. Chọn lớp phụ trách <span class="text-rose-500">*</span></span>
            <span class="text-[11px] font-medium text-orange-600 lowercase">
              ({{ classes.length }} lớp được phân công)
            </span>
          </label>
          <div class="relative">
            <select
              v-model="selectedClassId"
              @change="onClassChange"
              class="w-full text-xs font-semibold bg-orange-50/50 border border-orange-200 text-gray-900 rounded-xl px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 transition-all"
            >
              <option :value="null" disabled>-- Chọn lớp học đang phụ trách --</option>
              <option v-for="cls in classes" :key="cls.id" :value="cls.id">
                {{ cls.name }} {{ cls.room ? `(${cls.room})` : '' }}
              </option>
            </select>
          </div>
          <p v-if="classes.length === 0 && !isLoadingClasses" class="text-xs text-amber-600 mt-1">
            Không tìm thấy lớp học nào được phân công cho giáo viên này.
          </p>
        </div>

        <!-- 2. Select Lesson (Sorted by Date & Sequenced: Buổi 1 -> Buổi 24) -->
        <div class="lg:col-span-2">
          <label class="block text-xs font-bold uppercase tracking-wider text-gray-700 mb-1.5 flex items-center justify-between">
            <span>2. Chọn buổi học <span class="text-rose-500">*</span></span>
            <span class="text-[11px] font-medium text-gray-500">
              (Sắp xếp theo ngày học: Buổi 1 &rarr; Buổi {{ lessons.length || 24 }})
            </span>
          </label>
          <div class="relative">
            <select
              v-model="selectedLessonId"
              @change="onLessonChange"
              :disabled="isLoadingLessons || lessons.length === 0"
              class="w-full text-xs font-semibold bg-orange-50/50 border border-orange-200 text-gray-900 rounded-xl px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 transition-all disabled:opacity-50"
            >
              <option :value="null" disabled>
                {{ isLoadingLessons ? 'Đang tải danh sách buổi học...' : (lessons.length === 0 ? '-- Lớp chưa có buổi học nào --' : '-- Chọn buổi học --') }}
              </option>
              <option
                v-for="les in filteredLessonsByTab"
                :key="les.id"
                :value="les.id"
              >
                {{ les.displayName }} [{{ les.timingStatus.label }}]
              </option>
            </select>
          </div>
        </div>
      </div>

      <!-- Quick Lesson Filter Bar by State: Tất cả, Đang diễn ra, Đã kết thúc, Đã điểm danh, Chưa điểm danh -->
      <div v-if="lessons.length > 0" class="pt-2 border-t border-gray-100 flex items-center justify-between flex-wrap gap-2 text-xs">
        <span class="font-bold text-gray-600 text-[11px] uppercase tracking-wider">Lọc nhanh buổi học:</span>
        <div class="flex items-center gap-1.5 flex-wrap">
          <button
            type="button"
            v-for="tab in lessonTabs"
            :key="tab.id"
            @click="currentLessonTab = tab.id"
            :class="[
              'px-3 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer',
              currentLessonTab === tab.id
                ? 'bg-orange-500 text-white shadow-xs'
                : 'bg-gray-100 text-gray-600 hover:bg-orange-50 hover:text-orange-900'
            ]"
          >
            {{ tab.label }}
          </button>
        </div>
      </div>

      <!-- ─── Current Lesson Metadata & Status Banner ───────────────────── -->
      <div
        v-if="currentLesson"
        class="mt-3 p-4 rounded-2xl bg-gradient-to-r from-orange-50 via-amber-50 to-orange-50/40 border border-orange-200 flex flex-col md:flex-row md:items-center justify-between gap-4 text-xs"
      >
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-2xl bg-orange-500 text-white flex items-center justify-center font-bold text-base shadow-xs flex-shrink-0">
            {{ currentLesson.lessonNumber || '1' }}
          </div>
          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <span class="font-bold text-base text-gray-900">{{ currentLesson.label }} - {{ lessonDetail?.className || 'Lớp học' }}</span>

              <!-- Timing Status: Đang diễn ra / Đã kết thúc / Sắp diễn ra -->
              <span
                :class="[
                  'px-2.5 py-0.5 rounded-full font-bold text-[11px] inline-flex items-center gap-1',
                  currentLessonTiming.code === 'IN_PROGRESS' ? 'bg-emerald-100 text-emerald-800 ring-2 ring-emerald-300' :
                  currentLessonTiming.code === 'ENDED' ? 'bg-gray-200 text-gray-800' :
                  'bg-blue-100 text-blue-800'
                ]"
              >
                <span v-if="currentLessonTiming.code === 'IN_PROGRESS'" class="w-2 h-2 rounded-full bg-emerald-600 animate-ping"></span>
                <span>{{ currentLessonTiming.label }}</span>
              </span>

              <!-- Attendance Status: Đã điểm danh / Chưa điểm danh -->
              <span
                :class="[
                  'px-2.5 py-0.5 rounded-full font-bold text-[11px] inline-flex items-center gap-1',
                  currentLessonAttendanceStatus.code === 'COMPLETED' ? 'bg-emerald-100 text-emerald-800 border border-emerald-300' :
                  currentLessonAttendanceStatus.code === 'PARTIAL' ? 'bg-amber-100 text-amber-800 border border-amber-300' :
                  'bg-rose-100 text-rose-800 border border-rose-300'
                ]"
              >
                <CheckIcon v-if="currentLessonAttendanceStatus.code === 'COMPLETED'" class="w-3.5 h-3.5" />
                <span>{{ currentLessonAttendanceStatus.label }}</span>
              </span>
            </div>

            <!-- Date and Time -->
            <p class="text-gray-600 mt-1 flex items-center gap-3 flex-wrap">
              <span class="flex items-center gap-1 text-gray-700">
                <CalendarDaysIcon class="w-3.5 h-3.5 text-orange-600" />
                Ngày học: <strong>{{ formatDate(currentLesson.lessonDate) }}</strong>
              </span>
              <span v-if="currentLesson.startTime" class="flex items-center gap-1 text-gray-700">
                <ClockIcon class="w-3.5 h-3.5 text-orange-600" />
                Thời gian: {{ formatTime(currentLesson.startTime) }} - {{ formatTime(currentLesson.endTime) }}
              </span>
            </p>
          </div>
        </div>

        <!-- Attendance Deadline / Lock Indicator -->
        <div class="flex items-center gap-3 self-start md:self-auto">
          <div
            v-if="lockState.isLocked"
            class="px-3 py-2 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-right"
          >
            <p class="font-bold flex items-center gap-1 text-[11px] text-rose-700">
              <LockClosedIcon class="w-3.5 h-3.5" /> ĐÃ HẾT HẠN ĐIỂM DANH
            </p>
            <p class="text-[10px] text-rose-600 mt-0.5">Quá 1 ngày sau khi kết thúc buổi</p>
          </div>
          <div
            v-else
            class="px-3 py-2 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-right"
          >
            <p class="font-bold flex items-center gap-1 text-[11px] text-emerald-700">
              <ClockIcon class="w-3.5 h-3.5" /> Có thể điểm danh
            </p>
            <p class="text-[10px] text-emerald-600 mt-0.5">Hạn chót: {{ lockState.deadlineFormatted }}</p>
          </div>

          <!-- Ratio ring -->
          <div class="w-12 h-12 rounded-full border-2 border-orange-400 flex flex-col items-center justify-center font-bold text-xs text-orange-950 bg-white shadow-2xs">
            <span>{{ attendanceRate }}%</span>
            <span class="text-[8px] font-normal text-gray-500">có mặt</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Warning Banner When Attendance is Locked (After 1 day) ──────── -->
    <div
      v-if="lockState.isLocked && currentLesson"
      class="rounded-3xl border border-rose-200 bg-rose-50/90 p-4.5 flex items-start gap-3.5 text-xs text-rose-900 shadow-xs"
    >
      <div class="w-8 h-8 rounded-xl bg-rose-500 text-white flex items-center justify-center flex-shrink-0 mt-0.5 shadow-2xs">
        <LockClosedIcon class="w-4 h-4" />
      </div>
      <div class="flex-1">
        <h4 class="font-bold text-sm text-rose-950">Ràng buộc bảo mật: Đã khóa quyền điểm danh cho buổi học này</h4>
        <p class="text-rose-800 mt-1 leading-relaxed">
          Theo quy chế của trung tâm, giáo viên chỉ được phép ghi nhận điểm danh trong thời gian buổi học diễn ra và <strong>tối đa 1 ngày (24 giờ) sau khi kết thúc buổi học</strong>.
          Buổi học này đã kết thúc vào ngày <strong>{{ lockState.lessonEndTimeFormatted }}</strong> và đã hết hạn chỉnh sửa từ <strong>{{ lockState.deadlineFormatted }}</strong>.
        </p>
        <p class="text-rose-700 mt-1 font-medium">
          Dữ liệu bên dưới đang ở <strong>Chế độ chỉ xem</strong>. Nếu cần điều chỉnh điểm danh học viên cho buổi học này, vui lòng liên hệ Ban Đào Tạo / Quản trị viên (Admin).
        </p>
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
        <!-- Quick Batch Actions (Disabled if locked) -->
        <div class="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            @click="markAllPresent"
            :disabled="students.length === 0 || lockState.isLocked"
            :class="[
              'px-3.5 py-2 rounded-xl text-xs font-bold transition-colors cursor-pointer flex items-center gap-1.5',
              lockState.isLocked
                ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
                : 'bg-emerald-100/80 hover:bg-emerald-200 text-emerald-900 disabled:opacity-50'
            ]"
            :title="lockState.isLocked ? 'Đã hết hạn điểm danh' : 'Đánh dấu tất cả có mặt'"
          >
            <CheckIcon class="w-3.5 h-3.5" />
            <span>Tất cả CÓ MẶT</span>
          </button>

          <button
            type="button"
            @click="autoApplyExcusedForLeaveRequests"
            :disabled="students.length === 0 || countWithLeaveRequest === 0 || lockState.isLocked"
            :class="[
              'px-3.5 py-2 rounded-xl text-xs font-bold transition-colors cursor-pointer flex items-center gap-1.5',
              lockState.isLocked
                ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
                : 'bg-sky-100/80 hover:bg-sky-200 text-sky-900 disabled:opacity-50'
            ]"
            :title="`Có ${countWithLeaveRequest} học sinh gửi đơn xin nghỉ phép`"
          >
            <DocumentTextIcon class="w-3.5 h-3.5 text-sky-700" />
            <span>Duyệt đơn nghỉ phép ({{ countWithLeaveRequest }})</span>
          </button>

          <button
            type="button"
            @click="resetAllToUnmarked"
            :disabled="students.length === 0 || lockState.isLocked"
            class="px-3 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
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
                        v-if="st.status !== 'EXCUSED' && !lockState.isLocked"
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
                    :disabled="lockState.isLocked"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all flex items-center gap-1',
                      lockState.isLocked ? 'cursor-not-allowed opacity-80' : 'cursor-pointer',
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
                    :disabled="lockState.isLocked"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all flex items-center gap-1',
                      lockState.isLocked ? 'cursor-not-allowed opacity-80' : 'cursor-pointer',
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
                    :disabled="lockState.isLocked"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all flex items-center gap-1',
                      lockState.isLocked ? 'cursor-not-allowed opacity-80' : 'cursor-pointer',
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
                    :disabled="lockState.isLocked"
                    :class="[
                      'px-3 py-1.5 rounded-xl font-bold text-xs transition-all flex items-center gap-1',
                      lockState.isLocked ? 'cursor-not-allowed opacity-80' : 'cursor-pointer',
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
                  :disabled="lockState.isLocked"
                  :placeholder="st.status === 'ABSENT' ? 'Lý do vắng mặt...' : 'Ghi chú thêm...'"
                  :class="[
                    'w-full text-xs rounded-xl px-3 py-2 border transition-all focus:outline-none focus:ring-2 focus:ring-orange-300',
                    lockState.isLocked ? 'bg-gray-50 text-gray-500 cursor-not-allowed border-gray-200' :
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
                    :disabled="st.isSavingSingle || st.status === 'UNMARKED' || lockState.isLocked"
                    :class="[
                      'px-2.5 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1',
                      lockState.isLocked ? 'bg-gray-100 text-gray-400 cursor-not-allowed' :
                      st.justSavedSingle
                        ? 'bg-emerald-100 text-emerald-800'
                        : 'bg-orange-100/70 hover:bg-orange-200 text-orange-900 cursor-pointer'
                    ]"
                    :title="lockState.isLocked ? 'Đã hết hạn điểm danh' : (st.status === 'UNMARKED' ? 'Vui lòng chọn trạng thái trước khi lưu lẻ' : 'Lưu riêng học sinh này')"
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
          :disabled="isSavingBatch || !selectedLessonId || lockState.isLocked"
          :class="[
            'inline-flex items-center gap-2 px-6 py-2.5 rounded-xl text-xs font-bold shadow-sm transition-all',
            lockState.isLocked
              ? 'bg-gray-200 text-gray-400 cursor-not-allowed shadow-none'
              : 'bg-orange-500 hover:bg-orange-600 text-white cursor-pointer disabled:opacity-50'
          ]"
        >
          <LockClosedIcon v-if="lockState.isLocked" class="w-4 h-4" />
          <ArrowPathIcon v-else-if="isSavingBatch" class="w-4 h-4 animate-spin" />
          <CheckCircleIcon v-else class="w-4 h-4" />
          <span>{{ lockState.isLocked ? 'Đã khóa điểm danh' : 'Lưu tất cả điểm danh' }}</span>
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
              v-if="!lockState.isLocked"
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
import teacherService from '@/services/teacher.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'
import {
  ChevronRightIcon,
  ArrowPathIcon,
  CheckCircleIcon,
  ExclamationCircleIcon,
  ExclamationTriangleIcon,
  XMarkIcon,
  CalendarDaysIcon,
  ClockIcon,
  DocumentTextIcon,
  MagnifyingGlassIcon,
  CheckIcon,
  LockClosedIcon,
} from '@heroicons/vue/24/outline'

const route = useRoute()
const router = useRouter()
const { profile, fetchProfile } = useUserState()

// ─── Component State ─────────────────────────────────────────────────────────
const classes = ref([])
const lessons = ref([])

const selectedClassId = ref(null)
const selectedLessonId = ref(null)

const lessonDetail = ref(null)
const students = ref([])

const isLoading = ref(false)
const isLoadingClasses = ref(false)
const isLoadingLessons = ref(false)
const isSavingBatch = ref(false)

const searchQuery = ref('')
const statusFilter = ref('ALL') // 'ALL', 'PRESENT', 'LATE', 'EXCUSED', 'ABSENT', 'UNMARKED', 'HAS_LEAVE'
const currentLessonTab = ref('ALL') // 'ALL', 'IN_PROGRESS', 'ENDED', 'COMPLETED', 'UNMARKED'

const lessonTabs = [
  { id: 'ALL', label: 'Tất cả' },
  { id: 'IN_PROGRESS', label: 'Đang diễn ra' },
  { id: 'ENDED', label: 'Đã kết thúc' },
  { id: 'COMPLETED', label: 'Đã điểm danh' },
  { id: 'UNMARKED', label: 'Chưa điểm danh' },
]

// Modal states
const showHistoryModal = ref(false)
const selectedStudentHistory = ref(null)
const studentHistoryList = ref([])
const isLoadingHistory = ref(false)

const showLeaveModal = ref(false)
const selectedLeaveStudent = ref(null)

const feedback = reactive({
  message: '',
  type: 'success', // 'success' | 'warning' | 'error'
})

function showFeedback(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) {
      feedback.message = ''
    }
  }, 4500)
}

// ─── Current Selected Lesson ─────────────────────────────────────────────────
const currentLesson = computed(() => {
  return lessons.value.find(l => l.id === selectedLessonId.value) || null
})

// ─── Lesson Timing Status (Đang diễn ra / Đã kết thúc / Sắp diễn ra) ─────────
function getLessonEndDateTime(les) {
  if (!les) return null
  const d = les.lessonDate ? new Date(les.lessonDate) : new Date()

  if (les.endTime) {
    if (typeof les.endTime === 'string') {
      if (les.endTime.includes('T')) {
        const parsed = new Date(les.endTime)
        if (!isNaN(parsed.getTime())) return parsed
      }
      if (les.endTime.includes(':')) {
        const [hh, mm] = les.endTime.split(':')
        d.setHours(parseInt(hh, 10), parseInt(mm, 10), 0, 0)
        return d
      }
    } else if (les.endTime instanceof Date) {
      return les.endTime
    }
  }

  // Fallback: end of the lesson day (23:59:59)
  d.setHours(23, 59, 59, 999)
  return d
}

function getLessonStartDateTime(les) {
  if (!les) return null
  const d = les.lessonDate ? new Date(les.lessonDate) : new Date()

  if (les.startTime) {
    if (typeof les.startTime === 'string') {
      if (les.startTime.includes('T')) {
        const parsed = new Date(les.startTime)
        if (!isNaN(parsed.getTime())) return parsed
      }
      if (les.startTime.includes(':')) {
        const [hh, mm] = les.startTime.split(':')
        d.setHours(parseInt(hh, 10), parseInt(mm, 10), 0, 0)
        return d
      }
    } else if (les.startTime instanceof Date) {
      return les.startTime
    }
  }

  d.setHours(0, 0, 0, 0)
  return d
}

function calculateLessonTimingStatus(les) {
  const now = new Date()
  const start = getLessonStartDateTime(les)
  const end = getLessonEndDateTime(les)

  if (start && end) {
    if (now >= start && now <= end) {
      return { code: 'IN_PROGRESS', label: 'Đang diễn ra' }
    }
    if (now > end) {
      return { code: 'ENDED', label: 'Đã kết thúc' }
    }
    return { code: 'UPCOMING', label: 'Sắp diễn ra' }
  }

  return { code: 'SCHEDULED', label: 'Theo lịch' }
}

const currentLessonTiming = computed(() => {
  if (!currentLesson.value) return { code: 'NONE', label: 'Chưa chọn' }
  return calculateLessonTimingStatus(currentLesson.value)
})

// ─── Attendance Status: Đã điểm danh / Chưa điểm danh ───────────────────────
const currentLessonAttendanceStatus = computed(() => {
  if (!lessonDetail.value || students.value.length === 0) {
    return { code: 'UNMARKED', label: 'Chưa điểm danh' }
  }
  const unmarked = stats.value.unmarked
  const total = stats.value.total
  if (unmarked === 0 && total > 0) {
    return { code: 'COMPLETED', label: `Đã điểm danh (${total}/${total})` }
  }
  if (unmarked < total && unmarked > 0) {
    return { code: 'PARTIAL', label: `Đang điểm danh (${total - unmarked}/${total})` }
  }
  return { code: 'UNMARKED', label: `Chưa điểm danh (${total} HS)` }
})

// ─── RÀNG BUỘC: Giáo viên chỉ có thể điểm danh đến sau khi kết thúc buổi 1 ngày ──
const ONE_DAY_MS = 24 * 60 * 60 * 1000

const lockState = computed(() => {
  if (!currentLesson.value) {
    return { isLocked: false, deadlineFormatted: '', lessonEndTimeFormatted: '' }
  }

  const end = getLessonEndDateTime(currentLesson.value)
  if (!end) {
    return { isLocked: false, deadlineFormatted: '', lessonEndTimeFormatted: '' }
  }

  const deadline = new Date(end.getTime() + ONE_DAY_MS)
  const now = new Date()
  const isLocked = now > deadline

  return {
    isLocked,
    deadline,
    deadlineFormatted: formatDateTime(deadline),
    lessonEndTimeFormatted: formatDateTime(end),
  }
})

// ─── Lessons Filter by Tabs (Tất cả, Đang diễn ra, Đã kết thúc...) ───────────
const filteredLessonsByTab = computed(() => {
  const tab = currentLessonTab.value
  if (tab === 'ALL') return lessons.value

  return lessons.value.filter(les => {
    const timing = les.timingStatus?.code
    if (tab === 'IN_PROGRESS') return timing === 'IN_PROGRESS'
    if (tab === 'ENDED') return timing === 'ENDED'
    if (tab === 'COMPLETED') return les.isAttendanceCompleted
    if (tab === 'UNMARKED') return !les.isAttendanceCompleted
    return true
  })
})

// ─── Statistics Counters ─────────────────────────────────────────────────────
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

  if (statusFilter.value === 'HAS_LEAVE') {
    list = list.filter(s => s.hasLeaveRequest)
  } else if (statusFilter.value !== 'ALL') {
    list = list.filter(s => s.status === statusFilter.value)
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(s =>
      s.studentName.toLowerCase().includes(q) ||
      String(s.studentId).includes(q)
    )
  }

  return list
})

// ─── Lifecycle & Load Data ───────────────────────────────────────────────────
onMounted(async () => {
  await loadClasses()

  if (route.query.lessonId) {
    const lId = parseInt(route.query.lessonId, 10)
    if (!isNaN(lId)) {
      selectedLessonId.value = lId
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

// ─── 1. Load Classes: CHỈ NHỮNG LỚP MÀ GIÁO VIÊN ĐƯỢC PHÂN CÔNG ─────────────
async function loadClasses() {
  isLoadingClasses.value = true
  try {
    const [allClassesData, userProfile] = await Promise.all([
      classService.getAll(),
      fetchProfile()
    ])

    const rawClasses = Array.isArray(allClassesData) ? allClassesData : (allClassesData.items || [])
    
    // Determine current teacher identifier
    const role = (userProfile?.role || '').toLowerCase()
    let teacherProfileId = getProfileId()
    const teacherFullName = (userProfile?.fullName || '').trim().toLowerCase()
    const teacherPhone = (userProfile?.phone || '').trim()

    // If teacherProfileId is not in storage, resolve from teacherService
    if (!teacherProfileId && role === 'teacher') {
      try {
        const teachersList = await teacherService.getAll()
        const rawTeachers = Array.isArray(teachersList) ? teachersList : (teachersList.items || [])
        const matched = rawTeachers.find(t =>
          (teacherPhone && t.phone === teacherPhone) ||
          (teacherFullName && t.fullName && t.fullName.trim().toLowerCase() === teacherFullName) ||
          (userProfile?.accountId && t.accountId === userProfile.accountId)
        )
        if (matched) {
          teacherProfileId = matched.id
        }
      } catch (err) {
        console.warn('Không thể tra cứu hồ sơ giáo viên:', err)
      }
    }

    // Filter only assigned classes
    const assigned = rawClasses.filter(c => {
      // Admin has permission to view all classes
      if (role === 'admin') return true

      // Match by teacherId (TeacherProfile.Id)
      if (teacherProfileId && c.teacherId === teacherProfileId) return true

      // Match by teacherName
      if (teacherFullName && c.teacherName && c.teacherName.trim().toLowerCase() === teacherFullName) return true

      return false
    })

    classes.value = assigned.length > 0 ? assigned : (role === 'admin' ? rawClasses : [])

    if (classes.value.length > 0 && !selectedClassId.value) {
      selectedClassId.value = classes.value[0].id
      await loadLessonsByClass(selectedClassId.value)
    }
  } catch (err) {
    console.warn('Lỗi khi tải danh sách lớp học:', err)
  } finally {
    isLoadingClasses.value = false
  }
}

// ─── 2. Load Lessons: SORT THEO NGÀY & ĐÁNH SỐ BUỔI 1 -> BUỔI 24 ────────────
async function loadLessonsByClass(classId) {
  if (!classId) return
  isLoadingLessons.value = true
  lessons.value = []
  try {
    const data = await lessonService.getByClass(classId)
    const rawLessons = Array.isArray(data) ? data : (data.items || [])

    // 1. Sort ascending by date (ngày cũ nhất -> mới nhất)
    const sorted = [...rawLessons].sort((a, b) => {
      const timeA = new Date(a.lessonDate || a.date).getTime()
      const timeB = new Date(b.lessonDate || b.date).getTime()
      return timeA - timeB
    })

    // 2. Number sequentially: Buổi 1 -> Buổi 24
    lessons.value = sorted.map((les, index) => {
      const lessonNumber = index + 1
      const label = `Buổi ${lessonNumber}`
      const timingStatus = calculateLessonTimingStatus(les)
      return {
        ...les,
        lessonNumber,
        label,
        timingStatus,
        displayName: `${label} - ${formatDate(les.lessonDate)}`,
        isAttendanceCompleted: les.status === 'COMPLETED'
      }
    })

    if (lessons.value.length > 0 && !selectedLessonId.value) {
      // Find today or next upcoming lesson, otherwise default to first
      const todayLesson = lessons.value.find(l => l.timingStatus.code === 'IN_PROGRESS') || lessons.value[0]
      selectedLessonId.value = todayLesson.id
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

function reloadCurrentLesson() {
  if (selectedLessonId.value) {
    fetchAttendance(selectedLessonId.value)
  }
}

// ─── GET /api/attendance/lesson/{lessonId} ───────────────────────────────────
async function fetchAttendance(lessonId) {
  if (!lessonId) return
  isLoading.value = true
  try {
    const res = await attendanceService.getByLesson(lessonId)
    lessonDetail.value = res

    if (res.classId && selectedClassId.value !== res.classId) {
      selectedClassId.value = res.classId
    }

    students.value = (res.students || []).map(st => ({
      ...st,
      status: st.status || 'UNMARKED',
      teacherNotes: st.teacherNotes || '',
      isSavingSingle: false,
      justSavedSingle: false,
    }))

    // If attendance is locked, show a notice
    if (lockState.value.isLocked) {
      showFeedback('Buổi học này đã kết thúc hơn 1 ngày. Quyền điểm danh đã khóa (Chế độ chỉ xem).', 'warning')
    }
  } catch (err) {
    const msg = err.response?.data?.message || `Không thể tải thông tin điểm danh buổi #${lessonId}.`
    showFeedback(msg, 'error')
  } finally {
    isLoading.value = false
  }
}

// ─── Status change helpers ───────────────────────────────────────────────────
function setStudentStatus(student, status) {
  if (lockState.value.isLocked) {
    showFeedback('Đã hết hạn điểm danh cho buổi học này.', 'warning')
    return
  }
  student.status = status
  student.justSavedSingle = false
}

function markAllPresent() {
  if (lockState.value.isLocked) {
    showFeedback('Đã hết hạn điểm danh cho buổi học này.', 'warning')
    return
  }
  students.value.forEach(st => {
    st.status = 'PRESENT'
    st.justSavedSingle = false
  })
  showFeedback('Đã đánh dấu tất cả học sinh là CÓ MẶT.', 'success')
}

function autoApplyExcusedForLeaveRequests() {
  if (lockState.value.isLocked) {
    showFeedback('Đã hết hạn điểm danh cho buổi học này.', 'warning')
    return
  }
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
  if (lockState.value.isLocked) {
    showFeedback('Đã hết hạn điểm danh cho buổi học này.', 'warning')
    return
  }
  students.value.forEach(st => {
    st.status = 'UNMARKED'
    st.justSavedSingle = false
  })
}

// ─── POST /api/attendance/lesson/{lessonId} (Save All Batch) ─────────────────
async function handleSaveBatchAttendance() {
  if (!selectedLessonId.value) return

  if (lockState.value.isLocked) {
    showFeedback('Đã quá thời hạn điểm danh cho buổi học này (quá 1 ngày). Không thể lưu.', 'error')
    return
  }

  const unassigned = students.value.filter(s => !s.status || s.status === 'UNMARKED')
  if (unassigned.length > 0) {
    if (!confirm(`Còn ${unassigned.length} học sinh chưa được đánh dấu điểm danh. Các em này sẽ được ghi nhận là VẮNG MẶT. Bạn có muốn tiếp tục lưu?`)) {
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
    const res = await attendanceService.saveBatchByLesson(selectedLessonId.value, items)
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

// ─── PUT /api/attendance/lesson/{lessonId}/student/{studentId} (Save Single) ─
async function handleSaveSingle(student) {
  if (!selectedLessonId.value || !student.studentId) return

  if (lockState.value.isLocked) {
    showFeedback('Đã hết thời hạn điểm danh. Không thể cập nhật.', 'error')
    return
  }

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

// ─── GET /api/attendance/student/{studentId}?classId={classId} (History) ────
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
  if (lockState.value.isLocked) {
    showFeedback('Đã hết hạn điểm danh cho buổi học này.', 'warning')
    return
  }
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

function formatDateTime(d) {
  if (!d) return ''
  return `${d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false })} ngày ${d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })}`
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
