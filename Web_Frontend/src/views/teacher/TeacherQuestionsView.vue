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
      <button @click="actionMessage.text = ''" class="text-gray-400 hover:text-gray-600 text-sm font-bold cursor-pointer">✕</button>
    </div>

    <!-- ─── Quick Info Banner: Điểm theo mốc & Cách tính điểm ──────────────── -->
    <div class="p-3.5 bg-gradient-to-r from-orange-50 via-amber-50 to-orange-50 rounded-2xl border border-orange-200 flex flex-wrap items-center justify-between gap-3 text-xs shadow-2xs">
      <div class="flex items-center gap-2.5 text-orange-950 font-medium">
        <span class="p-1 rounded-lg bg-orange-200/70 text-orange-800 text-sm">🎯</span>
        <span>
          <strong>Ràng buộc điểm theo mức độ:</strong>
          <span class="inline-flex items-center gap-1 mx-1 px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 font-bold text-[11px]">Dễ: 1.0đ</span>
          <span class="inline-flex items-center gap-1 mx-1 px-2 py-0.5 rounded-md bg-blue-100 text-blue-800 font-bold text-[11px]">Trung bình: 1.5đ</span>
          <span class="inline-flex items-center gap-1 mx-1 px-2 py-0.5 rounded-md bg-amber-100 text-amber-900 font-bold text-[11px]">Khó: 2.0đ</span>
        </span>
      </div>
      <div class="text-[11px] font-semibold text-orange-900 bg-white/90 px-3 py-1.5 rounded-xl border border-orange-200 shadow-2xs">
        Cách tính điểm bài thi: <span class="font-bold text-orange-600">(Tổng điểm đạt / Tổng số điểm) × 100</span>
      </div>
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
              <label class="block text-[11px] font-semibold text-gray-500 mb-1">Mức độ khó & Điểm</label>
              <select
                v-model="filterDifficulty"
                @change="fetchQuestions()"
                class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-2.5 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
              >
                <option value="">-- Tất cả mức độ --</option>
                <option value="EASY">Dễ (1.0 điểm)</option>
                <option value="MEDIUM">Trung bình (1.5 điểm)</option>
                <option value="HARD">Khó (2.0 điểm)</option>
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
                <option value="MULTIPLE_CHOICE">Trắc nghiệm (4 phương án)</option>
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
              class="inline-flex items-center gap-1 text-orange-700 hover:text-orange-900 font-semibold cursor-pointer"
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

        <div v-else class="space-y-3.5">
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

                <!-- Difficulty & Score Milestone Tag -->
                <span
                  :class="[
                    'text-[10px] font-bold px-2 py-0.5 rounded-full inline-flex items-center gap-1',
                    q.difficulty === 'EASY' ? 'bg-emerald-100 text-emerald-800' :
                    q.difficulty === 'MEDIUM' ? 'bg-blue-100 text-blue-800' :
                    'bg-amber-100 text-amber-800'
                  ]"
                >
                  <span>{{ q.difficulty === 'EASY' ? 'Dễ' : q.difficulty === 'MEDIUM' ? 'Trung bình' : 'Khó' }}</span>
                  <span class="font-extrabold">• {{ getDifficultyPoint(q.difficulty) }}đ</span>
                </span>

                <span class="text-[10px] font-semibold text-gray-700 bg-gray-100 px-2 py-0.5 rounded-full">
                  {{
                    q.type === 'MULTIPLE_CHOICE' ? 'Trắc nghiệm' :
                    q.type === 'FILL_IN' ? 'Điền khuyết' : 'Tự luận'
                  }}
                </span>
              </div>

              <!-- Question Content & Options -->
              <div class="space-y-2">
                <!-- If Multiple Choice: display question text + 4 distinct options -->
                <template v-if="q.type === 'MULTIPLE_CHOICE'">
                  <p class="text-xs sm:text-sm font-semibold text-gray-900 leading-relaxed whitespace-pre-line">
                    {{ getParsedQuestion(q).questionText }}
                  </p>

                  <!-- 4 Options Grid -->
                  <div
                    v-if="hasValidMCOptions(getParsedQuestion(q))"
                    class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2 pt-1"
                  >
                    <div
                      v-for="optKey in ['A', 'B', 'C', 'D']"
                      :key="optKey"
                      :class="[
                        'p-2.5 rounded-xl border text-xs flex items-start gap-2 transition-all',
                        showAnswerMap[q.id] && isCorrectOption(getParsedQuestion(q), optKey)
                          ? 'bg-emerald-50 border-emerald-300 text-emerald-950 ring-1 ring-emerald-300 font-semibold'
                          : 'bg-orange-50/30 border-orange-100 text-gray-800'
                      ]"
                    >
                      <span
                        :class="[
                          'w-5 h-5 rounded-md flex items-center justify-center font-bold text-[11px] flex-shrink-0',
                          showAnswerMap[q.id] && isCorrectOption(getParsedQuestion(q), optKey)
                            ? 'bg-emerald-500 text-white'
                            : 'bg-orange-200/80 text-orange-950'
                        ]"
                      >
                        {{ optKey }}
                      </span>
                      <span class="flex-1 leading-snug">
                        {{ getOptionText(getParsedQuestion(q), optKey) }}
                      </span>
                      <span
                        v-if="showAnswerMap[q.id] && isCorrectOption(getParsedQuestion(q), optKey)"
                        class="text-emerald-600 text-xs font-bold"
                      >
                        ✓ Đúng
                      </span>
                    </div>
                  </div>
                </template>

                <!-- If Fill-in or Essay: regular text -->
                <template v-else>
                  <p class="text-xs sm:text-sm font-semibold text-gray-900 leading-relaxed whitespace-pre-line">
                    {{ q.content }}
                  </p>
                </template>
              </div>

              <!-- Media Display: YouTube Video or Image -->
              <div v-if="q.imageUrl" class="mt-3">
                <!-- YouTube Video Embed -->
                <div
                  v-if="isYouTubeUrl(q.imageUrl)"
                  class="max-w-md rounded-2xl overflow-hidden border border-orange-200 shadow-xs bg-black"
                >
                  <iframe
                    :src="getYouTubeEmbedUrl(q.imageUrl)"
                    class="w-full aspect-video"
                    title="Video hướng dẫn câu hỏi"
                    frameborder="0"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen
                  ></iframe>
                </div>

                <!-- Image Display -->
                <div
                  v-else
                  class="max-w-sm rounded-xl overflow-hidden border border-orange-100 bg-white shadow-2xs"
                >
                  <img
                    :src="q.imageUrl"
                    alt="Hình ảnh minh họa"
                    class="w-full object-contain max-h-48 bg-orange-50/20"
                    loading="lazy"
                  />
                </div>
              </div>

              <!-- Answer / Options display toggle -->
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
                  class="mt-2.5 p-3 rounded-xl bg-orange-50/80 border border-orange-200 text-xs text-orange-950 space-y-1.5"
                >
                  <p class="font-bold text-orange-900 flex items-center gap-1">
                    🎯 Đáp án chuẩn & Điểm:
                    <span class="text-[11px] font-normal text-gray-600">
                      (Độ khó: {{ q.difficulty === 'EASY' ? 'Dễ' : q.difficulty === 'MEDIUM' ? 'Trung bình' : 'Khó' }} • Điểm số: <strong>{{ getDifficultyPoint(q.difficulty) }}đ</strong>)
                    </span>
                  </p>
                  <p class="text-gray-800 whitespace-pre-line font-medium leading-relaxed bg-white/80 p-2.5 rounded-lg border border-orange-100">
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
          <button @click="showBankModal = false" class="text-gray-400 hover:text-gray-600 text-lg cursor-pointer">✕</button>
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

    <!-- ─── Modal 2: Thêm / Sửa Câu hỏi (Nâng cấp A, B, C, D & Media) ────────── -->
    <div
      v-if="showQuestionModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-2xl w-full p-6 shadow-xl border border-orange-100 space-y-4 max-h-[92vh] overflow-y-auto">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <div>
            <h3 class="text-base font-bold text-orange-950">
              {{ editingQuestion ? 'Chỉnh sửa câu hỏi' : 'Thêm câu hỏi mới vào ngân hàng' }}
            </h3>
            <p class="text-[11px] text-gray-500 mt-0.5">
              Ngân hàng: <span class="text-orange-700 font-semibold">{{ selectedBank?.name }}</span>
            </p>
          </div>
          <button @click="showQuestionModal = false" class="text-gray-400 hover:text-gray-600 text-lg cursor-pointer">✕</button>
        </div>

        <form @submit.prevent="saveQuestion" class="space-y-4 text-xs">
          <!-- 1. Loại câu hỏi & Mức độ khó -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <div>
              <label class="block font-semibold text-gray-700 mb-1">
                Loại câu hỏi <span class="text-red-500">*</span>
              </label>
              <select
                v-model="questionForm.type"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium bg-white"
              >
                <option value="MULTIPLE_CHOICE">Trắc nghiệm (Hiện 4 ô A, B, C, D)</option>
                <option value="FILL_IN">Điền khuyết / Điền số</option>
                <option value="ESSAY">Tự luận / Trình bày giải</option>
              </select>
            </div>

            <div>
              <label class="block font-semibold text-gray-700 mb-1">
                Mức độ khó & Điểm quy chuẩn <span class="text-red-500">*</span>
              </label>
              <select
                v-model="questionForm.difficulty"
                required
                class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 font-medium bg-white"
              >
                <option value="EASY">Dễ (Nhận biết) — Quy định 1.0 điểm</option>
                <option value="MEDIUM">Trung bình (Thông hiểu) — Quy định 1.5 điểm</option>
                <option value="HARD">Khó (Vận dụng cao) — Quy định 2.0 điểm</option>
              </select>
              <div class="mt-1 flex items-center justify-between text-[11px] text-orange-800 font-semibold">
                <span>Điểm câu này:</span>
                <span class="px-2 py-0.5 rounded-md bg-orange-100 text-orange-950 font-bold">
                  {{ DIFFICULTY_POINTS[questionForm.difficulty] }} điểm
                </span>
              </div>
            </div>
          </div>

          <!-- 2. Nội dung câu hỏi -->
          <div>
            <label class="block font-semibold text-gray-700 mb-1">
              Nội dung câu hỏi / Đề bài <span class="text-red-500">*</span>
            </label>
            <textarea
              v-model="questionForm.content"
              required
              rows="3"
              placeholder="Nhập nội dung đề bài, các điều kiện hoặc yêu cầu của câu hỏi..."
              class="w-full rounded-xl border border-orange-200 p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
            ></textarea>
          </div>

          <!-- 3. Tùy chọn Media: Upload ảnh hoặc Link YouTube -->
          <div class="p-3.5 rounded-2xl bg-orange-50/50 border border-orange-200 space-y-2.5">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <label class="font-bold text-orange-950 flex items-center gap-1.5">
                <PhotoIcon class="w-4 h-4 text-orange-600" />
                Tài liệu minh họa (Ảnh / Video YouTube)
              </label>

              <!-- Media Type Selector -->
              <div class="flex items-center gap-1 bg-white p-1 rounded-xl border border-orange-200 text-[11px] font-semibold">
                <button
                  type="button"
                  @click="setMediaType('upload')"
                  :class="[
                    'px-2.5 py-1 rounded-lg transition-colors cursor-pointer',
                    mediaType === 'upload' ? 'bg-orange-500 text-white font-bold' : 'text-gray-600 hover:text-orange-900'
                  ]"
                >
                  Upload ảnh
                </button>
                <button
                  type="button"
                  @click="setMediaType('youtube')"
                  :class="[
                    'px-2.5 py-1 rounded-lg transition-colors cursor-pointer',
                    mediaType === 'youtube' ? 'bg-orange-500 text-white font-bold' : 'text-gray-600 hover:text-orange-900'
                  ]"
                >
                  Link YouTube
                </button>
                <button
                  type="button"
                  @click="setMediaType('url')"
                  :class="[
                    'px-2.5 py-1 rounded-lg transition-colors cursor-pointer',
                    mediaType === 'url' ? 'bg-orange-500 text-white font-bold' : 'text-gray-600 hover:text-orange-900'
                  ]"
                >
                  Link ảnh web
                </button>
                <button
                  type="button"
                  @click="clearMedia()"
                  class="px-2 py-1 text-gray-400 hover:text-rose-600 rounded-lg cursor-pointer"
                  title="Không dùng media"
                >
                  ✕
                </button>
              </div>
            </div>

            <!-- Tab Upload Ảnh -->
            <div v-if="mediaType === 'upload'" class="space-y-2">
              <div class="flex items-center gap-2">
                <label class="px-3.5 py-2 rounded-xl bg-white border border-orange-300 text-orange-800 hover:bg-orange-50 font-semibold cursor-pointer shadow-2xs inline-flex items-center gap-1.5">
                  <ArrowDownTrayIcon class="w-3.5 h-3.5 text-orange-600" />
                  <span>Chọn tệp ảnh từ máy</span>
                  <input
                    type="file"
                    accept="image/*"
                    class="hidden"
                    @change="handleImageFileUpload"
                  />
                </label>
                <span v-if="uploadFileName" class="text-[11px] text-gray-600 font-medium truncate max-w-xs">
                  {{ uploadFileName }}
                </span>
                <span v-else class="text-[11px] text-gray-400">
                  Hỗ trợ PNG, JPG, JPEG, WebP, GIF (tối đa 10MB)
                </span>
              </div>
            </div>

            <!-- Tab Link YouTube -->
            <div v-else-if="mediaType === 'youtube'" class="space-y-1.5">
              <input
                type="url"
                v-model="questionForm.imageUrl"
                placeholder="Dán link video YouTube (VD: https://www.youtube.com/watch?v=... hoặc https://youtu.be/...)"
                class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
              <p class="text-[10px] text-gray-500">
                Video YouTube sẽ được nhúng trực tiếp và phát trên giao diện bài thi của học sinh.
              </p>
            </div>

            <!-- Tab Link Ảnh Ngoài -->
            <div v-else-if="mediaType === 'url'" class="space-y-1.5">
              <input
                type="url"
                v-model="questionForm.imageUrl"
                placeholder="https://example.com/image.png"
                class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
              />
            </div>

            <!-- Media Live Preview -->
            <div v-if="questionForm.imageUrl" class="pt-2 border-t border-orange-100 flex items-start gap-3">
              <!-- If YouTube -->
              <div v-if="isYouTubeUrl(questionForm.imageUrl)" class="w-full max-w-sm rounded-xl overflow-hidden border border-orange-300 bg-black shadow-xs">
                <iframe
                  :src="getYouTubeEmbedUrl(questionForm.imageUrl)"
                  class="w-full aspect-video"
                  title="YouTube Preview"
                  frameborder="0"
                  allowfullscreen
                ></iframe>
              </div>

              <!-- If Image -->
              <div v-else class="relative max-w-xs rounded-xl overflow-hidden border border-orange-200 bg-white p-1">
                <img :src="questionForm.imageUrl" alt="Xem trước ảnh" class="w-full max-h-36 object-contain" />
              </div>

              <button
                type="button"
                @click="clearMedia()"
                class="px-2.5 py-1 text-xs text-rose-600 hover:bg-rose-50 rounded-lg font-semibold cursor-pointer"
              >
                Gỡ bỏ media
              </button>
            </div>
          </div>

          <!-- 4. Nhập Đáp án: Nếu Trắc nghiệm hiện 4 ô A, B, C, D; Nếu Điền khuyết/Tự luận hiện input bình thường -->

          <!-- ─── CASE 1: TRẮC NGHIỆM (4 ô A, B, C, D) ─── -->
          <div
            v-if="questionForm.type === 'MULTIPLE_CHOICE'"
            class="p-4 rounded-2xl bg-orange-50/70 border border-orange-200 space-y-3"
          >
            <div class="flex items-center justify-between">
              <label class="font-bold text-orange-950 text-xs flex items-center gap-1.5">
                <span>🔘 4 Phương án Trắc nghiệm (Chọn chấm tròn để chỉ định đáp án đúng):</span>
              </label>
              <span class="text-[11px] font-bold text-emerald-700 bg-emerald-100/80 px-2 py-0.5 rounded-md">
                Đáp án đúng: Phương án {{ mcCorrectOption }}
              </span>
            </div>

            <!-- 4 Option Inputs -->
            <div class="space-y-2.5">
              <div
                v-for="opt in ['A', 'B', 'C', 'D']"
                :key="opt"
                :class="[
                  'p-2 rounded-xl border flex items-center gap-2.5 transition-all',
                  mcCorrectOption === opt
                    ? 'bg-emerald-50/90 border-emerald-300 ring-1 ring-emerald-300'
                    : 'bg-white border-orange-200'
                ]"
              >
                <!-- Radio chọn đáp án đúng -->
                <label class="flex items-center gap-1.5 cursor-pointer font-bold flex-shrink-0">
                  <input
                    type="radio"
                    name="mcCorrectOption"
                    :value="opt"
                    v-model="mcCorrectOption"
                    class="w-4 h-4 text-emerald-600 focus:ring-emerald-400 cursor-pointer"
                  />
                  <span
                    :class="[
                      'w-6 h-6 rounded-lg flex items-center justify-center text-xs font-bold',
                      mcCorrectOption === opt ? 'bg-emerald-600 text-white' : 'bg-orange-100 text-orange-950'
                    ]"
                  >
                    {{ opt }}
                  </span>
                </label>

                <!-- Input nội dung phương án -->
                <input
                  type="text"
                  v-model="mcOptions[opt]"
                  required
                  :placeholder="`Nhập nội dung phương án ${opt}...`"
                  class="flex-1 bg-transparent border-0 focus:outline-none text-xs font-medium text-gray-900 placeholder:text-gray-400"
                />

                <span v-if="mcCorrectOption === opt" class="text-[11px] font-bold text-emerald-700 px-2">
                  ✓ Đáp án đúng
                </span>
              </div>
            </div>

            <!-- Lời giải / Giải thích chi tiết cho trắc nghiệm (Tùy chọn) -->
            <div class="pt-2 border-t border-orange-200/60">
              <label class="block font-semibold text-gray-700 mb-1 text-[11px]">
                Lời giải / Hướng dẫn giải chi tiết (Tùy chọn)
              </label>
              <textarea
                v-model="mcExplanation"
                rows="2"
                placeholder="Giải thích vì sao phương án này đúng, phương pháp tư duy cần áp dụng..."
                class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 text-xs"
              ></textarea>
            </div>
          </div>

          <!-- ─── CASE 2: ĐIỀN KHUYẾT (Input chuẩn bình thường) ─── -->
          <div
            v-else-if="questionForm.type === 'FILL_IN'"
            class="p-4 rounded-2xl bg-orange-50/70 border border-orange-200 space-y-3"
          >
            <div>
              <label class="block font-semibold text-gray-800 mb-1 text-xs">
                Đáp án chuẩn cần điền <span class="text-red-500">*</span>
              </label>
              <input
                type="text"
                v-model="fillInAnswer"
                required
                placeholder="Nhập từ hoặc con số chính xác học sinh cần điền (VD: 42 hoặc 15 cm)..."
                class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 text-xs font-bold text-orange-950"
              />
              <p class="text-[10px] text-gray-400 mt-1">
                Hệ thống sẽ đối chiếu đáp án học sinh nhập với đáp án chuẩn này để chấm điểm.
              </p>
            </div>

            <div>
              <label class="block font-semibold text-gray-700 mb-1 text-[11px]">
                Hướng dẫn giải / Lời giải chi tiết (Tùy chọn)
              </label>
              <textarea
                v-model="fillInExplanation"
                rows="2"
                placeholder="Các bước tính toán ra kết quả..."
                class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 text-xs"
              ></textarea>
            </div>
          </div>

          <!-- ─── CASE 3: TỰ LUẬN (Textarea thông thường) ─── -->
          <div
            v-else-if="questionForm.type === 'ESSAY'"
            class="p-4 rounded-2xl bg-orange-50/70 border border-orange-200 space-y-2"
          >
            <label class="block font-semibold text-gray-800 mb-1 text-xs">
              Đáp án mẫu & Tiêu chí chấm điểm tự luận <span class="text-red-500">*</span>
            </label>
            <textarea
              v-model="essayAnswer"
              required
              rows="4"
              placeholder="Nhập bài giải mẫu, các bước lập luận, cách phân bổ điểm theo từng ý để giáo viên chấm thi..."
              class="w-full rounded-xl border border-orange-200 bg-white p-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 text-xs"
            ></textarea>
          </div>

          <!-- Modal Action Buttons -->
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
              class="px-5 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 disabled:opacity-50 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
            >
              <ArrowPathIcon v-if="savingQuestion" class="w-3.5 h-3.5 animate-spin" />
              <span>{{ editingQuestion ? 'Lưu cập nhật câu hỏi' : 'Thêm câu hỏi vào ngân hàng' }}</span>
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
  PhotoIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import questionBankService from '@/services/questionBank.service'
import courseService from '@/services/course.service'
import {
  isYouTubeUrl,
  getYouTubeEmbedUrl,
  fileToBase64,
  DIFFICULTY_POINTS,
  getDifficultyPoint,
  parseMultipleChoice,
  buildMultipleChoiceContent,
  buildMultipleChoiceAnswer,
} from '@/utils/media.helper'

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

// Multiple Choice state
const mcOptions = reactive({
  A: '',
  B: '',
  C: '',
  D: '',
})
const mcCorrectOption = ref('A')
const mcExplanation = ref('')

// Fill-in & Essay state
const fillInAnswer = ref('')
const fillInExplanation = ref('')
const essayAnswer = ref('')

// Media State
const mediaType = ref('none') // 'upload', 'youtube', 'url', 'none'
const uploadFileName = ref('')

function setMediaType(type) {
  mediaType.value = type
}

function clearMedia() {
  mediaType.value = 'none'
  questionForm.imageUrl = ''
  uploadFileName.value = ''
}

async function handleImageFileUpload(e) {
  const file = e.target.files?.[0]
  if (!file) return
  uploadFileName.value = file.name
  try {
    const base64 = await fileToBase64(file)
    questionForm.imageUrl = base64
    mediaType.value = 'upload'
  } catch (err) {
    console.error('Lỗi đọc file ảnh:', err)
    notify('Không thể đọc file ảnh tải lên.', true)
  }
}

// ─── Helpers for Displaying Question in List ────────────────────────────────
const parsedQuestionsCache = reactive({})

function getParsedQuestion(q) {
  if (!q) return { questionText: '', optionA: '', optionB: '', optionC: '', optionD: '', correctOption: 'A' }
  const cacheKey = `${q.id}_${q.content}_${q.answer}`
  if (!parsedQuestionsCache[cacheKey]) {
    parsedQuestionsCache[cacheKey] = parseMultipleChoice(q.content, q.answer)
  }
  return parsedQuestionsCache[cacheKey]
}

function hasValidMCOptions(parsed) {
  return !!(parsed.optionA || parsed.optionB || parsed.optionC || parsed.optionD)
}

function getOptionText(parsed, key) {
  if (key === 'A') return parsed.optionA || 'Phương án A'
  if (key === 'B') return parsed.optionB || 'Phương án B'
  if (key === 'C') return parsed.optionC || 'Phương án C'
  if (key === 'D') return parsed.optionD || 'Phương án D'
  return ''
}

function isCorrectOption(parsed, key) {
  return parsed.correctOption === key
}

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
  uploadFileName.value = ''

  if (q) {
    questionForm.bankId = q.bankId || selectedBank.value.id
    questionForm.type = q.type || 'MULTIPLE_CHOICE'
    questionForm.difficulty = q.difficulty || 'EASY'
    questionForm.imageUrl = q.imageUrl || ''

    // Setup media type
    if (q.imageUrl) {
      if (isYouTubeUrl(q.imageUrl)) {
        mediaType.value = 'youtube'
      } else if (q.imageUrl.startsWith('data:image/')) {
        mediaType.value = 'upload'
      } else {
        mediaType.value = 'url'
      }
    } else {
      mediaType.value = 'none'
    }

    // Populate type-specific answers
    if (questionForm.type === 'MULTIPLE_CHOICE') {
      const parsed = parseMultipleChoice(q.content, q.answer)
      questionForm.content = parsed.questionText || q.content
      mcOptions.A = parsed.optionA
      mcOptions.B = parsed.optionB
      mcOptions.C = parsed.optionC
      mcOptions.D = parsed.optionD
      mcCorrectOption.value = parsed.correctOption || 'A'
      mcExplanation.value = parsed.explanation || ''
    } else if (questionForm.type === 'FILL_IN') {
      questionForm.content = q.content || ''
      fillInAnswer.value = q.answer || ''
      fillInExplanation.value = ''
    } else {
      questionForm.content = q.content || ''
      essayAnswer.value = q.answer || ''
    }
  } else {
    // Create new question
    questionForm.bankId = selectedBank.value.id
    questionForm.type = 'MULTIPLE_CHOICE'
    questionForm.difficulty = 'EASY'
    questionForm.content = ''
    questionForm.imageUrl = ''
    questionForm.answer = ''

    mcOptions.A = ''
    mcOptions.B = ''
    mcOptions.C = ''
    mcOptions.D = ''
    mcCorrectOption.value = 'A'
    mcExplanation.value = ''

    fillInAnswer.value = ''
    fillInExplanation.value = ''
    essayAnswer.value = ''

    mediaType.value = 'none'
  }

  showQuestionModal.value = true
}

async function saveQuestion() {
  if (!questionForm.content.trim()) {
    notify('Vui lòng nhập nội dung đề bài / câu hỏi!', true)
    return
  }

  let finalContent = questionForm.content.trim()
  let finalAnswer = ''

  // Validate and assemble answer depending on type
  if (questionForm.type === 'MULTIPLE_CHOICE') {
    if (
      !mcOptions.A.trim() ||
      !mcOptions.B.trim() ||
      !mcOptions.C.trim() ||
      !mcOptions.D.trim()
    ) {
      notify('Vui lòng nhập đầy đủ cả 4 phương án A, B, C, D!', true)
      return
    }

    finalContent = buildMultipleChoiceContent(questionForm.content, mcOptions)
    finalAnswer = buildMultipleChoiceAnswer(mcCorrectOption.value, mcOptions, mcExplanation.value)
  } else if (questionForm.type === 'FILL_IN') {
    if (!fillInAnswer.value.trim()) {
      notify('Vui lòng nhập đáp án chuẩn cần điền!', true)
      return
    }
    finalAnswer = fillInAnswer.value.trim()
    if (fillInExplanation.value.trim()) {
      finalAnswer += `\n- Hướng dẫn giải: ${fillInExplanation.value.trim()}`
    }
  } else if (questionForm.type === 'ESSAY') {
    if (!essayAnswer.value.trim()) {
      notify('Vui lòng nhập đáp án mẫu & tiêu chí chấm điểm tự luận!', true)
      return
    }
    finalAnswer = essayAnswer.value.trim()
  }

  savingQuestion.value = true
  try {
    const payload = {
      bankId: selectedBank.value.id,
      type: questionForm.type,
      difficulty: questionForm.difficulty,
      content: finalContent,
      imageUrl: mediaType.value !== 'none' && questionForm.imageUrl?.trim() ? questionForm.imageUrl.trim() : null,
      answer: finalAnswer,
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
