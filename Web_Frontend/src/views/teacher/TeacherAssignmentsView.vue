<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12">
    <!-- ─── Header & Title ──────────────────────────────────────────────── -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-orange-400" />
          <span>Bài kiểm tra & Bài tập</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-gray-900 tracking-tight">Bài kiểm tra & Bài tập về nhà</h1>
        <p class="text-xs md:text-sm text-gray-500 mt-0.5">
          Tạo bài kiểm tra từ ngân hàng đề, đính kèm vào buổi học và phân công cho cả lớp hoặc riêng lẻ từng học sinh
        </p>
      </div>

      <!-- Action buttons -->
      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="reloadLessonAssignments"
          :disabled="isLoading || !selectedLessonId"
          class="inline-flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl border border-orange-200 bg-white hover:bg-orange-50 text-orange-800 text-xs font-semibold shadow-xs transition-colors cursor-pointer disabled:opacity-50"
          title="Làm mới danh sách bài tập"
        >
          <ArrowPathIcon class="w-4 h-4 text-orange-600" :class="{ 'animate-spin': isLoading }" />
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          @click="openCreateModal"
          :disabled="!selectedLessonId"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white text-xs font-bold shadow-sm transition-all cursor-pointer"
        >
          <PlusIcon class="w-4 h-4" />
          <span>Tạo bài kiểm tra mới</span>
        </button>
      </div>
    </div>

    <!-- ─── Notification Alerts ─────────────────────────────────────────── -->
    <div
      v-if="feedback.message"
      :class="[
        'px-4 py-3 rounded-2xl text-xs font-medium flex items-center justify-between transition-all duration-300',
        feedback.type === 'error'
          ? 'bg-rose-50 text-rose-800 border border-rose-200'
          : 'bg-emerald-50 text-emerald-800 border border-emerald-200'
      ]"
    >
      <div class="flex items-center gap-2">
        <span class="text-base">{{ feedback.type === 'error' ? '⚠️' : '✅' }}</span>
        <span>{{ feedback.message }}</span>
      </div>
      <button @click="feedback.message = ''" class="text-gray-400 hover:text-gray-600 text-sm font-bold">✕</button>
    </div>

    <!-- ─── Filter & Selection Bar (Class & Lesson) ──────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 sm:p-5 shadow-sm space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <!-- Class Selector -->
        <div>
          <label class="block text-xs font-bold text-gray-700 mb-1 flex items-center gap-1.5">
            <AcademicCapIcon class="w-4 h-4 text-orange-500" />
            Lớp học đang phụ trách
          </label>
          <select
            v-model="selectedClassId"
            @change="handleClassChange"
            :disabled="isLoadingClasses"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option :value="null" disabled>-- Chọn lớp học --</option>
            <option v-for="c in classes" :key="c.id" :value="c.id">
              {{ c.name }} ({{ c.code || 'Mã lớp' }})
            </option>
          </select>
        </div>

        <!-- Lesson Selector (Numbered Buổi 1, Buổi 2...) -->
        <div>
          <label class="block text-xs font-bold text-gray-700 mb-1 flex items-center gap-1.5">
            <CalendarDaysIcon class="w-4 h-4 text-orange-500" />
            Buổi học đính kèm
          </label>
          <select
            v-model="selectedLessonId"
            @change="handleLessonChange"
            :disabled="isLoadingLessons || sortedLessons.length === 0"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option :value="null" disabled>-- Chọn buổi học --</option>
            <option v-for="l in sortedLessons" :key="l.id" :value="l.id">
              {{ l.lessonDisplayTitle }} ({{ formatDate(l.date) }})
            </option>
          </select>
        </div>

        <!-- Search in Lesson Assignments -->
        <div>
          <label class="block text-xs font-bold text-gray-700 mb-1 flex items-center gap-1.5">
            <MagnifyingGlassIcon class="w-4 h-4 text-orange-500" />
            Tìm bài kiểm tra
          </label>
          <SearchBar
            v-model="searchQuery"
            placeholder="Tìm theo tiêu đề bài kiểm tra..."
          />
        </div>
      </div>

      <!-- Quick Info Bar for current Lesson -->
      <div
        v-if="selectedLesson"
        class="flex flex-wrap items-center justify-between gap-3 pt-3 border-t border-orange-50 text-xs text-gray-600"
      >
        <div class="flex items-center gap-3">
          <span class="font-bold text-orange-900 bg-orange-100/80 px-2.5 py-1 rounded-lg">
            {{ selectedLesson.lessonDisplayTitle }}
          </span>
          <span>📅 Ngày: <strong>{{ formatDate(selectedLesson.date) }}</strong></span>
          <span v-if="selectedLesson.startTime">⏰ Giờ: {{ selectedLesson.startTime }} - {{ selectedLesson.endTime || '...' }}</span>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-gray-500">Sĩ số lớp: <strong class="text-orange-900">{{ classStudents.length }}</strong> học sinh</span>
          <span>•</span>
          <span class="text-gray-500">Bài kiểm tra gắn kèm: <strong class="text-orange-900">{{ assignments.length }}</strong></span>
        </div>
      </div>
    </div>

    <!-- ─── Assignments List Container ───────────────────────────────────── -->
    <div v-if="isLoading" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
      <ArrowPathIcon class="w-7 h-7 text-orange-500 animate-spin mx-auto mb-2" />
      <p class="text-xs text-gray-500">Đang tải danh sách bài kiểm tra...</p>
    </div>

    <div v-else-if="!selectedLessonId" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
      <EmptyState
        :icon="CalendarDaysIcon"
        message="Chưa chọn buổi học"
        sub="Vui lòng chọn lớp học và một buổi học cụ thể để xem hoặc đính kèm bài kiểm tra."
      />
    </div>

    <div v-else-if="filteredAssignments.length === 0" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
      <EmptyState
        :icon="ClipboardDocumentListIcon"
        message="Chưa có bài kiểm tra nào đính kèm buổi học này"
        sub="Nhấn nút 'Tạo bài kiểm tra mới' để soạn đề từ ngân hàng câu hỏi và giao cho học sinh."
      />
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="item in filteredAssignments"
        :key="item.id"
        class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm hover:border-orange-300 transition-all flex flex-col lg:flex-row lg:items-center justify-between gap-5"
      >
        <!-- Left info -->
        <div class="flex items-start gap-4">
          <div class="w-12 h-12 rounded-2xl bg-orange-100 text-orange-900 font-bold text-xs flex flex-col items-center justify-center flex-shrink-0 border border-orange-200">
            <ClipboardDocumentCheckIcon class="w-5 h-5 text-orange-600" />
            <span class="text-[10px] mt-0.5 font-extrabold">{{ item.totalQuestions }} câu</span>
          </div>

          <div class="space-y-1">
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="text-sm sm:text-base font-bold text-gray-900 hover:text-orange-600 cursor-pointer">
                {{ item.title }}
              </h3>

              <span
                :class="[
                  'text-[10px] font-bold px-2 py-0.5 rounded-full',
                  item.status === 'OPEN' ? 'bg-emerald-100 text-emerald-800' :
                  item.status === 'CLOSED' ? 'bg-gray-100 text-gray-700' :
                  item.status === 'SCHEDULED' ? 'bg-blue-100 text-blue-800' :
                  'bg-amber-100 text-amber-800'
                ]"
              >
                {{
                  item.status === 'OPEN' ? 'Đang mở' :
                  item.status === 'CLOSED' ? 'Đã đóng' :
                  item.status === 'SCHEDULED' ? 'Đã lên lịch' : 'Bản nháp'
                }}
              </span>

              <span class="text-[10px] font-semibold bg-orange-50 text-orange-800 border border-orange-200 px-2 py-0.5 rounded">
                Thang {{ item.totalPoints }} điểm
              </span>
            </div>

            <!-- Meta metrics -->
            <div class="flex flex-wrap items-center gap-3 sm:gap-4 text-xs text-gray-500 pt-1">
              <span class="flex items-center gap-1">
                <ClockIcon class="w-3.5 h-3.5 text-gray-400" />
                Thời gian: <strong>{{ item.timeLimitMinutes }} phút</strong>
              </span>
              <span>•</span>
              <span>Số lần làm: <strong>{{ item.maxAttempt || 1 }} lần</strong></span>
              <span>•</span>
              <span>Bắt đầu: <strong>{{ formatDateTime(item.startTime) }}</strong></span>
              <span>•</span>
              <span>Hạn nộp: <strong class="text-rose-600">{{ formatDateTime(item.deadline) }}</strong></span>
            </div>

            <!-- Assigned student count ribbon -->
            <div class="flex items-center gap-2 pt-1 text-xs">
              <span class="text-gray-500">Phân công:</span>
              <span class="font-bold text-orange-950 bg-orange-50 px-2 py-0.5 rounded border border-orange-200 text-[11px]">
                👥 {{ item.assignedStudentsCount }} học sinh
              </span>
              <span v-if="item.completedStudentsCount !== undefined" class="text-emerald-700 font-semibold text-[11px]">
                (Đã nộp: {{ item.completedStudentsCount }}/{{ item.assignedStudentsCount }})
              </span>
            </div>
          </div>
        </div>

        <!-- Right actions & Progress -->
        <div class="flex flex-col sm:flex-row sm:items-center gap-4 self-end lg:self-center border-t lg:border-t-0 pt-3 lg:pt-0 border-orange-50 w-full lg:w-auto justify-between lg:justify-end">
          <!-- Submission progress bar -->
          <div class="min-w-[130px] text-left sm:text-right">
            <div class="text-[11px] font-semibold text-gray-600 flex items-center justify-between sm:justify-end gap-2">
              <span>Tỷ lệ hoàn thành:</span>
              <span class="text-orange-700 font-bold">
                {{ item.assignedStudentsCount > 0 ? Math.round(((item.completedStudentsCount || 0) / item.assignedStudentsCount) * 100) : 0 }}%
              </span>
            </div>
            <div class="w-full bg-gray-100 rounded-full h-2 mt-1.5 overflow-hidden border border-gray-200">
              <div
                class="bg-orange-500 h-2 rounded-full transition-all duration-500"
                :style="{
                  width: `${item.assignedStudentsCount > 0 ? ((item.completedStudentsCount || 0) / item.assignedStudentsCount) * 100 : 0}%`
                }"
              ></div>
            </div>
          </div>

          <!-- Action buttons group -->
          <div class="flex flex-wrap items-center gap-1.5">
            <!-- Quản lý học sinh giao bài -->
            <button
              type="button"
              @click="openManageStudentsModal(item)"
              class="px-3 py-1.5 rounded-xl border border-orange-200 text-orange-900 bg-orange-50/50 hover:bg-orange-100 text-xs font-semibold transition-colors cursor-pointer inline-flex items-center gap-1"
              title="Quản lý danh sách học sinh được giao bài & Giao lẻ"
            >
              <UserGroupIcon class="w-3.5 h-3.5 text-orange-600" />
              <span>Giao học sinh</span>
            </button>

            <!-- Xem đề & câu hỏi -->
            <button
              type="button"
              @click="openViewQuestionsModal(item)"
              class="px-2.5 py-1.5 rounded-xl border border-gray-200 text-gray-700 hover:bg-gray-100 text-xs font-semibold transition-colors cursor-pointer inline-flex items-center gap-1"
              title="Xem danh sách câu hỏi trong đề"
            >
              <EyeIcon class="w-3.5 h-3.5 text-gray-500" />
              <span>Xem đề</span>
            </button>

            <!-- Clone sang buổi khác -->
            <button
              type="button"
              @click="openCloneModal(item)"
              class="px-2.5 py-1.5 rounded-xl border border-blue-200 text-blue-800 bg-blue-50/50 hover:bg-blue-100 text-xs font-semibold transition-colors cursor-pointer inline-flex items-center gap-1"
              title="Nhân bản bài kiểm tra sang buổi học khác"
            >
              <DocumentDuplicateIcon class="w-3.5 h-3.5 text-blue-600" />
              <span>Nhân bản</span>
            </button>

            <!-- Edit -->
            <button
              type="button"
              @click="openEditModal(item)"
              class="p-1.5 rounded-xl hover:bg-orange-100 text-gray-600 hover:text-orange-900 transition-colors cursor-pointer"
              title="Sửa thông tin bài kiểm tra"
            >
              <PencilSquareIcon class="w-4 h-4" />
            </button>

            <!-- Delete -->
            <button
              type="button"
              @click="confirmDeleteAssignment(item)"
              class="p-1.5 rounded-xl hover:bg-rose-100 text-gray-400 hover:text-rose-600 transition-colors cursor-pointer"
              title="Xóa bài kiểm tra"
            >
              <TrashIcon class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Modal 1: Tạo bài kiểm tra mới ─────────────────────────────────── -->
    <div
      v-if="showCreateModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-3xl w-full p-6 shadow-2xl border border-orange-100 space-y-4 max-h-[92vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">Tạo bài kiểm tra đính kèm buổi học</h3>
            <p class="text-xs text-gray-500 mt-0.5">
              Buổi học: <strong class="text-orange-800">{{ selectedLesson?.lessonDisplayTitle }}</strong>
              • Lớp: <strong class="text-orange-800">{{ selectedClass?.name }}</strong>
            </p>
          </div>
          <button @click="showCreateModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <form @submit.prevent="submitCreateAssignment" class="space-y-4 text-xs">
          <!-- Basic Info -->
          <div class="space-y-3 p-3.5 rounded-2xl bg-orange-50/40 border border-orange-100">
            <h4 class="font-bold text-orange-950 text-xs">1. Thông tin chung</h4>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">
                Tiêu đề bài kiểm tra <span class="text-rose-500">*</span>
              </label>
              <input
                type="text"
                v-model="createForm.title"
                required
                placeholder="VD: Kiểm tra 15 phút - Tính nhanh dãy số quy luật"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label class="block font-semibold text-gray-700 mb-1">
                  Thời gian mở đề <span class="text-rose-500">*</span>
                </label>
                <input
                  type="datetime-local"
                  v-model="createForm.startTime"
                  required
                  class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
                />
              </div>

              <div>
                <label class="block font-semibold text-gray-700 mb-1">
                  Hạn chót nộp bài <span class="text-rose-500">*</span>
                </label>
                <input
                  type="datetime-local"
                  v-model="createForm.deadline"
                  required
                  class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
                />
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div>
                <label class="block font-semibold text-gray-700 mb-1">Thời gian làm (phút)</label>
                <input
                  type="number"
                  v-model.number="createForm.timeLimitMinutes"
                  min="1"
                  max="1440"
                  required
                  class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
                />
              </div>
              <div>
                <label class="block font-semibold text-gray-700 mb-1">Số lần làm tối đa</label>
                <input
                  type="number"
                  v-model.number="createForm.maxAttempt"
                  min="1"
                  max="10"
                  required
                  class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
                />
              </div>
              <div>
                <label class="block font-semibold text-gray-700 mb-1">Trạng thái</label>
                <select
                  v-model="createForm.status"
                  class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-semibold"
                >
                  <option value="OPEN">Đang mở (OPEN)</option>
                  <option value="DRAFT">Bản nháp (DRAFT)</option>
                  <option value="SCHEDULED">Lên lịch (SCHEDULED)</option>
                </select>
              </div>
            </div>
          </div>

          <!-- Question Picker from Question Bank -->
          <div class="space-y-3 p-3.5 rounded-2xl bg-white border border-orange-200">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <div>
                <h4 class="font-bold text-orange-950 text-xs">2. Chọn câu hỏi từ Ngân hàng đề</h4>
                <p class="text-[11px] text-gray-500">Tích chọn các câu hỏi đưa vào bài kiểm tra và đặt điểm số</p>
              </div>
              <div class="text-right">
                <span class="text-xs font-bold text-orange-900 bg-orange-100 px-2.5 py-1 rounded-full">
                  Đã chọn: {{ createForm.selectedQuestions.length }} câu ({{ totalPointsCreate }}đ)
                </span>
              </div>
            </div>

            <!-- Choose bank -->
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Chọn Ngân hàng câu hỏi nguồn</label>
              <select
                v-model="createModalBankId"
                @change="loadBankQuestionsForModal"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium"
              >
                <option :value="null">-- Chọn ngân hàng câu hỏi --</option>
                <option v-for="b in allQuestionBanks" :key="b.id" :value="b.id">
                  {{ b.name }} ({{ b.totalQuestions || 0 }} câu) - {{ b.courseName }}
                </option>
              </select>
            </div>

            <!-- Bank questions checklist -->
            <div v-if="loadingModalQuestions" class="py-6 text-center text-gray-400">
              <ArrowPathIcon class="w-5 h-5 animate-spin mx-auto mb-1 text-orange-500" />
              Đang tải danh sách câu hỏi...
            </div>

            <div
              v-else-if="modalBankQuestions.length > 0"
              class="max-h-56 overflow-y-auto space-y-2 border border-orange-100 rounded-xl p-2.5 bg-orange-50/20"
            >
              <div
                v-for="q in modalBankQuestions"
                :key="q.id"
                class="flex items-start gap-3 p-2 bg-white rounded-lg border border-orange-100 hover:border-orange-200"
              >
                <input
                  type="checkbox"
                  :checked="isQuestionSelected(q.id)"
                  @change="toggleSelectQuestionForCreate(q)"
                  class="mt-1 rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
                />
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-gray-100 text-gray-700">
                      {{ q.difficulty }}
                    </span>
                    <span class="text-[10px] font-semibold text-orange-800">
                      {{ q.type === 'MULTIPLE_CHOICE' ? 'Trắc nghiệm' : q.type === 'FILL_IN' ? 'Điền khuyết' : 'Tự luận' }}
                    </span>
                  </div>
                  <p class="text-xs text-gray-900 line-clamp-2">{{ q.content }}</p>
                </div>

                <!-- Point input if selected -->
                <div v-if="isQuestionSelected(q.id)" class="flex items-center gap-1">
                  <input
                    type="number"
                    step="0.5"
                    min="0.1"
                    max="100"
                    :value="getQuestionPoint(q.id)"
                    @input="updateQuestionPoint(q.id, $event.target.value)"
                    class="w-14 text-center rounded border border-orange-300 p-1 text-xs font-bold text-orange-900"
                    title="Điểm số câu này"
                  />
                  <span class="text-[10px] text-gray-500 font-medium">đ</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Assignment Scope: All Class vs Specific Students -->
          <div class="space-y-3 p-3.5 rounded-2xl bg-orange-50/40 border border-orange-100">
            <h4 class="font-bold text-orange-950 text-xs">3. Đối tượng học sinh được giao</h4>
            <div class="space-y-2">
              <label class="flex items-center gap-2 cursor-pointer font-semibold text-gray-800">
                <input
                  type="radio"
                  :value="true"
                  v-model="createForm.assignAllStudentsInClass"
                  class="text-orange-500 focus:ring-orange-400"
                />
                <span>Giao cho toàn bộ học sinh trong lớp ({{ classStudents.length }} học sinh)</span>
              </label>

              <label class="flex items-center gap-2 cursor-pointer font-semibold text-gray-800">
                <input
                  type="radio"
                  :value="false"
                  v-model="createForm.assignAllStudentsInClass"
                  class="text-orange-500 focus:ring-orange-400"
                />
                <span>Giao riêng lẻ cho từng học sinh cụ thể</span>
              </label>
            </div>

            <!-- Specific Students Checklist -->
            <div
              v-if="!createForm.assignAllStudentsInClass"
              class="mt-2.5 max-h-44 overflow-y-auto space-y-1.5 border border-orange-200 rounded-xl p-3 bg-white"
            >
              <p class="text-[11px] font-bold text-gray-500 mb-1">
                Chọn học sinh nhận bài kiểm tra (Đã chọn: {{ createForm.specificStudentIds.length }}):
              </p>
              <div
                v-for="st in classStudents"
                :key="st.id || st.studentId"
                class="flex items-center gap-2.5 py-1 px-2 hover:bg-orange-50/50 rounded-lg cursor-pointer"
                @click="toggleSpecificStudent(st.id || st.studentId)"
              >
                <input
                  type="checkbox"
                  :checked="createForm.specificStudentIds.includes(st.id || st.studentId)"
                  @click.stop
                  @change="toggleSpecificStudent(st.id || st.studentId)"
                  class="rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
                />
                <span class="font-semibold text-gray-800">{{ st.fullName || st.studentName }}</span>
                <span class="text-[10px] text-gray-400">({{ st.studentCode || 'HS' }})</span>
              </div>
            </div>
          </div>

          <!-- Submit buttons -->
          <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showCreateModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmittingCreate"
              class="px-5 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="isSubmittingCreate" class="w-3.5 h-3.5 animate-spin" />
              <span>Phát hành bài kiểm tra</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ─── Modal 2: Quản lý học sinh giao bài & Giao lẻ ──────────────────── -->
    <div
      v-if="showManageStudentsModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-2xl w-full p-6 shadow-2xl border border-orange-100 space-y-4 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">Quản lý giao bài cho học sinh</h3>
            <p class="text-xs text-gray-500 mt-0.5">
              Bài kiểm tra: <strong class="text-orange-900">{{ activeAssignment?.title }}</strong>
            </p>
          </div>
          <button @click="showManageStudentsModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <div v-if="loadingAssignmentDetail" class="py-12 text-center">
          <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto text-orange-500 mb-2" />
          <p class="text-xs text-gray-400">Đang tải danh sách học sinh...</p>
        </div>

        <div v-else class="space-y-5 text-xs">
          <!-- List of currently assigned students -->
          <div>
            <div class="flex items-center justify-between mb-2">
              <h4 class="font-bold text-gray-800">
                Danh sách học sinh đã được giao bài ({{ activeAssignmentDetail?.assignedStudents?.length || 0 }})
              </h4>
            </div>

            <div v-if="!activeAssignmentDetail?.assignedStudents?.length" class="p-6 text-center bg-gray-50 rounded-xl text-gray-400">
              Chưa có học sinh nào được giao bài kiểm tra này.
            </div>

            <div v-else class="border border-orange-100 rounded-xl overflow-hidden">
              <table class="w-full text-left text-xs">
                <thead class="bg-orange-50/70 border-b border-orange-100 text-orange-950 font-bold uppercase text-[10px]">
                  <tr>
                    <th class="py-2.5 px-3">Học sinh</th>
                    <th class="py-2.5 px-3 text-center">Trạng thái</th>
                    <th class="py-2.5 px-3 text-center">Điểm số</th>
                    <th class="py-2.5 px-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-orange-50">
                  <tr v-for="st in activeAssignmentDetail.assignedStudents" :key="st.id" class="hover:bg-orange-50/30">
                    <td class="py-2.5 px-3">
                      <span class="font-bold text-gray-900">{{ st.studentName }}</span>
                      <p v-if="st.completedAt" class="text-[10px] text-gray-400">Nộp: {{ formatDateTime(st.completedAt) }}</p>
                    </td>
                    <td class="py-2.5 px-3 text-center">
                      <span
                        :class="[
                          'px-2 py-0.5 rounded-full font-bold text-[10px] inline-block',
                          st.status === 'GRADED' ? 'bg-emerald-100 text-emerald-800' :
                          st.status === 'SUBMITTED' ? 'bg-blue-100 text-blue-800' :
                          'bg-amber-100 text-amber-800'
                        ]"
                      >
                        {{ st.status === 'GRADED' ? 'Đã chấm' : st.status === 'SUBMITTED' ? 'Đã nộp bài' : 'Đang làm' }}
                      </span>
                    </td>
                    <td class="py-2.5 px-3 text-center font-bold text-gray-800">
                      {{ st.score !== null && st.score !== undefined ? `${st.score}đ` : '--' }}
                    </td>
                    <td class="py-2.5 px-3 text-right">
                      <button
                        v-if="st.status === 'ASSIGNED'"
                        type="button"
                        @click="handleUnassignStudent(st.studentId)"
                        class="px-2.5 py-1 rounded-lg text-rose-600 hover:bg-rose-50 font-semibold cursor-pointer text-[11px]"
                      >
                        Hủy giao
                      </button>
                      <span v-else class="text-[10px] text-gray-400 italic">Đã nộp bài</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Section: Giao thêm cho học sinh lẻ trong lớp -->
          <div class="p-3.5 rounded-2xl bg-orange-50/60 border border-orange-200 space-y-3">
            <h4 class="font-bold text-orange-950 flex items-center gap-1.5">
              <UserPlusIcon class="w-4 h-4 text-orange-600" />
              Giao thêm riêng lẻ cho học sinh chưa nhận bài
            </h4>

            <div v-if="unassignedClassStudents.length === 0" class="text-[11px] text-gray-500">
              Tất cả học sinh trong lớp đều đã được giao bài kiểm tra này.
            </div>

            <div v-else class="space-y-2">
              <div class="max-h-36 overflow-y-auto space-y-1 bg-white p-2 rounded-xl border border-orange-100">
                <div
                  v-for="st in unassignedClassStudents"
                  :key="st.id || st.studentId"
                  class="flex items-center gap-2 py-1 px-2 hover:bg-orange-50/50 rounded cursor-pointer"
                  @click="toggleAdditionalStudent(st.id || st.studentId)"
                >
                  <input
                    type="checkbox"
                    :checked="additionalStudentIds.includes(st.id || st.studentId)"
                    @click.stop
                    @change="toggleAdditionalStudent(st.id || st.studentId)"
                    class="rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
                  />
                  <span class="font-semibold text-gray-800">{{ st.fullName || st.studentName }}</span>
                  <span class="text-[10px] text-gray-400">({{ st.studentCode || 'HS' }})</span>
                </div>
              </div>

              <div class="flex items-center justify-between pt-1">
                <span class="text-[11px] text-gray-500">
                  Đã chọn: <strong class="text-orange-900">{{ additionalStudentIds.length }}</strong> học sinh
                </span>
                <button
                  type="button"
                  :disabled="additionalStudentIds.length === 0 || isAssigningAdditional"
                  @click="handleAssignAdditionalStudents"
                  class="px-4 py-1.5 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white font-bold text-xs shadow-xs cursor-pointer inline-flex items-center gap-1.5"
                >
                  <ArrowPathIcon v-if="isAssigningAdditional" class="w-3.5 h-3.5 animate-spin" />
                  <span>Xác nhận giao bài</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Modal 3: Xem chi tiết câu hỏi trong đề ────────────────────────── -->
    <div
      v-if="showViewQuestionsModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-2xl w-full p-6 shadow-2xl border border-orange-100 space-y-4 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">Cơ cấu câu hỏi trong đề thi</h3>
            <p class="text-xs text-gray-500 mt-0.5">
              Bài kiểm tra: <strong class="text-orange-900">{{ activeAssignment?.title }}</strong>
            </p>
          </div>
          <button @click="showViewQuestionsModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <div v-if="loadingAssignmentDetail" class="py-12 text-center">
          <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto text-orange-500 mb-2" />
          <p class="text-xs text-gray-400">Đang tải chi tiết đề thi...</p>
        </div>

        <div v-else class="space-y-3 text-xs">
          <div
            v-for="(q, idx) in activeAssignmentDetail?.questions || []"
            :key="q.questionAssignmentId || q.questionId"
            class="p-3.5 rounded-xl border border-orange-100 bg-orange-50/20 space-y-2"
          >
            <div class="flex items-center justify-between">
              <span class="font-bold text-orange-900 bg-orange-100 px-2 py-0.5 rounded text-[11px]">
                Câu {{ idx + 1 }} ({{ q.pointValue }} điểm)
              </span>
              <span class="text-[10px] font-semibold text-gray-500">
                {{ q.difficulty }} • {{ q.type === 'MULTIPLE_CHOICE' ? 'Trắc nghiệm' : q.type === 'FILL_IN' ? 'Điền khuyết' : 'Tự luận' }}
              </span>
            </div>

            <p class="font-semibold text-gray-900 whitespace-pre-line leading-relaxed">
              {{ q.content }}
            </p>

            <div v-if="q.imageUrl" class="max-w-sm rounded-lg overflow-hidden border border-orange-100">
              <img :src="q.imageUrl" alt="Hình minh họa" class="w-full max-h-40 object-contain bg-white" />
            </div>

            <div class="p-2 rounded-lg bg-emerald-50/80 border border-emerald-100 text-emerald-950 text-[11px]">
              <strong>Đáp án:</strong> {{ q.answer }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Modal 4: Nhân bản bài kiểm tra sang buổi học khác ──────────────── -->
    <div
      v-if="showCloneModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl border border-orange-100 space-y-4">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">Nhân bản bài kiểm tra sang buổi học mới</h3>
            <p class="text-xs text-gray-500 mt-0.5">
              Từ bài: <strong class="text-orange-900">{{ activeAssignment?.title }}</strong>
            </p>
          </div>
          <button @click="showCloneModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <form @submit.prevent="submitCloneAssignment" class="space-y-3.5 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Chọn buổi học đích nhận bài kiểm tra <span class="text-rose-500">*</span>
            </label>
            <select
              v-model="cloneForm.targetLessonId"
              required
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-semibold"
            >
              <option :value="null" disabled>-- Chọn buổi học đích --</option>
              <option v-for="l in sortedLessons" :key="l.id" :value="l.id">
                {{ l.lessonDisplayTitle }} ({{ formatDate(l.date) }})
              </option>
            </select>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Tiêu đề bài kiểm tra mới</label>
            <input
              type="text"
              v-model="cloneForm.newTitle"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Thời gian mở đề mới</label>
              <input
                type="datetime-local"
                v-model="cloneForm.startTime"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Hạn nộp mới</label>
              <input
                type="datetime-local"
                v-model="cloneForm.deadline"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
          </div>

          <div class="pt-2">
            <label class="flex items-center gap-2 cursor-pointer font-semibold text-gray-800">
              <input
                type="checkbox"
                v-model="cloneForm.assignAllStudentsInClass"
                class="rounded text-orange-500 focus:ring-orange-400"
              />
              <span>Tự động giao cho toàn bộ học sinh lớp của buổi học đó</span>
            </label>
          </div>

          <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showCloneModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmittingClone"
              class="px-5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="isSubmittingClone" class="w-3.5 h-3.5 animate-spin" />
              <span>Xác nhận nhân bản</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ─── Modal 5: Chỉnh sửa bài kiểm tra ──────────────────────────────── -->
    <div
      v-if="showEditModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl border border-orange-100 space-y-4">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <h3 class="text-base font-bold text-orange-950">Chỉnh sửa bài kiểm tra</h3>
          <button @click="showEditModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <form @submit.prevent="submitEditAssignment" class="space-y-3.5 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Tiêu đề bài kiểm tra <span class="text-rose-500">*</span>
            </label>
            <input
              type="text"
              v-model="editForm.title"
              required
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Thời gian bắt đầu</label>
              <input
                type="datetime-local"
                v-model="editForm.startTime"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Hạn nộp</label>
              <input
                type="datetime-local"
                v-model="editForm.deadline"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Thời gian làm (phút)</label>
              <input
                type="number"
                v-model.number="editForm.timeLimitMinutes"
                min="1"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Số lần làm tối đa</label>
              <input
                type="number"
                v-model.number="editForm.maxAttempt"
                min="1"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>
            <div>
              <label class="block font-semibold text-gray-700 mb-1">Trạng thái</label>
              <select
                v-model="editForm.status"
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-semibold"
              >
                <option value="OPEN">Đang mở (OPEN)</option>
                <option value="CLOSED">Đã đóng (CLOSED)</option>
                <option value="DRAFT">Bản nháp (DRAFT)</option>
                <option value="SCHEDULED">Lên lịch (SCHEDULED)</option>
              </select>
            </div>
          </div>

          <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showEditModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmittingEdit"
              class="px-5 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="isSubmittingEdit" class="w-3.5 h-3.5 animate-spin" />
              <span>Lưu thay đổi</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import {
  ChevronRightIcon,
  PlusIcon,
  AcademicCapIcon,
  CalendarDaysIcon,
  MagnifyingGlassIcon,
  ArrowPathIcon,
  ClipboardDocumentCheckIcon,
  ClipboardDocumentListIcon,
  ClockIcon,
  UserGroupIcon,
  UserPlusIcon,
  EyeIcon,
  DocumentDuplicateIcon,
  PencilSquareIcon,
  TrashIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'

import classService from '@/services/class.service'
import lessonService from '@/services/lesson.service'
import assignmentService from '@/services/assignment.service'
import questionBankService from '@/services/questionBank.service'
import { useUserState } from '@/services/user-state'
import { getProfileId } from '@/services/auth-storage'

// ─── Current User & State ──────────────────────────────────────────────────
const { profile, fetchProfile } = useUserState()

const classes = ref([])
const lessons = ref([])
const classStudents = ref([])
const assignments = ref([])
const allQuestionBanks = ref([])

const selectedClassId = ref(null)
const selectedLessonId = ref(null)
const searchQuery = ref('')

const isLoading = ref(false)
const isLoadingClasses = ref(false)
const isLoadingLessons = ref(false)

const feedback = reactive({
  message: '',
  type: 'success',
})

function notify(message, type = 'success') {
  feedback.message = message
  feedback.type = type
  setTimeout(() => {
    if (feedback.message === message) feedback.message = ''
  }, 4500)
}

// ─── Computed ──────────────────────────────────────────────────────────────
const selectedClass = computed(() => {
  return classes.value.find(c => c.id === selectedClassId.value) || null
})

const sortedLessons = computed(() => {
  const list = [...lessons.value]
  list.sort((a, b) => new Date(a.date || 0) - new Date(b.date || 0))
  return list.map((l, index) => ({
    ...l,
    lessonNumber: index + 1,
    lessonDisplayTitle: `Buổi ${index + 1}: ${l.title || 'Buổi học'}`,
  }))
})

const selectedLesson = computed(() => {
  return sortedLessons.value.find(l => l.id === selectedLessonId.value) || null
})

const filteredAssignments = computed(() => {
  return assignments.value.filter(a => {
    return (
      !searchQuery.value.trim() ||
      a.title?.toLowerCase().includes(searchQuery.value.toLowerCase())
    )
  })
})

// ─── Formatting helpers ────────────────────────────────────────────────────
function formatDate(dateStr) {
  if (!dateStr) return '--'
  try {
    const d = new Date(dateStr)
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
  } catch {
    return dateStr
  }
}

function formatDateTime(dateStr) {
  if (!dateStr) return '--'
  try {
    const d = new Date(dateStr)
    return d.toLocaleString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
  } catch {
    return dateStr
  }
}

function toLocalDatetimeString(date) {
  const pad = n => (n < 10 ? '0' + n : n)
  const y = date.getFullYear()
  const m = pad(date.getMonth() + 1)
  const d = pad(date.getDate())
  const hh = pad(date.getHours())
  const mm = pad(date.getMinutes())
  return `${y}-${m}-${d}T${hh}:${mm}`
}

// ─── Data Loading: Classes & Lessons ───────────────────────────────────────
async function loadClasses() {
  isLoadingClasses.value = true
  try {
    if (!profile.value) {
      await fetchProfile()
    }
    const teacherProfileId = getProfileId('TEACHER')
    const list = await classService.getAssignedClasses(profile.value, teacherProfileId)
    classes.value = Array.isArray(list) ? list : []

    if (classes.value.length > 0) {
      selectedClassId.value = classes.value[0].id
      await handleClassChange()
    }
  } catch (err) {
    console.error('Lỗi tải danh sách lớp học:', err)
    notify('Không thể tải danh sách lớp học.', 'error')
  } finally {
    isLoadingClasses.value = false
  }
}

async function handleClassChange() {
  if (!selectedClassId.value) return
  isLoadingLessons.value = true
  try {
    // 1. Tải danh sách buổi học
    const res = await lessonService.getByClass(selectedClassId.value)
    lessons.value = Array.isArray(res) ? res : []

    // 2. Tải học sinh của lớp
    const studentRes = await classService.getStudents(selectedClassId.value)
    classStudents.value = Array.isArray(studentRes) ? studentRes : (studentRes?.items || [])

    // 3. Chọn buổi học đầu tiên hoặc buổi gần nhất
    if (sortedLessons.value.length > 0) {
      selectedLessonId.value = sortedLessons.value[0].id
      await handleLessonChange()
    } else {
      selectedLessonId.value = null
      assignments.value = []
    }
  } catch (err) {
    console.error('Lỗi tải buổi học:', err)
    notify('Không thể tải danh sách buổi học của lớp.', 'error')
  } finally {
    isLoadingLessons.value = false
  }
}

async function handleLessonChange() {
  if (!selectedLessonId.value) return
  await reloadLessonAssignments()
}

async function reloadLessonAssignments() {
  if (!selectedLessonId.value) return
  isLoading.value = true
  try {
    const res = await assignmentService.getByLesson(selectedLessonId.value)
    assignments.value = Array.isArray(res) ? res : []
  } catch (err) {
    console.error('Lỗi tải bài kiểm tra của buổi học:', err)
    notify('Không thể tải bài kiểm tra của buổi học.', 'error')
  } finally {
    isLoading.value = false
  }
}

// ─── Modal 1: Create Assignment ───────────────────────────────────────────
const showCreateModal = ref(false)
const isSubmittingCreate = ref(false)
const createModalBankId = ref(null)
const modalBankQuestions = ref([])
const loadingModalQuestions = ref(false)

const createForm = reactive({
  title: '',
  startTime: '',
  deadline: '',
  timeLimitMinutes: 30,
  maxAttempt: 1,
  status: 'OPEN',
  selectedQuestions: [], // [{ questionId, orderIndex, pointValue }]
  assignAllStudentsInClass: true,
  specificStudentIds: [],
})

const totalPointsCreate = computed(() => {
  return createForm.selectedQuestions.reduce((sum, q) => sum + (Number(q.pointValue) || 0), 0)
})

async function openCreateModal() {
  if (!selectedLessonId.value) return

  // Load all banks for selection
  try {
    const banks = await questionBankService.getAll()
    allQuestionBanks.value = Array.isArray(banks) ? banks : []
  } catch (err) {
    console.error('Lỗi tải ngân hàng câu hỏi:', err)
  }

  const now = new Date()
  const nextWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)

  createForm.title = `Bài tập rèn luyện - ${selectedLesson.value?.lessonDisplayTitle || 'Buổi học'}`
  createForm.startTime = toLocalDatetimeString(now)
  createForm.deadline = toLocalDatetimeString(nextWeek)
  createForm.timeLimitMinutes = 30
  createForm.maxAttempt = 1
  createForm.status = 'OPEN'
  createForm.selectedQuestions = []
  createForm.assignAllStudentsInClass = true
  createForm.specificStudentIds = []

  createModalBankId.value = allQuestionBanks.value[0]?.id || null
  if (createModalBankId.value) {
    await loadBankQuestionsForModal()
  }

  showCreateModal.value = true
}

async function loadBankQuestionsForModal() {
  if (!createModalBankId.value) {
    modalBankQuestions.value = []
    return
  }
  loadingModalQuestions.value = true
  try {
    const res = await questionBankService.getQuestions(createModalBankId.value)
    modalBankQuestions.value = Array.isArray(res) ? res : []
  } catch (err) {
    console.error('Lỗi tải câu hỏi ngân hàng:', err)
  } finally {
    loadingModalQuestions.value = false
  }
}

function isQuestionSelected(qId) {
  return createForm.selectedQuestions.some(x => x.questionId === qId)
}

function getQuestionPoint(qId) {
  const found = createForm.selectedQuestions.find(x => x.questionId === qId)
  return found ? found.pointValue : 1.0
}

function updateQuestionPoint(qId, val) {
  const found = createForm.selectedQuestions.find(x => x.questionId === qId)
  if (found) {
    found.pointValue = parseFloat(val) || 1.0
  }
}

function toggleSelectQuestionForCreate(q) {
  const idx = createForm.selectedQuestions.findIndex(x => x.questionId === q.id)
  if (idx >= 0) {
    createForm.selectedQuestions.splice(idx, 1)
  } else {
    createForm.selectedQuestions.push({
      questionId: q.id,
      orderIndex: createForm.selectedQuestions.length + 1,
      pointValue: 1.0,
    })
  }
}

function toggleSpecificStudent(stId) {
  const idx = createForm.specificStudentIds.indexOf(stId)
  if (idx >= 0) {
    createForm.specificStudentIds.splice(idx, 1)
  } else {
    createForm.specificStudentIds.push(stId)
  }
}

async function submitCreateAssignment() {
  if (!createForm.title.trim()) {
    notify('Vui lòng nhập tiêu đề bài kiểm tra!', 'error')
    return
  }
  if (!createForm.assignAllStudentsInClass && createForm.specificStudentIds.length === 0) {
    notify('Vui lòng chọn ít nhất một học sinh nhận bài!', 'error')
    return
  }

  isSubmittingCreate.value = true
  try {
    const payload = {
      lessonId: selectedLessonId.value,
      title: createForm.title.trim(),
      startTime: new Date(createForm.startTime).toISOString(),
      deadline: new Date(createForm.deadline).toISOString(),
      timeLimitMinutes: createForm.timeLimitMinutes,
      maxAttempt: createForm.maxAttempt,
      status: createForm.status,
      questions: createForm.selectedQuestions.map((q, i) => ({
        questionId: q.questionId,
        orderIndex: i + 1,
        pointValue: q.pointValue || 1.0,
      })),
      assignAllStudentsInClass: createForm.assignAllStudentsInClass,
      specificStudentIds: createForm.assignAllStudentsInClass ? null : createForm.specificStudentIds,
    }

    await assignmentService.create(payload)
    notify('Tạo bài kiểm tra đính kèm buổi học thành công!')
    showCreateModal.value = false
    await reloadLessonAssignments()
  } catch (err) {
    console.error('Lỗi tạo bài kiểm tra:', err)
    notify('Không thể tạo bài kiểm tra.', 'error')
  } finally {
    isSubmittingCreate.value = false
  }
}

// ─── Modal 2: Quản lý học sinh giao bài & Giao lẻ ─────────────────────────
const showManageStudentsModal = ref(false)
const activeAssignment = ref(null)
const activeAssignmentDetail = ref(null)
const loadingAssignmentDetail = ref(false)
const additionalStudentIds = ref([])
const isAssigningAdditional = ref(false)

const unassignedClassStudents = computed(() => {
  if (!activeAssignmentDetail.value?.assignedStudents) return classStudents.value
  const assignedSet = new Set(activeAssignmentDetail.value.assignedStudents.map(s => s.studentId))
  return classStudents.value.filter(s => !assignedSet.has(s.id || s.studentId))
})

async function openManageStudentsModal(item) {
  activeAssignment.value = item
  additionalStudentIds.value = []
  showManageStudentsModal.value = true
  await loadAssignmentDetail(item.id)
}

async function loadAssignmentDetail(id) {
  loadingAssignmentDetail.value = true
  try {
    const res = await assignmentService.getById(id)
    activeAssignmentDetail.value = res
  } catch (err) {
    console.error('Lỗi lấy chi tiết bài kiểm tra:', err)
    notify('Không thể lấy chi tiết bài kiểm tra.', 'error')
  } finally {
    loadingAssignmentDetail.value = false
  }
}

function toggleAdditionalStudent(stId) {
  const idx = additionalStudentIds.value.indexOf(stId)
  if (idx >= 0) {
    additionalStudentIds.value.splice(idx, 1)
  } else {
    additionalStudentIds.value.push(stId)
  }
}

async function handleAssignAdditionalStudents() {
  if (additionalStudentIds.value.length === 0 || !activeAssignment.value) return
  isAssigningAdditional.value = true
  try {
    await assignmentService.assignStudents(activeAssignment.value.id, additionalStudentIds.value)
    notify(`Đã giao thêm bài cho ${additionalStudentIds.value.length} học sinh thành công!`)
    additionalStudentIds.value = []
    await loadAssignmentDetail(activeAssignment.value.id)
    await reloadLessonAssignments()
  } catch (err) {
    console.error('Lỗi giao thêm bài:', err)
    notify('Không thể giao thêm bài cho học sinh.', 'error')
  } finally {
    isAssigningAdditional.value = false
  }
}

async function handleUnassignStudent(studentId) {
  const confirmed = confirm('Bạn có chắc chắn muốn hủy giao bài cho học sinh này?')
  if (!confirmed || !activeAssignment.value) return

  try {
    await assignmentService.unassignStudent(activeAssignment.value.id, studentId)
    notify('Đã hủy giao bài cho học sinh.')
    await loadAssignmentDetail(activeAssignment.value.id)
    await reloadLessonAssignments()
  } catch (err) {
    console.error('Lỗi hủy giao bài:', err)
    notify('Không thể hủy giao bài.', 'error')
  }
}

// ─── Modal 3: View Questions Modal ────────────────────────────────────────
const showViewQuestionsModal = ref(false)

async function openViewQuestionsModal(item) {
  activeAssignment.value = item
  showViewQuestionsModal.value = true
  await loadAssignmentDetail(item.id)
}

// ─── Modal 4: Clone Assignment Modal ──────────────────────────────────────
const showCloneModal = ref(false)
const isSubmittingClone = ref(false)
const cloneForm = reactive({
  targetLessonId: null,
  newTitle: '',
  startTime: '',
  deadline: '',
  assignAllStudentsInClass: true,
})

function openCloneModal(item) {
  activeAssignment.value = item
  const now = new Date()
  const nextWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)

  cloneForm.targetLessonId = selectedLessonId.value
  cloneForm.newTitle = `${item.title} (Bản sao)`
  cloneForm.startTime = toLocalDatetimeString(now)
  cloneForm.deadline = toLocalDatetimeString(nextWeek)
  cloneForm.assignAllStudentsInClass = true

  showCloneModal.value = true
}

async function submitCloneAssignment() {
  if (!cloneForm.targetLessonId || !activeAssignment.value) {
    notify('Vui lòng chọn buổi học đích!', 'error')
    return
  }

  isSubmittingClone.value = true
  try {
    const payload = {
      sourceAssignmentId: activeAssignment.value.id,
      targetLessonId: cloneForm.targetLessonId,
      newTitle: cloneForm.newTitle.trim() || null,
      startTime: cloneForm.startTime ? new Date(cloneForm.startTime).toISOString() : null,
      deadline: cloneForm.deadline ? new Date(cloneForm.deadline).toISOString() : null,
      assignAllStudentsInClass: cloneForm.assignAllStudentsInClass,
    }

    await assignmentService.clone(payload)
    notify('Nhân bản bài kiểm tra sang buổi học thành công!')
    showCloneModal.value = false
    if (cloneForm.targetLessonId === selectedLessonId.value) {
      await reloadLessonAssignments()
    }
  } catch (err) {
    console.error('Lỗi nhân bản bài kiểm tra:', err)
    notify('Không thể nhân bản bài kiểm tra.', 'error')
  } finally {
    isSubmittingClone.value = false
  }
}

// ─── Modal 5: Edit Assignment Modal ───────────────────────────────────────
const showEditModal = ref(false)
const isSubmittingEdit = ref(false)
const editForm = reactive({
  id: 0,
  title: '',
  startTime: '',
  deadline: '',
  timeLimitMinutes: 30,
  maxAttempt: 1,
  status: 'OPEN',
})

function openEditModal(item) {
  editForm.id = item.id
  editForm.title = item.title
  editForm.startTime = item.startTime ? toLocalDatetimeString(new Date(item.startTime)) : ''
  editForm.deadline = item.deadline ? toLocalDatetimeString(new Date(item.deadline)) : ''
  editForm.timeLimitMinutes = item.timeLimitMinutes || 30
  editForm.maxAttempt = item.maxAttempt || 1
  editForm.status = item.status || 'OPEN'
  showEditModal.value = true
}

async function submitEditAssignment() {
  isSubmittingEdit.value = true
  try {
    const payload = {
      title: editForm.title.trim(),
      startTime: new Date(editForm.startTime).toISOString(),
      deadline: new Date(editForm.deadline).toISOString(),
      timeLimitMinutes: editForm.timeLimitMinutes,
      maxAttempt: editForm.maxAttempt,
      status: editForm.status,
    }

    await assignmentService.update(editForm.id, payload)
    notify('Cập nhật bài kiểm tra thành công!')
    showEditModal.value = false
    await reloadLessonAssignments()
  } catch (err) {
    console.error('Lỗi cập nhật bài kiểm tra:', err)
    notify('Không thể cập nhật bài kiểm tra.', 'error')
  } finally {
    isSubmittingEdit.value = false
  }
}

// ─── Delete Assignment ────────────────────────────────────────────────────
async function confirmDeleteAssignment(item) {
  const confirmed = confirm(`Bạn có chắc chắn muốn xóa bài kiểm tra "${item.title}"?`)
  if (!confirmed) return

  try {
    await assignmentService.delete(item.id)
    notify(`Đã xóa bài kiểm tra "${item.title}".`)
    await reloadLessonAssignments()
  } catch (err) {
    console.error('Lỗi xóa bài kiểm tra:', err)
    notify('Không thể xóa bài kiểm tra.', 'error')
  }
}

// ─── Lifecycle ─────────────────────────────────────────────────────────────
onMounted(async () => {
  await loadClasses()
})
</script>
