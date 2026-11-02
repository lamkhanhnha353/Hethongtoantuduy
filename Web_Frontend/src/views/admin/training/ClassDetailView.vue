<template>
  <div class="space-y-6">
    <!-- ─── Loading State ─────────────────────────────────────────────────── -->
    <LoadingSpinner v-if="loading" label="Đang tải thông tin chi tiết lớp học..." />

    <!-- ─── Content ───────────────────────────────────────────────────────── -->
    <template v-else-if="cls">
      <!-- ─── Breadcrumb ───────────────────────────────────────────────── -->
      <nav class="flex items-center gap-2 text-sm text-gray-400 flex-wrap">
        <router-link to="/training/classes" class="hover:text-orange-600 transition-colors flex items-center gap-1 font-medium">
          <UserGroupIcon class="w-4 h-4 text-orange-500" />
          Danh sách Lớp học
        </router-link>
        <ChevronRightIcon class="w-4 h-4" />
        <router-link
          v-if="course"
          :to="{ name: 'course-detail', params: { id: course.id } }"
          class="hover:text-orange-600 transition-colors flex items-center gap-1 font-medium"
        >
          <BookOpenIcon class="w-4 h-4 text-orange-500" />
          {{ course.name }}
        </router-link>
        <ChevronRightIcon v-if="course" class="w-4 h-4" />
        <span class="text-orange-950 font-bold truncate">{{ cls.name }}</span>
      </nav>

      <!-- ─── Toast / Sync notification banner ────────────────────────── -->
      <transition name="slide-down">
        <div
          v-if="syncAlertMessage"
          :class="[
            'p-3.5 rounded-2xl border text-xs flex items-center justify-between gap-3 shadow-xs transition-all',
            syncAlertSuccess
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-amber-50 border-amber-200 text-amber-800'
          ]"
        >
          <div class="flex items-center gap-2">
            <CheckCircleIcon v-if="syncAlertSuccess" class="w-5 h-5 text-emerald-600 flex-shrink-0" />
            <InformationCircleIcon v-else class="w-5 h-5 text-amber-600 flex-shrink-0" />
            <span class="font-medium leading-relaxed">{{ syncAlertMessage }}</span>
          </div>
          <button
            @click="syncAlertMessage = ''"
            class="text-gray-400 hover:text-gray-600 p-1 rounded-md cursor-pointer"
          >
            <XMarkIcon class="w-4 h-4" />
          </button>
        </div>
      </transition>

      <!-- ─── Class Hero Banner & Action Header ────────────────────────── -->
      <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
        <div class="h-2 w-full bg-gradient-to-r from-orange-400 via-amber-500 to-orange-500" />
        <div class="p-6">
          <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
            <div class="flex-1 min-w-0">
              <!-- Badges row -->
              <div class="flex flex-wrap items-center gap-2 mb-2">
                <span class="text-xs font-mono font-bold text-orange-700 bg-orange-100 px-3 py-1 rounded-full border border-orange-200">
                  {{ cls.code ?? `LH-${cls.id}` }}
                </span>
                <StatusBadge :status="cls.status" />
                <span v-if="cls.courseName" class="text-xs text-orange-800 bg-orange-100 px-2.5 py-0.5 rounded-full border border-orange-300 font-bold">
                  Khóa: {{ cls.courseName }}
                </span>
                <span v-if="cls.courseLevelName" class="text-xs text-orange-800 bg-orange-50 px-2.5 py-0.5 rounded-full border border-orange-200 font-semibold">
                  Cấp độ: {{ cls.courseLevelName }}
                </span>
                <span v-if="level?.totalSessions" class="text-xs text-purple-800 bg-purple-50 px-2.5 py-0.5 rounded-full border border-purple-200 font-semibold">
                  Chuẩn khóa học: {{ level.totalSessions }} buổi
                </span>
              </div>

              <!-- Class Name -->
              <h1 class="text-2xl sm:text-3xl font-extrabold text-orange-950 mb-2 leading-tight">
                {{ cls.name }}
              </h1>

              <!-- Teacher & Room quick info -->
              <div class="flex flex-wrap items-center gap-4 text-xs text-gray-500">
                <div class="flex items-center gap-1.5 font-medium">
                  <AcademicCapIcon class="w-4 h-4 text-orange-500" />
                  <span>Giảng viên: <strong class="text-gray-800">{{ cls.teacherName || 'Chưa phân công' }}</strong></span>
                </div>
                <div v-if="cls.startDate" class="flex items-center gap-1.5 font-medium">
                  <CalendarDaysIcon class="w-4 h-4 text-orange-500" />
                  <span>Khai giảng: <strong class="text-gray-800">{{ formatDate(cls.startDate) }}</strong></span>
                </div>
                <div class="flex items-center gap-1.5 font-medium">
                  <ClockIcon class="w-4 h-4 text-orange-500" />
                  <span>
                    Bế giảng dự kiến:
                    <strong class="text-orange-900 font-bold">
                      {{ cls.endDate ? formatDate(cls.endDate) : (calculatedEndDateDisplay || 'Đang tính toán') }}
                    </strong>
                  </span>
                </div>
              </div>
            </div>

            <!-- Top Actions -->
            <div class="flex items-center gap-2.5 flex-wrap flex-shrink-0">
              <!-- Nút Phân công GV -->
              <button
                @click="openAssignTeacherModal"
                class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-purple-700 bg-purple-50 hover:bg-purple-100 border border-purple-200 rounded-xl transition-colors cursor-pointer"
                title="Phân công giáo viên giảng dạy từ danh sách teacher.service"
              >
                <AcademicCapIcon class="w-4 h-4 text-purple-600" />
                <span>{{ cls.teacherName ? 'Đổi giáo viên' : 'Phân công GV' }}</span>
              </button>

              <button
                @click="triggerManualSync"
                :disabled="isSyncing"
                class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 rounded-xl transition-colors cursor-pointer disabled:opacity-50"
                title="Tự động tính toán lại ngày kết thúc và đồng bộ buổi học không vượt quá khóa học"
              >
                <ArrowPathIcon :class="['w-4 h-4 text-emerald-600', isSyncing ? 'animate-spin' : '']" />
                <span>{{ isSyncing ? 'Đang đồng bộ...' : 'Đồng bộ & Tính ngày kết thúc' }}</span>
              </button>

              <button
                @click="openEditClassModal"
                class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold text-orange-700 bg-orange-100 hover:bg-orange-200 rounded-xl transition-colors cursor-pointer"
              >
                <PencilSquareIcon class="w-4 h-4" />
                <span>Chỉnh sửa lớp học</span>
              </button>

              <button
                @click="openAddScheduleModal"
                class="inline-flex items-center gap-1.5 px-4 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-sm transition-colors cursor-pointer"
              >
                <PlusIcon class="w-4 h-4" />
                <span>Thêm lịch tuần</span>
              </button>
            </div>
          </div>

          <!-- Quick Metrics Banner -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-6 pt-5 border-t border-orange-100">
            <!-- Capacity -->
            <div class="p-3.5 bg-orange-50/60 rounded-xl border border-orange-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1">
                  <UserGroupIcon class="w-4 h-4 text-orange-500" />
                  Sĩ số lớp
                </span>
                <span class="font-bold text-orange-600">{{ Math.round(capacityFillPct) }}%</span>
              </div>
              <p class="text-base font-extrabold text-gray-900">
                {{ activeCount }} / {{ maxCapacityDisplay }} <span class="text-xs font-normal text-gray-500">học viên</span>
              </p>
              <div class="h-1.5 bg-orange-100 rounded-full overflow-hidden mt-2">
                <div
                  :class="['h-full rounded-full transition-all duration-500', capacityFillPct >= 90 ? 'bg-red-500' : 'bg-orange-500']"
                  :style="{ width: capacityFillPct + '%' }"
                />
              </div>
            </div>

            <!-- Sessions progress vs totalSessions -->
            <div class="p-3.5 bg-blue-50/60 rounded-xl border border-blue-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-blue-700">
                  <CalendarDaysIcon class="w-4 h-4 text-blue-500" />
                  Số buổi học (Lessons)
                </span>
                <span class="font-bold text-blue-700">
                  {{ lessons.length }} / {{ level?.totalSessions ?? '?' }}
                </span>
              </div>
              <p class="text-base font-extrabold text-blue-950">
                {{ completedLessonsCount }} <span class="text-xs font-normal text-gray-500">đã hoàn thành</span>
              </p>
              <div class="h-1.5 bg-blue-100 rounded-full overflow-hidden mt-2">
                <div
                  class="h-full bg-blue-500 rounded-full transition-all duration-500"
                  :style="{ width: sessionsProgressPct + '%' }"
                />
              </div>
            </div>

            <!-- Fee & Level -->
            <div class="p-3.5 bg-green-50/60 rounded-xl border border-green-100">
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-green-700">
                  <CurrencyDollarIcon class="w-4 h-4 text-green-600" />
                  Học phí khóa học
                </span>
              </div>
              <p class="text-base font-extrabold text-green-700">
                {{ level?.fee ? formatFee(level.fee) : 'Liên hệ' }}
              </p>
              <p class="text-[11px] text-green-600 mt-1">
                {{ level?.totalSessions ?? 0 }} buổi đào tạo tiêu chuẩn
              </p>
            </div>

            <!-- Teacher -->
            <div
              @click="openAssignTeacherModal"
              class="p-3.5 bg-purple-50/60 hover:bg-purple-100/70 rounded-xl border border-purple-100 transition-colors cursor-pointer group"
              title="Bấm để phân công hoặc đổi giáo viên"
            >
              <div class="flex items-center justify-between text-xs text-gray-500 mb-1">
                <span class="flex items-center gap-1 text-purple-700 font-semibold">
                  <AcademicCapIcon class="w-4 h-4 text-purple-500" />
                  Giáo viên
                </span>
                <span class="text-[10px] text-purple-600 group-hover:underline">Đổi GV →</span>
              </div>
              <p class="text-sm font-bold text-purple-950 truncate">
                {{ cls.teacherName || 'Chưa phân công' }}
              </p>
              <p class="text-[11px] text-purple-600 mt-1 truncate">
                {{ teacher?.phone || teacher?.email || 'Bấm để phân công' }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Grid: Course Info (Left) + Schedules/Lessons/Students (Right) ─ -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <!-- ─── LEFT: Associated Course & Level Info (5 cols) ──────────── -->
        <div class="lg:col-span-5 space-y-6">
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-4">
            <div class="flex items-center justify-between pb-3 border-b border-orange-100">
              <h2 class="font-bold text-orange-950 text-base flex items-center gap-2">
                <BookOpenIcon class="w-5 h-5 text-orange-500" />
                Khóa học & Cấp độ Đào tạo
              </h2>
              <router-link
                v-if="course"
                :to="{ name: 'course-detail', params: { id: course.id } }"
                class="text-xs font-semibold text-orange-600 hover:text-orange-700 hover:underline flex items-center gap-1"
              >
                <span>Xem chi tiết</span>
                <ChevronRightIcon class="w-3.5 h-3.5" />
              </router-link>
            </div>

            <!-- Course Cover & Title -->
            <div v-if="course" class="space-y-3">
              <div v-if="course.imageUrl" class="w-full h-36 rounded-xl overflow-hidden bg-orange-50 border border-orange-100">
                <img :src="course.imageUrl" :alt="course.name" class="w-full h-full object-cover" />
              </div>

              <div>
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-[11px] font-mono font-bold text-orange-700 bg-orange-100 px-2 py-0.5 rounded">
                    {{ course.code }}
                  </span>
                  <StatusBadge :status="course.status" />
                </div>
                <h3 class="font-bold text-gray-900 text-base leading-snug">
                  {{ course.name }}
                </h3>
                <p v-if="course.description" class="text-xs text-gray-500 mt-1.5 leading-relaxed line-clamp-3">
                  {{ course.description }}
                </p>
              </div>

              <!-- Level Detail Card -->
              <div v-if="level" class="p-4 bg-orange-50/50 rounded-xl border border-orange-100 space-y-2.5">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-bold text-orange-900 flex items-center gap-1.5">
                    <AcademicCapIcon class="w-4 h-4 text-orange-500" />
                    Cấp độ: {{ level.name }}
                  </span>
                  <span class="text-[11px] font-mono font-bold text-orange-600 bg-white px-2 py-0.5 rounded border border-orange-200">
                    {{ level.code }}
                  </span>
                </div>
                <div class="grid grid-cols-2 gap-2 text-xs text-gray-600 pt-1 border-t border-orange-100">
                  <div>
                    <span class="text-gray-400 block text-[10px]">Học phí</span>
                    <strong class="text-orange-600 font-bold">{{ formatFee(level.fee) }}</strong>
                  </div>
                  <div>
                    <span class="text-gray-400 block text-[10px]">Thời lượng</span>
                    <strong>{{ level.totalSessions || 0 }} buổi học</strong>
                  </div>
                  <div v-if="course.minAge || course.maxAge" class="col-span-2">
                    <span class="text-gray-400 block text-[10px]">Độ tuổi phù hợp</span>
                    <strong>{{ course.minAge }} - {{ course.maxAge }} tuổi</strong>
                  </div>
                </div>
              </div>

              <!-- Benefits if any -->
              <div v-if="courseBenefits.length > 0" class="pt-2">
                <h4 class="text-xs font-bold text-gray-700 mb-2 flex items-center gap-1.5">
                  <SparklesIcon class="w-4 h-4 text-amber-500" />
                  Mục tiêu & Điểm nổi bật
                </h4>
                <ul class="space-y-1.5 text-xs text-gray-600">
                  <li v-for="(b, idx) in courseBenefits.slice(0, 4)" :key="idx" class="flex items-start gap-2">
                    <CheckCircleIcon class="w-4 h-4 text-green-500 flex-shrink-0 mt-0.5" />
                    <span>{{ b }}</span>
                  </li>
                </ul>
              </div>
            </div>

            <!-- Fallback if course data not loaded -->
            <div v-else class="text-xs text-gray-400 py-4 text-center">
              Đang liên kết dữ liệu khóa học...
            </div>
          </div>

          <!-- Teacher Details Card (Phân công giáo viên từ teacher.service) -->
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-6 space-y-3">
            <div class="flex items-center justify-between pb-3 border-b border-orange-100">
              <h2 class="font-bold text-orange-950 text-base flex items-center gap-2">
                <AcademicCapIcon class="w-5 h-5 text-orange-500" />
                Giáo viên Phụ trách
              </h2>
              <button
                @click="openAssignTeacherModal"
                class="inline-flex items-center gap-1 text-xs font-semibold text-purple-700 hover:text-purple-900 hover:bg-purple-50 px-2.5 py-1 rounded-lg transition-colors cursor-pointer"
              >
                <PencilSquareIcon class="w-3.5 h-3.5" />
                <span>{{ cls.teacherName ? 'Đổi giáo viên' : 'Phân công' }}</span>
              </button>
            </div>

            <div v-if="cls.teacherName" class="flex items-center gap-3.5">
              <div class="w-12 h-12 rounded-2xl bg-gradient-to-tr from-purple-500 to-indigo-600 text-white font-bold flex items-center justify-center text-lg shadow-sm">
                {{ cls.teacherName.charAt(0) }}
              </div>
              <div class="min-w-0 flex-1">
                <h4 class="font-bold text-gray-900 text-sm truncate">{{ cls.teacherName }}</h4>
                <p class="text-xs text-gray-500 mt-0.5">Giảng viên bộ môn Toán Tư Duy</p>
                <p v-if="teacher?.phone" class="text-xs text-purple-700 font-mono mt-0.5">
                  SĐT: {{ teacher.phone }}
                </p>
                <p v-if="teacher?.email" class="text-xs text-gray-400 truncate mt-0.5">
                  Email: {{ teacher.email }}
                </p>
              </div>
            </div>

            <div v-else class="p-4 bg-gray-50 rounded-xl text-center text-xs text-gray-400">
              Lớp học chưa được chỉ định giáo viên phụ trách.
              <button
                @click="openAssignTeacherModal"
                class="block mx-auto mt-2 text-purple-700 font-bold hover:underline cursor-pointer"
              >
                + Phân công giáo viên ngay
              </button>
            </div>
          </div>
        </div>

        <!-- ─── RIGHT: Schedules, Lessons & Students Tabs (7 cols) ───────── -->
        <div class="lg:col-span-7 space-y-6">
          <!-- Main Tabs Controller -->
          <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
            <!-- Tab Buttons Header -->
            <div class="px-6 pt-4 border-b border-orange-100 flex items-center justify-between flex-wrap gap-3 bg-gradient-to-r from-orange-50/40 to-white">
              <div class="flex items-center gap-2 flex-wrap">
                <!-- Tab Lịch tuần -->
                <button
                  @click="activeTab = 'schedules'"
                  :class="[
                    'pb-3 px-3 text-xs font-bold border-b-2 transition-all flex items-center gap-2 cursor-pointer',
                    activeTab === 'schedules'
                      ? 'border-orange-500 text-orange-600'
                      : 'border-transparent text-gray-500 hover:text-orange-500'
                  ]"
                >
                  <CalendarDaysIcon class="w-4 h-4 text-orange-500" />
                  <span>Lịch tuần ({{ schedules.length }} ca/tuần)</span>
                </button>

                <!-- Tab Buổi học -->
                <button
                  @click="activeTab = 'lessons'"
                  :class="[
                    'pb-3 px-3 text-xs font-bold border-b-2 transition-all flex items-center gap-2 cursor-pointer',
                    activeTab === 'lessons'
                      ? 'border-orange-500 text-orange-600'
                      : 'border-transparent text-gray-500 hover:text-orange-500'
                  ]"
                >
                  <ClockIcon class="w-4 h-4 text-orange-500" />
                  <span>Danh sách Buổi học ({{ lessons.length }}/{{ level?.totalSessions || '?' }})</span>
                </button>

                <!-- Tab Danh sách Học viên hiện tại -->
                <button
                  @click="activeTab = 'students'"
                  :class="[
                    'pb-3 px-3 text-xs font-bold border-b-2 transition-all flex items-center gap-2 cursor-pointer',
                    activeTab === 'students'
                      ? 'border-orange-500 text-orange-600'
                      : 'border-transparent text-gray-500 hover:text-orange-500'
                  ]"
                >
                  <UserGroupIcon class="w-4 h-4 text-orange-500" />
                  <span>Danh sách Học viên ({{ classStudents.length }})</span>
                </button>
              </div>

              <!-- Quick action in tab header -->
              <div class="pb-2.5 flex items-center gap-2">
                <button
                  v-if="activeTab === 'schedules'"
                  @click="openAddScheduleModal"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                >
                  <PlusIcon class="w-3.5 h-3.5" />
                  <span>Thêm lịch</span>
                </button>

                <button
                  v-else-if="activeTab === 'lessons'"
                  @click="openAddLessonModal"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                >
                  <PlusIcon class="w-3.5 h-3.5" />
                  <span>Thêm buổi học</span>
                </button>

                <button
                  v-else-if="activeTab === 'students'"
                  @click="openAddStudentModal"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
                >
                  <UserPlusIcon class="w-3.5 h-3.5" />
                  <span>Thêm học viên</span>
                </button>
              </div>
            </div>

            <!-- Tab 1: SCHEDULES (Lịch học theo tuần) -->
            <div v-if="activeTab === 'schedules'" class="p-6 space-y-5">
              <!-- Loading schedules -->
              <LoadingSpinner v-if="loadingSchedules" label="Đang tải danh sách lịch học..." />

              <!-- Empty Schedules -->
              <EmptyState
                v-else-if="schedules.length === 0"
                :icon="CalendarDaysIcon"
                message="Lớp học chưa được xếp lịch học"
                sub="Bấm nút 'Thêm lịch' phía trên để thiết lập các ngày và khung giờ học trong tuần"
              />

              <!-- Schedules Table / List -->
              <div v-else class="space-y-3">
                <div
                  v-for="item in sortedSchedules"
                  :key="item.id"
                  class="p-4 rounded-xl border border-orange-100 bg-white hover:border-orange-300 hover:shadow-xs transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                >
                  <!-- Day & Time -->
                  <div class="flex items-center gap-3.5">
                    <div :class="['w-16 h-12 rounded-xl flex flex-col items-center justify-center font-bold text-xs border flex-shrink-0', getDayBadge(item.dayOfWeek).color]">
                      <span>{{ getDayBadge(item.dayOfWeek).label }}</span>
                      <span class="text-[9px] font-normal opacity-80">{{ getDayBadge(item.dayOfWeek).full }}</span>
                    </div>

                    <div>
                      <div class="flex items-center gap-2">
                        <ClockIcon class="w-4 h-4 text-orange-500 flex-shrink-0" />
                        <span class="font-mono font-extrabold text-sm text-gray-900">
                          {{ formatTime(item.startTime) }} → {{ formatTime(item.endTime) }}
                        </span>
                        <span class="text-[10px] text-orange-700 bg-orange-100 font-semibold px-2 py-0.5 rounded-full">
                          {{ calcDuration(item.startTime, item.endTime) }}
                        </span>
                      </div>

                      <div class="flex items-center gap-1.5 text-xs text-gray-500 mt-1">
                        <MapPinIcon class="w-3.5 h-3.5 text-orange-400" />
                        <span>Phòng: <strong class="text-gray-700">{{ item.room || 'Chưa xếp phòng' }}</strong></span>
                      </div>
                    </div>
                  </div>

                  <!-- Schedule Actions -->
                  <div class="flex items-center gap-1.5 self-end sm:self-center">
                    <button
                      @click="openEditScheduleModal(item)"
                      class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-gray-600 hover:text-orange-600 hover:bg-orange-50 rounded-lg transition-colors cursor-pointer"
                      title="Chỉnh sửa lịch học này"
                    >
                      <PencilSquareIcon class="w-3.5 h-3.5 text-orange-500" />
                      <span>Sửa</span>
                    </button>

                    <button
                      @click="confirmDeleteSchedule(item)"
                      class="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-semibold text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                      title="Xóa lịch học này"
                    >
                      <TrashIcon class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              </div>

              <!-- Weekly Schedule Visual Map -->
              <div class="pt-3 border-t border-orange-100 space-y-3">
                <h3 class="font-bold text-orange-950 text-sm flex items-center gap-2">
                  <CalendarIcon class="w-4 h-4 text-orange-500" />
                  Thời khóa biểu tuần trực quan
                </h3>

                <div class="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-2">
                  <div
                    v-for="d in DAYS_OF_WEEK"
                    :key="d.value"
                    :class="[
                      'p-2.5 rounded-xl border text-center transition-all flex flex-col justify-between min-h-[100px]',
                      hasScheduleOnDay(d.value)
                        ? 'border-orange-300 bg-orange-50/70 shadow-2xs'
                        : 'border-gray-100 bg-gray-50/50 opacity-60'
                    ]"
                  >
                    <div>
                      <span class="block text-xs font-bold text-gray-700">{{ d.label }}</span>
                      <span class="block text-[10px] text-gray-400">{{ d.full }}</span>
                    </div>

                    <div v-if="hasScheduleOnDay(d.value)" class="my-1.5 space-y-1">
                      <div
                        v-for="sch in getSchedulesOnDay(d.value)"
                        :key="sch.id"
                        @click="openEditScheduleModal(sch)"
                        class="bg-white border border-orange-200 p-1 rounded-md text-left shadow-2xs hover:border-orange-400 cursor-pointer transition-all"
                      >
                        <p class="text-[10px] font-mono font-bold text-orange-700 leading-tight">
                          {{ formatTime(sch.startTime) }}
                        </p>
                        <p v-if="sch.room" class="text-[9px] text-gray-500 truncate mt-0.5">
                          {{ sch.room }}
                        </p>
                      </div>
                    </div>
                    <div v-else class="text-[10px] text-gray-400 italic my-auto">
                      Nghỉ
                    </div>

                    <div class="pt-1 border-t border-gray-100/80">
                      <span
                        v-if="hasScheduleOnDay(d.value)"
                        class="text-[10px] font-bold text-orange-600"
                      >
                        {{ getSchedulesOnDay(d.value).length }} ca
                      </span>
                      <span v-else class="text-[10px] text-gray-300">-</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Tab 2: LESSONS (Danh sách từng buổi học cụ thể) -->
            <div v-else-if="activeTab === 'lessons'" class="p-6 space-y-4">
              <!-- Helper status banner -->
              <div class="flex items-center justify-between flex-wrap gap-2 p-3 bg-orange-50/70 rounded-xl border border-orange-200 text-xs">
                <div>
                  <span class="font-bold text-orange-950">
                    Khóa học đặt chuẩn: {{ level?.totalSessions ?? 0 }} buổi học
                  </span>
                  <span class="text-gray-600 block sm:inline sm:ml-2">
                    (Hiện có: <strong class="text-orange-700">{{ lessons.length }} buổi</strong>, ngày bế giảng: <strong>{{ cls.endDate ? formatDate(cls.endDate) : '--' }}</strong>)
                  </span>
                </div>

                <div v-if="lessons.length > (level?.totalSessions || 0)" class="text-red-600 font-bold flex items-center gap-1">
                  <ExclamationCircleIcon class="w-4 h-4" />
                  <span>Vượt quá {{ lessons.length - level.totalSessions }} buổi!</span>
                </div>
                <div v-else-if="lessons.length === (level?.totalSessions || 0)" class="text-emerald-700 font-bold flex items-center gap-1">
                  <CheckCircleIcon class="w-4 h-4" />
                  <span>Đã khớp đủ số lượng buổi học</span>
                </div>
              </div>

              <!-- Loading lessons -->
              <LoadingSpinner v-if="loadingLessons" label="Đang tải danh sách các buổi học..." />

              <!-- Empty lessons -->
              <EmptyState
                v-else-if="lessons.length === 0"
                :icon="CalendarDaysIcon"
                message="Lớp học chưa có dữ liệu buổi học (Lessons)"
                sub="Bấm nút 'Đồng bộ & Tính ngày kết thúc' để tự động tạo danh sách buổi học theo số buổi của khóa học"
              />

              <!-- Lessons List -->
              <div v-else class="space-y-2.5 max-h-[500px] overflow-y-auto pr-1">
                <div
                  v-for="(l, idx) in sortedLessons"
                  :key="l.id"
                  class="p-3.5 rounded-xl border border-orange-100 bg-white hover:border-orange-300 hover:shadow-2xs transition-all flex items-center justify-between gap-3"
                >
                  <!-- Session Index & Date -->
                  <div class="flex items-center gap-3">
                    <span class="w-8 h-8 rounded-lg bg-orange-100 text-orange-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
                      #{{ idx + 1 }}
                    </span>

                    <div>
                      <div class="flex items-center gap-2 flex-wrap">
                        <span class="font-bold text-gray-900 text-sm">
                          {{ formatDate(l.lessonDate) }}
                        </span>
                        <span :class="['px-2 py-0.5 text-[10px] font-bold rounded-full border', getLessonStatusBadge(l.status).color]">
                          {{ getLessonStatusBadge(l.status).label }}
                        </span>
                        <span v-if="l.isImportan" class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-red-100 text-red-700 border border-red-200">
                          Buổi quan trọng
                        </span>
                      </div>

                      <div class="flex items-center gap-3 text-xs text-gray-500 mt-1">
                        <span class="flex items-center gap-1 font-mono">
                          <ClockIcon class="w-3.5 h-3.5 text-orange-400" />
                          {{ formatTime(l.startTime) }} - {{ formatTime(l.endTime) }}
                        </span>
                        <span v-if="l.attendancesCount > 0" class="text-emerald-700 font-semibold">
                          Điểm danh: {{ l.attendancesCount }}
                        </span>
                        <span v-if="l.leaveRequestsCount > 0" class="text-amber-700 font-semibold">
                          Nghỉ phép: {{ l.leaveRequestsCount }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Lesson Actions -->
                  <div class="flex items-center gap-1.5 flex-shrink-0">
                    <button
                      @click="openEditLessonModal(l)"
                      class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold text-gray-600 hover:text-orange-600 hover:bg-orange-50 rounded-lg transition-colors cursor-pointer"
                      title="Chỉnh sửa buổi học này"
                    >
                      <PencilSquareIcon class="w-3.5 h-3.5 text-orange-500" />
                      <span>Sửa</span>
                    </button>

                    <button
                      @click="confirmDeleteLesson(l)"
                      class="inline-flex items-center gap-1 px-2 py-1 text-xs font-semibold text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                      title="Xóa buổi học này"
                    >
                      <TrashIcon class="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Tab 3: STUDENTS (Danh sách học viên hiện tại của lớp) -->
            <div v-else-if="activeTab === 'students'" class="p-6 space-y-4">
              <!-- Summary & Search bar -->
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 bg-orange-50/70 rounded-xl border border-orange-200 text-xs">
                <div>
                  <span class="font-bold text-orange-950">
                    Sĩ số lớp: {{ classStudents.length }} / {{ maxCapacityDisplay }} học viên
                  </span>
                  <span class="text-gray-600 ml-2">
                    ({{ activeEnrolledCount }} đang học, {{ pendingEnrolledCount }} chờ xác nhận)
                  </span>
                </div>
                <div class="w-full sm:w-64">
                  <div class="relative">
                    <input
                      v-model="searchStudent"
                      type="text"
                      placeholder="Tìm kiếm học viên..."
                      class="w-full pl-8 pr-3 py-1.5 text-xs bg-white border border-orange-200 rounded-lg focus:outline-hidden focus:border-orange-500"
                    />
                    <MagnifyingGlassIcon class="w-4 h-4 text-gray-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
                  </div>
                </div>
              </div>

              <!-- Loading students -->
              <LoadingSpinner v-if="loadingStudents" label="Đang tải danh sách học viên trong lớp..." />

              <!-- Empty students -->
              <EmptyState
                v-else-if="classStudents.length === 0"
                :icon="UserGroupIcon"
                message="Lớp học chưa có học viên nào"
                sub="Bấm nút 'Thêm học viên' để ghi danh học viên vào lớp từ danh sách profile học viên"
              />

              <!-- Filtered empty -->
              <div v-else-if="filteredClassStudents.length === 0" class="text-center py-10 text-xs text-gray-400">
                Không tìm thấy học viên nào phù hợp với từ khóa "{{ searchStudent }}".
              </div>

              <!-- Student Table -->
              <div v-else class="overflow-x-auto border border-orange-100 rounded-xl">
                <table class="w-full text-left text-xs">
                  <thead class="bg-orange-50/80 text-orange-900 font-bold border-b border-orange-100 uppercase text-[10px] tracking-wider">
                    <tr>
                      <th class="py-3 px-3 text-center w-10">STT</th>
                      <th class="py-3 px-3">Học viên</th>
                      <th class="py-3 px-3">Phụ huynh liên hệ</th>
                      <th class="py-3 px-3">Ngày tham gia</th>
                      <th class="py-3 px-3 text-center">Trạng thái</th>
                      <th class="py-3 px-3 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-orange-50 bg-white">
                    <tr
                      v-for="(st, idx) in filteredClassStudents"
                      :key="st.id || st.studentId"
                      class="hover:bg-orange-50/40 transition-colors"
                    >
                      <td class="py-3 px-3 text-center text-gray-400 font-medium">{{ idx + 1 }}</td>
                      <td class="py-3 px-3">
                        <div class="flex items-center gap-2.5">
                          <img
                            v-if="st.avatar"
                            :src="st.avatar"
                            :alt="st.studentName"
                            class="w-8 h-8 rounded-full object-cover border border-orange-200 flex-shrink-0"
                          />
                          <div
                            v-else
                            class="w-8 h-8 rounded-full bg-gradient-to-tr from-orange-400 to-amber-500 text-white font-bold flex items-center justify-center text-xs flex-shrink-0"
                          >
                            {{ (st.studentName || 'H').charAt(0) }}
                          </div>
                          <div>
                            <div class="font-bold text-gray-900">{{ st.studentName }}</div>
                            <div class="text-[10px] text-gray-400 flex items-center gap-2 mt-0.5">
                              <span>⭐ {{ st.stars || 0 }} sao</span>
                              <span>🏆 {{ st.totalPoints || 0 }} đ</span>
                              <span>🔥 {{ st.streakDays || 0 }} ngày</span>
                            </div>
                          </div>
                        </div>
                      </td>
                      <td class="py-3 px-3">
                        <div class="font-medium text-gray-800">{{ st.parentName || '--' }}</div>
                        <div class="text-[11px] text-orange-600 font-mono mt-0.5">{{ st.parentPhone || st.parentEmail || '--' }}</div>
                      </td>
                      <td class="py-3 px-3 text-gray-500">
                        {{ formatDate(st.joinedAt) }}
                      </td>
                      <td class="py-3 px-3 text-center">
                        <span
                          :class="[
                            'px-2 py-0.5 rounded-full text-[10px] font-bold border',
                            st.status === 'ACTIVE'
                              ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                              : 'bg-amber-50 text-amber-700 border-amber-200'
                          ]"
                        >
                          {{ st.status === 'ACTIVE' ? 'Đang học' : 'Chờ xác nhận' }}
                        </span>
                      </td>
                      <td class="py-3 px-3 text-right">
                        <button
                          @click="confirmRemoveStudent(st)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                          title="Rút học viên khỏi lớp"
                        >
                          <TrashIcon class="w-3.5 h-3.5" />
                          <span>Xóa</span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Modal Thêm Học Viên vào Lớp ──────────────────────────────── -->
      <transition name="modal-fade">
        <div
          v-if="showAddStudentModal"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs"
          @click.self="showAddStudentModal = false"
        >
          <div class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-lg overflow-hidden transform transition-all my-8">
            <!-- Modal Header -->
            <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50 to-amber-50">
              <div class="flex items-center gap-2.5">
                <div class="w-9 h-9 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-xs">
                  <UserPlusIcon class="w-5 h-5" />
                </div>
                <div>
                  <h3 class="text-base font-bold text-orange-950">Thêm học viên vào lớp</h3>
                  <p class="text-xs text-gray-500">Lớp: {{ cls.name }} (Sĩ số hiện tại: {{ classStudents.length }}/{{ maxCapacityDisplay }})</p>
                </div>
              </div>
              <button
                @click="showAddStudentModal = false"
                class="text-gray-400 hover:text-gray-600 p-1.5 rounded-lg hover:bg-orange-100/60 transition-colors cursor-pointer"
              >
                <XMarkIcon class="w-5 h-5" />
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
              <!-- Capacity Warning if full -->
              <div
                v-if="maxCapacity && classStudents.length >= maxCapacity"
                class="p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700 flex items-center gap-2"
              >
                <ExclamationCircleIcon class="w-4 h-4 text-red-500 flex-shrink-0" />
                <span>Lớp học đã đạt sĩ số tối đa ({{ maxCapacity }} học viên). Việc thêm học viên sẽ vượt định mức sĩ số lớp.</span>
              </div>

              <!-- Search student profile from Backend -->
              <div>
                <label class="block text-xs font-bold text-gray-700 mb-1.5">
                  Tìm kiếm học viên theo tên <span class="text-red-500">*</span>
                </label>
                <div class="relative">
                  <input
                    v-model="studentSearchQuery"
                    type="text"
                    placeholder="Nhập tên học sinh để tìm (VD: Minh Khang, Bảo Ngọc...)"
                    class="w-full pl-9 pr-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
                  />
                  <MagnifyingGlassIcon class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
                </div>
              </div>

              <!-- List of available student profiles -->
              <div>
                <div class="flex items-center justify-between mb-1.5">
                  <label class="block text-xs font-bold text-gray-700">
                    Danh sách profile học viên (Lấy từ Backend):
                  </label>
                  <span class="text-[11px] text-gray-400">
                    {{ filteredAvailableStudents.length }} học viên
                  </span>
                </div>

                <div v-if="loadingAvailableStudents" class="py-6 text-center text-xs text-gray-400">
                  <LoadingSpinner label="Đang tải danh sách profile học viên từ backend..." />
                </div>

                <div
                  v-else
                  class="space-y-1.5 max-h-56 overflow-y-auto border border-gray-200 rounded-xl p-2 bg-gray-50/50"
                >
                  <div
                    v-for="st in filteredAvailableStudents"
                    :key="st.id"
                    @click="!isStudentInClass(st.id) && selectStudentToAdd(st)"
                    :class="[
                      'p-2.5 rounded-xl border flex items-center justify-between gap-3 transition-all',
                      isStudentInClass(st.id)
                        ? 'opacity-50 cursor-not-allowed bg-gray-100 border-gray-200'
                        : selectedStudentToAdd?.id === st.id
                          ? 'border-orange-500 bg-orange-50/90 shadow-2xs cursor-pointer'
                          : 'border-transparent bg-white hover:border-orange-200 hover:bg-orange-50/40 cursor-pointer'
                    ]"
                  >
                    <div class="flex items-center gap-2.5 min-w-0">
                      <img
                        v-if="st.avatar"
                        :src="st.avatar"
                        :alt="st.fullName"
                        class="w-8 h-8 rounded-full object-cover border border-orange-200 flex-shrink-0"
                      />
                      <div
                        v-else
                        class="w-8 h-8 rounded-full bg-orange-200 text-orange-800 font-bold flex items-center justify-center text-xs flex-shrink-0"
                      >
                        {{ (st.fullName || 'H').charAt(0) }}
                      </div>
                      <div class="min-w-0">
                        <div class="font-bold text-gray-900 text-xs truncate">{{ st.fullName }}</div>
                        <div class="text-[11px] text-gray-500 truncate">
                          PH: {{ st.parentName || '--' }} · {{ st.parentPhone || '' }}
                        </div>
                      </div>
                    </div>

                    <div class="flex items-center gap-2 flex-shrink-0">
                      <span
                        v-if="isStudentInClass(st.id)"
                        class="text-[10px] font-semibold text-gray-500 bg-gray-200 px-2 py-0.5 rounded-full"
                      >
                        Đã trong lớp
                      </span>
                      <span
                        v-else-if="selectedStudentToAdd?.id === st.id"
                        class="text-[10px] font-bold text-white bg-orange-500 px-2 py-0.5 rounded-full"
                      >
                        Đã chọn
                      </span>
                    </div>
                  </div>

                  <div v-if="filteredAvailableStudents.length === 0" class="py-4 text-center text-xs text-gray-400">
                    Không tìm thấy học viên nào phù hợp với từ khóa.
                  </div>
                </div>
              </div>

              <!-- Selected Student Preview Card -->
              <div
                v-if="selectedStudentToAdd"
                class="p-3.5 bg-gradient-to-r from-orange-50 to-amber-50 rounded-xl border border-orange-200 space-y-2"
              >
                <div class="text-[11px] font-bold text-orange-900 uppercase tracking-wide">
                  Học viên được chọn:
                </div>
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2.5">
                    <img
                      v-if="selectedStudentToAdd.avatar"
                      :src="selectedStudentToAdd.avatar"
                      class="w-10 h-10 rounded-full border border-orange-300"
                    />
                    <div v-else class="w-10 h-10 rounded-full bg-orange-400 text-white font-bold flex items-center justify-center text-sm">
                      {{ (selectedStudentToAdd.fullName || 'H').charAt(0) }}
                    </div>
                    <div>
                      <div class="font-bold text-gray-900 text-sm">{{ selectedStudentToAdd.fullName }}</div>
                      <div class="text-xs text-gray-600">
                        Phụ huynh: {{ selectedStudentToAdd.parentName }} · {{ selectedStudentToAdd.parentPhone }}
                      </div>
                    </div>
                  </div>
                  <CheckCircleIcon class="w-6 h-6 text-emerald-600" />
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="px-6 py-4 border-t border-gray-100 flex items-center justify-end gap-2.5 bg-gray-50">
              <button
                type="button"
                @click="showAddStudentModal = false"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-200 rounded-xl transition-colors cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                @click="handleAddStudentSubmit"
                :disabled="!selectedStudentToAdd || isAddingStudent"
                class="px-4 py-2 text-xs font-bold text-white bg-orange-500 hover:bg-orange-600 rounded-xl shadow-sm transition-colors cursor-pointer disabled:opacity-50"
              >
                {{ isAddingStudent ? 'Đang thêm...' : 'Xác nhận thêm vào lớp' }}
              </button>
            </div>
          </div>
        </div>
      </transition>

      <!-- ─── Modal Phân công Giáo viên cho Lớp học ─────────────────────── -->
      <transition name="modal-fade">
        <div
          v-if="showAssignTeacherModal"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs"
          @click.self="showAssignTeacherModal = false"
        >
          <div class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-md overflow-hidden transform transition-all my-8">
            <!-- Modal Header -->
            <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-purple-50 to-orange-50">
              <div class="flex items-center gap-2.5">
                <div class="w-9 h-9 rounded-xl bg-purple-600 text-white flex items-center justify-center shadow-xs">
                  <AcademicCapIcon class="w-5 h-5" />
                </div>
                <div>
                  <h3 class="text-base font-bold text-purple-950">Phân công Giáo viên</h3>
                  <p class="text-xs text-gray-500">Lớp: {{ cls.name }}</p>
                </div>
              </div>
              <button
                @click="showAssignTeacherModal = false"
                class="text-gray-400 hover:text-gray-600 p-1.5 rounded-lg hover:bg-orange-100/60 transition-colors cursor-pointer"
              >
                <XMarkIcon class="w-5 h-5" />
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              <p class="text-xs text-gray-600">
                Chọn giáo viên phụ trách giảng dạy cho lớp học này (danh sách được lấy từ <code class="font-mono text-purple-600">teacher.service</code>):
              </p>

              <div v-if="loadingTeachers" class="py-6 text-center text-xs text-gray-400">
                <LoadingSpinner label="Đang tải danh sách giáo viên từ teacher.service..." />
              </div>

              <div v-else class="space-y-2">
                <!-- Option: Không phân công -->
                <div
                  @click="selectedTeacherId = null"
                  :class="[
                    'p-3 rounded-xl border flex items-center justify-between cursor-pointer transition-all',
                    selectedTeacherId === null
                      ? 'border-purple-500 bg-purple-50 shadow-2xs'
                      : 'border-gray-200 hover:border-purple-200 bg-white'
                  ]"
                >
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 rounded-full bg-gray-200 text-gray-600 font-bold flex items-center justify-center text-xs">
                      --
                    </div>
                    <div>
                      <div class="font-bold text-gray-700 text-xs">Chưa phân công giáo viên</div>
                      <div class="text-[11px] text-gray-400">Để trống thông tin giáo viên phụ trách</div>
                    </div>
                  </div>
                  <input
                    type="radio"
                    name="assign-teacher"
                    :checked="selectedTeacherId === null"
                    class="text-purple-600 focus:ring-purple-500"
                  />
                </div>

                <!-- Teacher options from teacher.service -->
                <div
                  v-for="t in availableTeachers"
                  :key="t.id"
                  @click="selectedTeacherId = t.id"
                  :class="[
                    'p-3 rounded-xl border flex items-center justify-between cursor-pointer transition-all',
                    selectedTeacherId === t.id
                      ? 'border-purple-500 bg-purple-50 shadow-2xs'
                      : 'border-gray-200 hover:border-purple-200 bg-white'
                  ]"
                >
                  <div class="flex items-center gap-3 min-w-0">
                    <div class="w-9 h-9 rounded-full bg-gradient-to-tr from-purple-500 to-indigo-500 text-white font-bold flex items-center justify-center text-xs flex-shrink-0">
                      {{ (t.fullName || 'G').charAt(0) }}
                    </div>
                    <div class="min-w-0">
                      <div class="font-bold text-gray-900 text-xs truncate">{{ t.fullName }}</div>
                      <div class="text-[11px] text-gray-500 truncate">
                        {{ t.phone || t.email || `Mã GV: GV-${t.id}` }}
                      </div>
                    </div>
                  </div>
                  <input
                    type="radio"
                    name="assign-teacher"
                    :checked="selectedTeacherId === t.id"
                    class="text-purple-600 focus:ring-purple-500"
                  />
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="px-6 py-4 border-t border-gray-100 flex items-center justify-end gap-2.5 bg-gray-50">
              <button
                type="button"
                @click="showAssignTeacherModal = false"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-200 rounded-xl transition-colors cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                @click="handleAssignTeacherSubmit"
                :disabled="isAssigningTeacher"
                class="px-4 py-2 text-xs font-bold text-white bg-purple-600 hover:bg-purple-700 rounded-xl shadow-sm transition-colors cursor-pointer disabled:opacity-50"
              >
                {{ isAssigningTeacher ? 'Đang lưu...' : 'Lưu phân công' }}
              </button>
            </div>
          </div>
        </div>
      </transition>

      <!-- ─── Add / Edit Class Modal ───────────────────────────────────── -->
      <ClassModal
        :show="showClassModal"
        :cls="cls"
        @close="showClassModal = false"
        @saved="handleClassSaved"
        @deleted="handleClassDeleted"
      />

      <!-- ─── Add / Edit Schedule Modal ────────────────────────────────── -->
      <ScheduleModal
        :show="showScheduleModal"
        :schedule="selectedSchedule"
        :classes="[cls]"
        :default-class-id="cls.id"
        :initial-mode="scheduleModalMode"
        @close="showScheduleModal = false"
        @saved="handleScheduleSaved"
        @deleted="handleScheduleDeleted"
      />

      <!-- ─── Add / Edit Lesson Modal ──────────────────────────────────── -->
      <LessonModal
        :show="showLessonModal"
        :lesson="selectedLesson"
        :class-id="cls.id"
        @close="showLessonModal = false"
        @saved="handleLessonSaved"
        @deleted="handleLessonDeleted"
      />
    </template>

    <!-- ─── Not Found / Error ─────────────────────────────────────────────── -->
    <div v-else class="flex flex-col items-center justify-center py-20 gap-3 bg-white rounded-2xl border border-orange-100">
      <ExclamationCircleIcon class="w-12 h-12 text-red-400" />
      <p class="text-gray-700 font-bold text-lg">Không tìm thấy thông tin lớp học</p>
      <p class="text-gray-400 text-sm">Lớp học không tồn tại hoặc đã bị xóa khỏi hệ thống.</p>
      <router-link to="/training/classes" class="mt-2 text-sm text-orange-600 hover:text-orange-700 underline font-semibold">
        ← Quay lại danh sách lớp học
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  UserGroupIcon, UserPlusIcon, ChevronRightIcon, BookOpenIcon, AcademicCapIcon,
  CalendarDaysIcon, CalendarIcon, ClockIcon, MapPinIcon, CurrencyDollarIcon,
  PencilSquareIcon, PlusIcon, TrashIcon, SparklesIcon, CheckCircleIcon,
  ExclamationCircleIcon, InformationCircleIcon, ArrowPathIcon, XMarkIcon,
  MagnifyingGlassIcon,
} from '@heroicons/vue/24/outline'

import LoadingSpinner from '@/components/common/LoadingSpinner.vue'
import EmptyState     from '@/components/common/EmptyState.vue'
import StatusBadge    from '@/components/course/StatusBadge.vue'
import ClassModal     from '@/components/class/ClassModal.vue'
import ScheduleModal  from '@/components/schedule/ScheduleModal.vue'
import LessonModal    from '@/components/class/LessonModal.vue'

import classService         from '@/services/class.service'
import classScheduleService from '@/services/classSchedule.service'
import lessonService        from '@/services/lesson.service'
import courseLevelService   from '@/services/courseLevel.service'
import courseService        from '@/services/course.service'
import teacherService       from '@/services/teacher.service'
import parentService        from '@/services/parent.service'
import { syncClassLessonsAndEndDate, calculateScheduleSessions } from '@/services/scheduleSync.helper'

const route  = useRoute()
const router = useRouter()

// ─── State ───────────────────────────────────────────────────────────────────
const loading          = ref(true)
const loadingSchedules = ref(false)
const loadingLessons   = ref(false)
const loadingStudents  = ref(false)
const isSyncing        = ref(false)
const activeTab        = ref('schedules')

const cls           = ref(null)
const level         = ref(null)
const course        = ref(null)
const teacher       = ref(null)
const schedules     = ref([])
const lessons       = ref([])
const classStudents = ref([])

// Toast / Notification
const syncAlertMessage = ref('')
const syncAlertSuccess = ref(true)

// Modals State
const showClassModal    = ref(false)
const showScheduleModal = ref(false)
const selectedSchedule  = ref(null)
const scheduleModalMode = ref('create')

const showLessonModal   = ref(false)
const selectedLesson    = ref(null)

// ─── Add Student Modal State ──────────────────────────────────────────────────
const showAddStudentModal      = ref(false)
const availableStudents        = ref([])
const loadingAvailableStudents = ref(false)
const studentSearchQuery       = ref('')
const selectedStudentToAdd     = ref(null)
const isAddingStudent          = ref(false)
const searchStudent            = ref('')

// ─── Assign Teacher Modal State ───────────────────────────────────────────────
const showAssignTeacherModal = ref(false)
const availableTeachers      = ref([])
const loadingTeachers        = ref(false)
const selectedTeacherId      = ref(null)
const isAssigningTeacher     = ref(false)

// ─── Days of Week Constants ───────────────────────────────────────────────────
const DAYS_OF_WEEK = [
  { value: 1, label: 'Thứ 2', full: 'Thứ Hai', color: 'border-blue-200 bg-blue-50 text-blue-700' },
  { value: 2, label: 'Thứ 3', full: 'Thứ Ba', color: 'border-emerald-200 bg-emerald-50 text-emerald-700' },
  { value: 3, label: 'Thứ 4', full: 'Thứ Tư', color: 'border-amber-200 bg-amber-50 text-amber-700' },
  { value: 4, label: 'Thứ 5', full: 'Thứ Năm', color: 'border-purple-200 bg-purple-50 text-purple-700' },
  { value: 5, label: 'Thứ 6', full: 'Thứ Sáu', color: 'border-rose-200 bg-rose-50 text-rose-700' },
  { value: 6, label: 'Thứ 7', full: 'Thứ Bảy', color: 'border-cyan-200 bg-cyan-50 text-cyan-700' },
  { value: 0, label: 'Chủ nhật', full: 'Chủ Nhật', color: 'border-red-200 bg-red-50 text-red-700' },
]

// ─── Computed ─────────────────────────────────────────────────────────────────
const activeCount = computed(() => classStudents.value.length || cls.value?.activeStudentsCount || cls.value?.currentStudents || 0)
const maxCapacity = computed(() => cls.value?.maxCapacity ?? cls.value?.maxStudents)
const maxCapacityDisplay = computed(() => maxCapacity.value ?? '∞')

const capacityFillPct = computed(() => {
  if (!maxCapacity.value) return 0
  return Math.min(100, Math.round((activeCount.value / maxCapacity.value) * 100))
})

const completedLessonsCount = computed(() =>
  lessons.value.filter(l => l.status === 'COMPLETED').length
)

const sessionsProgressPct = computed(() => {
  const total = level.value?.totalSessions || lessons.value.length || 1
  return Math.min(100, Math.round((completedLessonsCount.value / total) * 100))
})

const calculatedEndDateDisplay = computed(() => {
  if (!cls.value?.startDate || schedules.value.length === 0 || !level.value?.totalSessions) return null
  const { calculatedEndDate } = calculateScheduleSessions(cls.value.startDate, schedules.value, level.value.totalSessions)
  return calculatedEndDate ? formatDate(calculatedEndDate) : null
})

const courseBenefits = computed(() => {
  if (!course.value?.benefits) return []
  if (Array.isArray(course.value.benefits)) return course.value.benefits
  try {
    return JSON.parse(course.value.benefits)
  } catch {
    return [course.value.benefits]
  }
})

const sortedSchedules = computed(() => {
  const list = [...schedules.value]
  return list.sort((a, b) => {
    const dayA = a.dayOfWeek === 0 ? 7 : a.dayOfWeek
    const dayB = b.dayOfWeek === 0 ? 7 : b.dayOfWeek
    if (dayA !== dayB) return dayA - dayB
    return String(a.startTime).localeCompare(String(b.startTime))
  })
})

const sortedLessons = computed(() => {
  const list = [...lessons.value]
  return list.sort((a, b) => new Date(a.lessonDate) - new Date(b.lessonDate))
})

// Students Computed
const activeEnrolledCount = computed(() =>
  classStudents.value.filter(s => s.status === 'ACTIVE').length
)
const pendingEnrolledCount = computed(() =>
  classStudents.value.filter(s => s.status === 'PENDING').length
)

const filteredClassStudents = computed(() => {
  if (!searchStudent.value.trim()) return classStudents.value
  const q = searchStudent.value.toLowerCase().trim()
  return classStudents.value.filter(s =>
    (s.studentName || '').toLowerCase().includes(q) ||
    (s.parentName || '').toLowerCase().includes(q) ||
    (s.parentPhone || '').includes(q) ||
    String(s.studentId).includes(q)
  )
})

const filteredAvailableStudents = computed(() => {
  if (!studentSearchQuery.value.trim()) return availableStudents.value
  const q = studentSearchQuery.value.toLowerCase().trim()
  return availableStudents.value.filter(s =>
    (s.fullName || '').toLowerCase().includes(q) ||
    (s.parentName || '').toLowerCase().includes(q) ||
    (s.parentPhone || '').includes(q) ||
    String(s.id).includes(q)
  )
})

function isStudentInClass(studentId) {
  return classStudents.value.some(s => s.studentId === studentId)
}

function selectStudentToAdd(st) {
  selectedStudentToAdd.value = st
}

// ─── Day & Status Helpers ─────────────────────────────────────────────────────
function getDayBadge(dow) {
  return DAYS_OF_WEEK.find(d => d.value === Number(dow)) ?? {
    label: `Thứ ${dow}`,
    full: `Thứ ${dow}`,
    color: 'border-gray-200 bg-gray-50 text-gray-700',
  }
}

function hasScheduleOnDay(dow) {
  return schedules.value.some(s => Number(s.dayOfWeek) === Number(dow))
}

function getSchedulesOnDay(dow) {
  return schedules.value.filter(s => Number(s.dayOfWeek) === Number(dow))
}

function getLessonStatusBadge(status) {
  switch (String(status || '').toUpperCase()) {
    case 'COMPLETED':
      return { label: 'Đã hoàn thành', color: 'bg-emerald-50 text-emerald-700 border-emerald-200' }
    case 'CANCELLED':
      return { label: 'Đã hủy', color: 'bg-gray-100 text-gray-500 border-gray-200' }
    default:
      return { label: 'Lên lịch', color: 'bg-blue-50 text-blue-700 border-blue-200' }
  }
}

function formatFee(fee) {
  return Number(fee || 0).toLocaleString('vi-VN') + 'đ'
}

function formatDate(d) {
  if (!d) return '--'
  return new Date(d).toLocaleDateString('vi-VN')
}

function formatTime(t) {
  if (!t) return '--:--'
  if (String(t).includes('T')) {
    const d = new Date(t)
    if (!isNaN(d.getTime())) {
      const hh = String(d.getHours()).padStart(2, '0')
      const mm = String(d.getMinutes()).padStart(2, '0')
      return `${hh}:${mm}`
    }
  }
  const parts = String(t).split(':')
  if (parts.length >= 2) return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
  return t
}

function calcDuration(start, end) {
  if (!start || !end) return ''
  const [sH, sM] = String(start).split(':').map(Number)
  const [eH, eM] = String(end).split(':').map(Number)
  if (isNaN(sH) || isNaN(eH)) return ''
  const diff = (eH * 60 + (eM || 0)) - (sH * 60 + (sM || 0))
  if (diff <= 0) return ''
  return `${diff} phút`
}

// ─── Modal Actions: Class ─────────────────────────────────────────────────────
function openEditClassModal() {
  showClassModal.value = true
}

async function handleClassSaved(updatedData) {
  if (updatedData) {
    cls.value = { ...cls.value, ...updatedData }
  }
  await loadAllData()
  await triggerAutoSync()
}

function handleClassDeleted() {
  router.push({ name: 'classes' })
}

// ─── Modal Actions: Schedule ──────────────────────────────────────────────────
function openAddScheduleModal() {
  selectedSchedule.value = null
  scheduleModalMode.value = 'create'
  showScheduleModal.value = true
}

function openEditScheduleModal(item) {
  selectedSchedule.value = item
  scheduleModalMode.value = 'edit'
  showScheduleModal.value = true
}

async function handleScheduleSaved() {
  await loadSchedules()
  await triggerAutoSync()
}

async function handleScheduleDeleted() {
  await loadSchedules()
  await triggerAutoSync()
}

async function confirmDeleteSchedule(item) {
  const dInfo = getDayBadge(item.dayOfWeek)
  const msg = `Bạn có chắc chắn muốn xóa buổi học ${dInfo.label} (${formatTime(item.startTime)} - ${formatTime(item.endTime)}) của lớp không?`
  if (!window.confirm(msg)) return

  try {
    await classScheduleService.delete(item.id)
    await loadSchedules()
    await triggerAutoSync()
  } catch (err) {
    console.error('Lỗi khi xóa lịch học:', err)
    alert(err?.response?.data?.message || 'Không thể xóa lịch học. Vui lòng thử lại.')
  }
}

// ─── Modal Actions: Lesson ────────────────────────────────────────────────────
function openAddLessonModal() {
  selectedLesson.value = null
  showLessonModal.value = true
}

function openEditLessonModal(l) {
  selectedLesson.value = l
  showLessonModal.value = true
}

async function handleLessonSaved() {
  await loadLessons()
}

async function handleLessonDeleted() {
  await loadLessons()
}

async function confirmDeleteLesson(l) {
  if (l.attendancesCount > 0) {
    alert('Không thể xóa buổi học đã có dữ liệu điểm danh.')
    return
  }
  if (!window.confirm(`Bạn có chắc chắn muốn xóa buổi học ngày ${formatDate(l.lessonDate)} không?`)) {
    return
  }

  try {
    await lessonService.delete(l.id)
    await loadLessons()
  } catch (err) {
    console.error('Lỗi khi xóa buổi học:', err)
    alert(err?.response?.data?.message || 'Không thể xóa buổi học.')
  }
}

// ─── Modal Actions: Student Management ────────────────────────────────────────
async function openAddStudentModal() {
  selectedStudentToAdd.value = null
  studentSearchQuery.value = ''
  showAddStudentModal.value = true
  if (availableStudents.value.length === 0) {
    await loadAvailableStudents()
  }
}

async function loadAvailableStudents() {
  loadingAvailableStudents.value = true
  try {
    const data = await parentService.getAllStudents()
    availableStudents.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải danh sách profile học viên từ backend:', err)
    availableStudents.value = []
  } finally {
    loadingAvailableStudents.value = false
  }
}

async function handleAddStudentSubmit() {
  if (!cls.value || !selectedStudentToAdd.value) return
  isAddingStudent.value = true
  try {
    await classService.addStudent(cls.value.id, selectedStudentToAdd.value.id)
    await loadClassStudents()
    if (cls.value) {
      cls.value.activeStudentsCount = classStudents.value.length
    }
    showAddStudentModal.value = false
    syncAlertSuccess.value = true
    syncAlertMessage.value = `Đã thêm học viên "${selectedStudentToAdd.value.fullName}" vào lớp "${cls.value.name}" thành công!`
    selectedStudentToAdd.value = null
    studentSearchQuery.value = ''
  } catch (err) {
    console.error('Lỗi khi thêm học viên vào lớp:', err)
    alert(err?.response?.data?.message || 'Không thể thêm học viên vào lớp.')
  } finally {
    isAddingStudent.value = false
  }
}

async function confirmRemoveStudent(st) {
  const studentName = st.studentName || 'học viên này'
  if (!window.confirm(`Bạn có chắc chắn muốn xóa học viên "${studentName}" khỏi lớp "${cls.value?.name}" không?`)) {
    return
  }
  try {
    await classService.removeStudent(cls.value.id, st.studentId)
    await loadClassStudents()
    if (cls.value) {
      cls.value.activeStudentsCount = classStudents.value.length
    }
    syncAlertSuccess.value = true
    syncAlertMessage.value = `Đã xóa học viên "${studentName}" khỏi lớp học.`
  } catch (err) {
    console.error('Lỗi khi xóa học viên khỏi lớp:', err)
    alert(err?.response?.data?.message || 'Không thể xóa học viên khỏi lớp.')
  }
}

// ─── Modal Actions: Assign Teacher (teacher.service) ──────────────────────────
async function openAssignTeacherModal() {
  selectedTeacherId.value = cls.value?.teacherId ?? null
  showAssignTeacherModal.value = true
  if (availableTeachers.value.length === 0) {
    await loadAvailableTeachers()
  }
}

async function loadAvailableTeachers() {
  loadingTeachers.value = true
  try {
    const data = await teacherService.getAll()
    availableTeachers.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải danh sách giáo viên từ teacher.service:', err)
    availableTeachers.value = []
  } finally {
    loadingTeachers.value = false
  }
}

async function handleAssignTeacherSubmit() {
  if (!cls.value) return
  isAssigningTeacher.value = true
  try {
    const payload = {
      courseLevelId: cls.value.courseLevelId,
      teacherId: selectedTeacherId.value,
      name: cls.value.name,
      maxCapacity: cls.value.maxCapacity ?? 15,
      startDate: cls.value.startDate,
      endDate: cls.value.endDate,
      status: cls.value.status ?? 'ACTIVE',
    }
    await classService.update(cls.value.id, payload)

    cls.value.teacherId = selectedTeacherId.value
    const foundTeacher = availableTeachers.value.find(t => t.id === selectedTeacherId.value)
    cls.value.teacherName = foundTeacher ? foundTeacher.fullName : null
    teacher.value = foundTeacher || null

    showAssignTeacherModal.value = false
    syncAlertSuccess.value = true
    syncAlertMessage.value = `Đã cập nhật phân công giáo viên cho lớp "${cls.value.name}": ${cls.value.teacherName || 'Chưa phân công'}!`
  } catch (err) {
    console.error('Lỗi khi phân công giáo viên:', err)
    alert(err?.response?.data?.message || 'Không thể phân công giáo viên. Vui lòng thử lại.')
  } finally {
    isAssigningTeacher.value = false
  }
}

// ─── Auto Sync & EndDate Calculation ──────────────────────────────────────────
async function triggerAutoSync() {
  if (!cls.value || !level.value) return
  isSyncing.value = true
  try {
    const res = await syncClassLessonsAndEndDate(cls.value.id, {
      classData: cls.value,
      courseLevel: level.value,
      schedules: schedules.value,
      currentLessons: lessons.value,
    })

    if (res.success) {
      if (res.updatedClass) {
        cls.value = { ...cls.value, ...res.updatedClass }
      }
      await loadLessons()

      const endStr = res.calculatedEndDate ? formatDate(res.calculatedEndDate) : '--'
      syncAlertSuccess.value = true
      syncAlertMessage.value = `Đã đồng bộ thành công: Tính toán bế giảng ngày ${endStr}, chuẩn hóa ${res.expectedTotal} buổi học theo khóa học.`
    }
  } catch (err) {
    console.error('Lỗi tự động đồng bộ:', err)
  } finally {
    isSyncing.value = false
  }
}

async function triggerManualSync() {
  syncAlertMessage.value = ''
  await triggerAutoSync()
}

// ─── Data Loading ─────────────────────────────────────────────────────────────
async function loadSchedules() {
  const classId = route.params.id
  loadingSchedules.value = true
  try {
    const data = await classScheduleService.getByClass(classId)
    schedules.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải lịch học của lớp:', err)
    schedules.value = []
  } finally {
    loadingSchedules.value = false
  }
}

async function loadLessons() {
  const classId = route.params.id
  loadingLessons.value = true
  try {
    const data = await lessonService.getByClass(classId)
    lessons.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải buổi học của lớp:', err)
    lessons.value = []
  } finally {
    loadingLessons.value = false
  }
}

async function loadClassStudents() {
  const classId = route.params.id
  loadingStudents.value = true
  try {
    const data = await classService.getStudents(classId)
    classStudents.value = Array.isArray(data) ? data : (data.items ?? data.data ?? [])
  } catch (err) {
    console.error('Lỗi khi tải học viên của lớp:', err)
    classStudents.value = []
  } finally {
    loadingStudents.value = false
  }
}

async function loadAllData() {
  const classId = route.params.id
  loading.value = true
  try {
    const classData = await classService.get(classId)
    cls.value = classData

    if (classData?.courseLevelId) {
      try {
        const levelData = await courseLevelService.get(classData.courseLevelId)
        level.value = levelData

        if (levelData?.courseId) {
          const courseData = await courseService.get(levelData.courseId)
          course.value = courseData
        }
      } catch (err) {
        console.error('Lỗi khi tải cấp độ / khóa học:', err)
      }
    }

    if (classData?.teacherId) {
      try {
        teacher.value = await teacherService.get(classData.teacherId)
      } catch (err) {
        console.error('Lỗi khi tải thông tin giáo viên:', err)
      }
    }

    await Promise.all([loadSchedules(), loadLessons(), loadClassStudents()])
  } catch (err) {
    console.error('Không tìm thấy lớp học:', err)
    cls.value = null
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadAllData()
  loadAvailableTeachers()
  loadAvailableStudents()
})
</script>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.25s ease;
  overflow: hidden;
}
.slide-down-enter-from,
.slide-down-leave-to {
  opacity: 0;
  max-height: 0;
  transform: translateY(-8px);
}
.slide-down-enter-to,
.slide-down-leave-from {
  opacity: 1;
  max-height: 100px;
  transform: translateY(0);
}

.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
</style>
