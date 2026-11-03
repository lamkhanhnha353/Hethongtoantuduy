<template>
  <div class="space-y-6">
    <!-- ─── Page Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div>
        <h1 class="text-xl sm:text-2xl font-bold text-orange-950">Ngân hàng câu hỏi</h1>
        <p class="text-xs sm:text-sm text-gray-500 mt-0.5">
          Quản lý ngân hàng câu hỏi theo khóa học, phân loại độ khó và xây dựng kho đề bài chuẩn mực
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          @click="openBankModal()"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-white border border-orange-200 text-orange-900 hover:bg-orange-50 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <FolderPlusIcon class="w-4 h-4 text-orange-600" />
          Tạo ngân hàng mới
        </button>
        <button
          type="button"
          :disabled="!selectedBank"
          @click="openQuestionModal()"
          class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
        >
          <PlusIcon class="w-4 h-4" />
          Thêm câu hỏi
        </button>
      </div>
    </div>

    <!-- ─── Alert notification ────────────────────────────────────────────── -->
    <div
      v-if="actionMessage.text"
      :class="[
        'px-4 py-3 rounded-2xl text-xs font-medium flex items-center justify-between transition-all duration-300',
        actionMessage.isError
          ? 'bg-red-50 text-red-800 border border-red-200'
          : 'bg-emerald-50 text-emerald-800 border border-emerald-200'
      ]"
    >
      <div class="flex items-center gap-2">
        <span class="text-base">{{ actionMessage.isError ? '⚠️' : '✅' }}</span>
        <span>{{ actionMessage.text }}</span>
      </div>
      <button @click="actionMessage.text = ''" class="text-gray-400 hover:text-gray-600 text-sm font-bold">✕</button>
    </div>

    <!-- ─── Main Layout: Banks List (Left) & Questions List (Right) ────────── -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-5">
      <!-- ─── Left Column: Ngân hàng câu hỏi (4 cols) ───────────────────────── -->
      <div class="lg:col-span-4 space-y-4">
        <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
          <div class="flex items-center justify-between">
            <h2 class="text-sm font-bold text-orange-950 flex items-center gap-1.5">
              <FolderIcon class="w-4 h-4 text-orange-500" />
              Ngân hàng đề & chuyên đề
            </h2>
            <span class="text-xs bg-orange-100 text-orange-900 px-2 py-0.5 rounded-full font-bold">
              {{ questionBanks.length }}
            </span>
          </div>

          <!-- Course filter -->
          <div>
            <label class="block text-[11px] font-semibold text-gray-500 mb-1">Lọc theo khóa học</label>
            <select
              v-model="filterCourseId"
              @change="fetchBanks()"
              class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-2.5 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
            >
              <option :value="null">-- Tất cả khóa học --</option>
              <option v-for="c in courses" :key="c.id" :value="c.id">
                {{ c.name }}
              </option>
            </select>
          </div>

          <!-- Keyword search for bank -->
          <div>
            <SearchBar
              v-model="bankKeyword"
              placeholder="Tìm ngân hàng..."
              @update:modelValue="debouncedFetchBanks"
            />
          </div>

          <!-- Banks List -->
          <div v-if="loadingBanks" class="py-10 text-center">
            <ArrowPathIcon class="w-6 h-6 text-orange-400 animate-spin mx-auto mb-2" />
            <p class="text-xs text-gray-400">Đang tải danh sách ngân hàng...</p>
          </div>

          <div v-else-if="questionBanks.length === 0" class="py-8 text-center bg-orange-50/50 rounded-xl border border-orange-100">
            <p class="text-xs text-gray-500 font-medium">Chưa có ngân hàng câu hỏi nào</p>
            <button
              type="button"
              @click="openBankModal()"
              class="mt-2 text-xs text-orange-600 hover:text-orange-700 font-bold inline-flex items-center gap-1 cursor-pointer"
            >
              <PlusIcon class="w-3.5 h-3.5" /> Tạo ngay
            </button>
          </div>

          <div v-else class="space-y-2 max-h-[580px] overflow-y-auto pr-1">
            <div
              v-for="b in questionBanks"
              :key="b.id"
              @click="selectBank(b)"
              :class="[
                'p-3 rounded-xl border transition-all cursor-pointer flex flex-col gap-1',
                selectedBank?.id === b.id
                  ? 'bg-orange-50/90 border-orange-400 shadow-xs ring-1 ring-orange-300'
                  : 'bg-white border-orange-100 hover:border-orange-200 hover:bg-orange-50/30'
              ]"
            >
              <div class="flex items-start justify-between gap-2">
                <h3 class="text-xs font-bold text-gray-900 line-clamp-1">
                  {{ b.name }}
                </h3>
                <div class="flex items-center gap-1 flex-shrink-0" @click.stop>
                  <button
                    type="button"
                    title="Chỉnh sửa ngân hàng"
                    @click="openBankModal(b)"
                    class="p-1 hover:bg-orange-200/60 text-gray-500 hover:text-orange-900 rounded-md transition-colors"
                  >
                    <PencilSquareIcon class="w-3.5 h-3.5" />
                  </button>
                  <button
                    type="button"
                    title="Xóa ngân hàng"
                    @click="confirmDeleteBank(b)"
                    class="p-1 hover:bg-red-100 text-gray-400 hover:text-red-600 rounded-md transition-colors"
                  >
                    <TrashIcon class="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>

              <span class="text-[10px] text-orange-800 bg-orange-100/70 px-2 py-0.5 rounded-md font-medium inline-block w-fit">
                {{ b.courseName || 'Khóa học' }}
              </span>

              <p v-if="b.description" class="text-[11px] text-gray-500 line-clamp-2 mt-0.5">
                {{ b.description }}
              </p>

              <div class="flex items-center justify-between text-[11px] text-gray-400 pt-1 mt-1 border-t border-orange-50">
                <span>{{ b.totalQuestions || 0 }} câu hỏi</span>
                <span :class="selectedBank?.id === b.id ? 'text-orange-600 font-bold' : 'text-gray-400'">
                  {{ selectedBank?.id === b.id ? 'Đang chọn ▸' : 'Xem câu hỏi' }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Right Column: Danh sách câu hỏi trong ngân hàng (8 cols) ───────── -->
      <div class="lg:col-span-8 space-y-4">
        <!-- Bank Active Header & Filters -->
        <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-orange-50">
            <div>
              <span class="text-[11px] uppercase tracking-wider font-bold text-orange-600">
                Ngân hàng được chọn
              </span>
              <h2 class="text-base font-bold text-orange-950">
                {{ selectedBank ? selectedBank.name : 'Vui lòng chọn một ngân hàng' }}
              </h2>
              <p v-if="selectedBank" class="text-xs text-gray-500 mt-0.5">
                Thuộc khóa: <strong class="text-gray-700">{{ selectedBank.courseName }}</strong>
                <span v-if="selectedBank.description"> • {{ selectedBank.description }}</span>
              </p>
            </div>

            <div v-if="selectedBank" class="flex items-center gap-2">
              <button
                type="button"
                @click="openQuestionModal()"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-2xs transition-colors cursor-pointer"
              >
                <PlusIcon class="w-3.5 h-3.5" />
                Thêm câu hỏi
              </button>
            </div>
          </div>

          <!-- Questions Filter Bar -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label class="block text-[11px] font-semibold text-gray-500 mb-1">Mức độ khó</label>
              <select
                v-model="filterDifficulty"
                @change="fetchQuestions()"
                class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-2.5 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
              >
                <option value="">-- Tất cả mức độ --</option>
                <option value="EASY">Dễ (Nhận biết)</option>
                <option value="MEDIUM">Trung bình (Thông hiểu)</option>
                <option value="HARD">Khó (Vận dụng cao)</option>
              </select>
            </div>

            <div>
              <label class="block text-[11px] font-semibold text-gray-500 mb-1">Loại câu hỏi</label>
              <select
                v-model="filterType"
                @change="fetchQuestions()"
                class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-2.5 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
              >
                <option value="">-- Tất cả loại --</option>
                <option value="MULTIPLE_CHOICE">Trắc nghiệm</option>
                <option value="FILL_IN">Điền khuyết / Điền số</option>
                <option value="ESSAY">Tự luận / Lời giải</option>
              </select>
            </div>

            <div>
              <label class="block text-[11px] font-semibold text-gray-500 mb-1">Tìm nội dung</label>
              <SearchBar
                v-model="questionKeyword"
                placeholder="Tìm nội dung câu hỏi..."
              />
            </div>
          </div>
        </div>

        <!-- Selection toolbar & Stats -->
        <div class="flex items-center justify-between text-xs text-gray-500 px-1">
          <div class="flex items-center gap-2">
            <label class="flex items-center gap-1.5 font-semibold text-orange-950 cursor-pointer">
              <input
                type="checkbox"
                :checked="isAllSelected"
                @change="toggleSelectAll"
                class="rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
              />
              Chọn tất cả ({{ filteredQuestions.length }})
            </label>
            <span v-if="selectedQuestionIds.length > 0" class="text-orange-600 font-bold">
              • Đã chọn {{ selectedQuestionIds.length }} câu
            </span>
          </div>

          <div class="flex items-center gap-2">
            <button
              v-if="selectedQuestionIds.length > 0"
              type="button"
              @click="exportSelectedQuestions"
              class="inline-flex items-center gap-1 text-orange-700 hover:text-orange-900 font-semibold"
            >
              <ArrowDownTrayIcon class="w-3.5 h-3.5" /> Trích xuất câu hỏi
            </button>
            <span>Hiển thị {{ filteredQuestions.length }} câu hỏi</span>
          </div>
        </div>

        <!-- Questions List Container -->
        <div v-if="loadingQuestions" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
          <ArrowPathIcon class="w-7 h-7 text-orange-400 animate-spin mx-auto mb-2" />
          <p class="text-xs text-gray-500">Đang tải danh sách câu hỏi...</p>
        </div>

        <div v-else-if="!selectedBank" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
          <EmptyState
            :icon="FolderIcon"
            message="Chưa chọn ngân hàng câu hỏi"
            sub="Vui lòng nhấn chọn một ngân hàng ở danh sách bên trái để xem và quản lý câu hỏi."
          />
        </div>

        <div v-else-if="filteredQuestions.length === 0" class="bg-white rounded-2xl border border-orange-100 p-12 text-center shadow-sm">
          <EmptyState
            :icon="DocumentTextIcon"
            message="Không tìm thấy câu hỏi nào"
            sub="Hãy thêm câu hỏi mới vào ngân hàng hoặc thay đổi bộ lọc tìm kiếm."
          />
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="(q, idx) in filteredQuestions"
            :key="q.id"
            class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-all flex items-start gap-3.5"
          >
            <input
              type="checkbox"
              :value="q.id"
              v-model="selectedQuestionIds"
              class="mt-1 rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
            />

            <div class="flex-1 min-w-0">
              <!-- Question meta tags -->
              <div class="flex flex-wrap items-center gap-2 mb-2">
                <span class="text-[11px] font-mono font-bold text-orange-800 bg-orange-50 border border-orange-200 px-2 py-0.5 rounded">
                  #{{ idx + 1 }} (ID: {{ q.id }})
                </span>

                <span
                  :class="[
                    'text-[10px] font-bold px-2 py-0.5 rounded-full',
                    q.difficulty === 'EASY' ? 'bg-emerald-100 text-emerald-800' :
                    q.difficulty === 'MEDIUM' ? 'bg-blue-100 text-blue-800' :
                    'bg-amber-100 text-amber-800'
                  ]"
                >
                  {{ q.difficulty === 'EASY' ? 'Dễ' : q.difficulty === 'MEDIUM' ? 'Trung bình' : 'Khó' }}
                </span>

                <span class="text-[10px] font-semibold text-gray-700 bg-gray-100 px-2 py-0.5 rounded-full">
                  {{
                    q.type === 'MULTIPLE_CHOICE' ? 'Trắc nghiệm' :
                    q.type === 'FILL_IN' ? 'Điền khuyết' : 'Tự luận'
                  }}
                </span>
              </div>

              <!-- Question content -->
              <p class="text-xs sm:text-sm font-semibold text-gray-900 leading-relaxed whitespace-pre-line">
                {{ q.content }}
              </p>

              <!-- Optional Image -->
              <div v-if="q.imageUrl" class="mt-2.5 max-w-sm rounded-xl overflow-hidden border border-orange-100">
                <img :src="q.imageUrl" alt="Ảnh minh họa" class="w-full object-contain max-h-48 bg-orange-50/50" />
              </div>

              <!-- Answer / Options display -->
              <div class="mt-3 pt-2.5 border-t border-orange-50">
                <div class="flex items-center justify-between text-xs">
                  <button
                    type="button"
                    @click="toggleAnswerView(q.id)"
                    class="text-orange-600 hover:text-orange-800 font-semibold cursor-pointer inline-flex items-center gap-1"
                  >
                    <EyeIcon v-if="!showAnswerMap[q.id]" class="w-3.5 h-3.5" />
                    <EyeSlashIcon v-else class="w-3.5 h-3.5" />
                    {{ showAnswerMap[q.id] ? 'Ẩn đáp án' : 'Xem đáp án & giải thích' }}
                  </button>

                  <div class="flex items-center gap-1">
                    <button
                      type="button"
                      @click="openQuestionModal(q)"
                      class="px-2 py-1 text-xs font-semibold text-orange-700 hover:bg-orange-50 rounded-lg transition-colors cursor-pointer"
                    >
                      Sửa
                    </button>
                    <button
                      type="button"
                      @click="confirmDeleteQuestion(q)"
                      class="px-2 py-1 text-xs font-semibold text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                    >
                      Xóa
                    </button>
                  </div>
                </div>

                <!-- Answer reveal box -->
                <div
                  v-if="showAnswerMap[q.id]"
                  class="mt-2.5 p-3 rounded-xl bg-orange-50/80 border border-orange-200 text-xs text-orange-950 space-y-1"
                >
                  <p class="font-bold text-orange-900 flex items-center gap-1">
                    🎯 Đáp án chuẩn:
                  </p>
                  <p class="text-gray-800 whitespace-pre-line font-medium leading-relaxed bg-white/70 p-2 rounded-lg border border-orange-100">
                    {{ q.answer }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Modal 1: Tạo / Sửa Ngân hàng câu hỏi ────────────────────────────── -->
    <div
      v-if="showBankModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-md w-full p-6 shadow-xl border border-orange-100 space-y-4">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <h3 class="text-base font-bold text-orange-950">
            {{ editingBank ? 'Chỉnh sửa ngân hàng câu hỏi' : 'Tạo ngân hàng câu hỏi mới' }}
          </h3>
          <button @click="showBankModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <form @submit.prevent="saveBank" class="space-y-3.5 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Khóa học <span class="text-red-500">*</span>
            </label>
            <select
              v-model="bankForm.courseId"
              required
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium"
            >
              <option :value="null" disabled>-- Chọn khóa học --</option>
              <option v-for="c in courses" :key="c.id" :value="c.id">
                {{ c.name }}
              </option>
            </select>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Tên ngân hàng câu hỏi <span class="text-red-500">*</span>
            </label>
            <input
              type="text"
              v-model="bankForm.name"
              required
              placeholder="VD: Chuyên đề Số học & Dãy số logic Lớp 3"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Mô tả / Ghi chú</label>
            <textarea
              v-model="bankForm.description"
              rows="3"
              placeholder="Ghi chú về phạm vi kiến thức, mục tiêu rèn luyện..."
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            ></textarea>
          </div>

          <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showBankModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="savingBank"
              class="px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="savingBank" class="w-3.5 h-3.5 animate-spin" />
              <span>{{ editingBank ? 'Lưu cập nhật' : 'Tạo ngân hàng' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ─── Modal 2: Thêm / Sửa Câu hỏi ────────────────────────────────────── -->
    <div
      v-if="showQuestionModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-xl w-full p-6 shadow-xl border border-orange-100 space-y-4 max-h-[90vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">
              {{ editingQuestion ? 'Chỉnh sửa câu hỏi' : 'Thêm câu hỏi mới vào ngân hàng' }}
            </h3>
            <p class="text-[11px] text-gray-500 mt-0.5">
              Ngân hàng: <span class="text-orange-700 font-semibold">{{ selectedBank?.name }}</span>
            </p>
          </div>
          <button @click="showQuestionModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <form @submit.prevent="saveQuestion" class="space-y-3.5 text-xs">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">
                Loại câu hỏi <span class="text-red-500">*</span>
              </label>
              <select
                v-model="questionForm.type"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium"
              >
                <option value="MULTIPLE_CHOICE">Trắc nghiệm</option>
                <option value="FILL_IN">Điền khuyết / Điền số</option>
                <option value="ESSAY">Tự luận / Trình bày giải</option>
              </select>
            </div>

            <div>
              <label class="block font-semibold text-gray-700 mb-1">
                Mức độ khó <span class="text-red-500">*</span>
              </label>
              <select
                v-model="questionForm.difficulty"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium"
              >
                <option value="EASY">Dễ (Nhận biết)</option>
                <option value="MEDIUM">Trung bình (Thông hiểu)</option>
                <option value="HARD">Khó (Vận dụng cao)</option>
              </select>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Nội dung câu hỏi <span class="text-red-500">*</span>
            </label>
            <textarea
              v-model="questionForm.content"
              required
              rows="3"
              placeholder="Nhập nội dung đề bài, các điều kiện hoặc yêu cầu..."
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            ></textarea>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Link hình ảnh minh họa (tùy chọn)
            </label>
            <input
              type="url"
              v-model="questionForm.imageUrl"
              placeholder="https://example.com/image.png"
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            />
          </div>

          <!-- Answer & Explanation -->
          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Đáp án & Lời giải chuẩn <span class="text-red-500">*</span>
            </label>
            <textarea
              v-model="questionForm.answer"
              required
              rows="3"
              placeholder="Nhập đáp án đúng (VD: A. 81 cm2 hoặc Số 42) kèm giải thích chi tiết..."
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            ></textarea>
            <p class="text-[10px] text-gray-400 mt-1">
              Đối với câu trắc nghiệm, có thể ghi rõ các phương án A, B, C, D và chỉ ra đáp án đúng.
            </p>
          </div>

          <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
            <button
              type="button"
              @click="showQuestionModal = false"
              class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100 cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="savingQuestion"
              class="px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="savingQuestion" class="w-3.5 h-3.5 animate-spin" />
              <span>{{ editingQuestion ? 'Lưu câu hỏi' : 'Thêm vào ngân hàng' }}</span>
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
  PlusIcon,
  FolderIcon,
  FolderPlusIcon,
  PencilSquareIcon,
  TrashIcon,
  DocumentTextIcon,
  ArrowDownTrayIcon,
  ArrowPathIcon,
  EyeIcon,
  EyeSlashIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import questionBankService from '@/services/questionBank.service'
import courseService from '@/services/course.service'

// ─── State ─────────────────────────────────────────────────────────────────
const courses = ref([])
const questionBanks = ref([])
const questions = ref([])

const selectedBank = ref(null)
const filterCourseId = ref(null)
const bankKeyword = ref('')

const filterDifficulty = ref('')
const filterType = ref('')
const questionKeyword = ref('')
const selectedQuestionIds = ref([])
const showAnswerMap = reactive({})

const loadingBanks = ref(false)
const loadingQuestions = ref(false)
const savingBank = ref(false)
const savingQuestion = ref(false)

const actionMessage = reactive({
  text: '',
  isError: false,
})

function notify(text, isError = false) {
  actionMessage.text = text
  actionMessage.isError = isError
  setTimeout(() => {
    if (actionMessage.text === text) {
      actionMessage.text = ''
    }
  }, 4000)
}

// ─── Modal State: Bank ─────────────────────────────────────────────────────
const showBankModal = ref(false)
const editingBank = ref(null)
const bankForm = reactive({
  courseId: null,
  name: '',
  description: '',
})

// ─── Modal State: Question ─────────────────────────────────────────────────
const showQuestionModal = ref(false)
const editingQuestion = ref(null)
const questionForm = reactive({
  bankId: 0,
  type: 'MULTIPLE_CHOICE',
  difficulty: 'EASY',
  content: '',
  imageUrl: '',
  answer: '',
})

// ─── Computed ──────────────────────────────────────────────────────────────
const filteredQuestions = computed(() => {
  return questions.value.filter(q => {
    const matchSearch =
      !questionKeyword.value.trim() ||
      q.content.toLowerCase().includes(questionKeyword.value.toLowerCase()) ||
      q.answer.toLowerCase().includes(questionKeyword.value.toLowerCase())
    return matchSearch
  })
})

const isAllSelected = computed(() => {
  return (
    filteredQuestions.value.length > 0 &&
    filteredQuestions.value.every(q => selectedQuestionIds.value.includes(q.id))
  )
})

function toggleSelectAll() {
  if (isAllSelected.value) {
    selectedQuestionIds.value = []
  } else {
    selectedQuestionIds.value = filteredQuestions.value.map(q => q.id)
  }
}

function toggleAnswerView(id) {
  showAnswerMap[id] = !showAnswerMap[id]
}

// ─── API: Load Courses & Banks ─────────────────────────────────────────────
let debounceTimer = null
function debouncedFetchBanks() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    fetchBanks()
  }, 300)
}

async function loadCourses() {
  try {
    const res = await courseService.getAll()
    courses.value = Array.isArray(res) ? res : (res?.items || [])
  } catch (err) {
    console.error('Lỗi tải danh sách khóa học:', err)
  }
}

async function fetchBanks() {
  loadingBanks.value = true
  try {
    const res = await questionBankService.getAll(
      filterCourseId.value || null,
      bankKeyword.value.trim() || null
    )
    questionBanks.value = Array.isArray(res) ? res : []

    // If current selected bank is still in list, keep it; otherwise select first
    if (selectedBank.value) {
      const found = questionBanks.value.find(b => b.id === selectedBank.value.id)
      if (found) {
        selectedBank.value = found
      } else if (questionBanks.value.length > 0) {
        selectBank(questionBanks.value[0])
      } else {
        selectedBank.value = null
        questions.value = []
      }
    } else if (questionBanks.value.length > 0) {
      selectBank(questionBanks.value[0])
    } else {
      questions.value = []
    }
  } catch (err) {
    console.error('Lỗi tải ngân hàng câu hỏi:', err)
    notify('Không thể tải danh sách ngân hàng câu hỏi.', true)
  } finally {
    loadingBanks.value = false
  }
}

// ─── API: Select Bank & Load Questions ─────────────────────────────────────
async function selectBank(bank) {
  selectedBank.value = bank
  selectedQuestionIds.value = []
  await fetchQuestions()
}

async function fetchQuestions() {
  if (!selectedBank.value) return
  loadingQuestions.value = true
  try {
    const res = await questionBankService.getQuestions(
      selectedBank.value.id,
      filterDifficulty.value || null,
      filterType.value || null
    )
    questions.value = Array.isArray(res) ? res : []
  } catch (err) {
    console.error('Lỗi tải danh sách câu hỏi:', err)
    notify('Không thể tải danh sách câu hỏi của ngân hàng.', true)
  } finally {
    loadingQuestions.value = false
  }
}

// ─── Bank Actions (Create / Edit / Delete) ──────────────────────────────────
function openBankModal(bank = null) {
  editingBank.value = bank
  if (bank) {
    bankForm.courseId = bank.courseId
    bankForm.name = bank.name
    bankForm.description = bank.description || ''
  } else {
    bankForm.courseId = filterCourseId.value || (courses.value[0]?.id || null)
    bankForm.name = ''
    bankForm.description = ''
  }
  showBankModal.value = true
}

async function saveBank() {
  if (!bankForm.courseId || !bankForm.name.trim()) {
    notify('Vui lòng chọn khóa học và nhập tên ngân hàng!', true)
    return
  }

  savingBank.value = true
  try {
    if (editingBank.value) {
      await questionBankService.update(editingBank.value.id, {
        courseId: bankForm.courseId,
        name: bankForm.name.trim(),
        description: bankForm.description?.trim() || null,
      })
      notify('Đã cập nhật ngân hàng câu hỏi thành công!')
    } else {
      const created = await questionBankService.create({
        courseId: bankForm.courseId,
        name: bankForm.name.trim(),
        description: bankForm.description?.trim() || null,
      })
      notify('Đã tạo ngân hàng câu hỏi mới thành công!')
      selectedBank.value = created
    }
    showBankModal.value = false
    await fetchBanks()
  } catch (err) {
    console.error('Lỗi lưu ngân hàng câu hỏi:', err)
    notify('Có lỗi xảy ra khi lưu ngân hàng câu hỏi.', true)
  } finally {
    savingBank.value = false
  }
}

async function confirmDeleteBank(bank) {
  const confirmed = confirm(
    `Bạn có chắc chắn muốn xóa ngân hàng "${bank.name}"? Toàn bộ câu hỏi bên trong cũng sẽ bị xóa.`
  )
  if (!confirmed) return

  try {
    await questionBankService.delete(bank.id)
    notify(`Đã xóa ngân hàng "${bank.name}".`)
    if (selectedBank.value?.id === bank.id) {
      selectedBank.value = null
    }
    await fetchBanks()
  } catch (err) {
    console.error('Lỗi xóa ngân hàng:', err)
    notify('Không thể xóa ngân hàng câu hỏi.', true)
  }
}

// ─── Question Actions (Create / Edit / Delete) ──────────────────────────────
function openQuestionModal(q = null) {
  if (!selectedBank.value) {
    notify('Vui lòng chọn một ngân hàng câu hỏi trước!', true)
    return
  }

  editingQuestion.value = q
  if (q) {
    questionForm.bankId = q.bankId || selectedBank.value.id
    questionForm.type = q.type || 'MULTIPLE_CHOICE'
    questionForm.difficulty = q.difficulty || 'EASY'
    questionForm.content = q.content || ''
    questionForm.imageUrl = q.imageUrl || ''
    questionForm.answer = q.answer || ''
  } else {
    questionForm.bankId = selectedBank.value.id
    questionForm.type = 'MULTIPLE_CHOICE'
    questionForm.difficulty = 'EASY'
    questionForm.content = ''
    questionForm.imageUrl = ''
    questionForm.answer = ''
  }
  showQuestionModal.value = true
}

async function saveQuestion() {
  if (!questionForm.content.trim() || !questionForm.answer.trim()) {
    notify('Vui lòng nhập đầy đủ nội dung câu hỏi và đáp án!', true)
    return
  }

  savingQuestion.value = true
  try {
    const payload = {
      bankId: selectedBank.value.id,
      type: questionForm.type,
      difficulty: questionForm.difficulty,
      content: questionForm.content.trim(),
      imageUrl: questionForm.imageUrl?.trim() || null,
      answer: questionForm.answer.trim(),
    }

    if (editingQuestion.value) {
      await questionBankService.updateQuestion(editingQuestion.value.id, payload)
      notify('Đã cập nhật câu hỏi thành công!')
    } else {
      await questionBankService.createQuestion(payload)
      notify('Đã thêm câu hỏi mới vào ngân hàng!')
    }

    showQuestionModal.value = false
    await fetchQuestions()
    await fetchBanks() // refresh total count
  } catch (err) {
    console.error('Lỗi lưu câu hỏi:', err)
    notify('Không thể lưu câu hỏi.', true)
  } finally {
    savingQuestion.value = false
  }
}

async function confirmDeleteQuestion(q) {
  const confirmed = confirm('Bạn có chắc chắn muốn xóa câu hỏi này khỏi ngân hàng?')
  if (!confirmed) return

  try {
    await questionBankService.deleteQuestion(q.id)
    notify('Đã xóa câu hỏi khỏi ngân hàng.')
    await fetchQuestions()
    await fetchBanks() // refresh total count
  } catch (err) {
    console.error('Lỗi xóa câu hỏi:', err)
    notify('Không thể xóa câu hỏi.', true)
  }
}

function exportSelectedQuestions() {
  alert(`Đã trích xuất ${selectedQuestionIds.value.length} câu hỏi đã chọn.`)
}

// ─── Lifecycle ─────────────────────────────────────────────────────────────
onMounted(async () => {
  await loadCourses()
  await fetchBanks()
})
</script>
