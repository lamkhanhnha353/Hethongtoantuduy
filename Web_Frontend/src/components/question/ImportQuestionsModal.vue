<template>
  <div
    v-if="modelValue"
    class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-3 sm:p-5 backdrop-blur-2xs overflow-y-auto"
  >
    <div
      class="bg-white rounded-3xl max-w-4xl w-full shadow-2xl border border-orange-100 flex flex-col max-h-[92vh] overflow-hidden my-auto"
    >
      <!-- ─── Modal Header ─────────────────────────────────────────────────── -->
      <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50/70 via-white to-amber-50/60">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-2xl bg-orange-500 text-white flex items-center justify-center shadow-xs">
            <SparklesIcon class="w-5 h-5" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <h3 class="text-base sm:text-lg font-bold text-orange-950">
                Nhập câu hỏi từ File (AI Import Worker)
              </h3>
              <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-orange-100 text-orange-800 border border-orange-200">
                AI Powered
              </span>
            </div>
            <p class="text-xs text-gray-500">
              Tự động bóc tách đề thi (.docx, .pdf), trích xuất hình ảnh vẽ hình và cấu trúc hóa câu hỏi
            </p>
          </div>
        </div>

        <button
          type="button"
          @click="closeModal"
          :disabled="isProcessing"
          class="text-gray-400 hover:text-gray-700 text-xl font-bold p-1 rounded-xl hover:bg-orange-50 transition-colors disabled:opacity-30 disabled:cursor-not-allowed cursor-pointer"
        >
          ✕
        </button>
      </div>

      <!-- ─── Step Indicator Bar ───────────────────────────────────────────── -->
      <div class="px-6 py-3 bg-orange-50/30 border-b border-orange-100/80 flex items-center justify-between text-xs font-semibold">
        <div
          :class="[
            'flex items-center gap-2 transition-colors',
            currentStep === 1 ? 'text-orange-600 font-bold' : currentStep > 1 ? 'text-emerald-600' : 'text-gray-400'
          ]"
        >
          <span
            :class="[
              'w-6 h-6 rounded-full flex items-center justify-center text-xs',
              currentStep === 1 ? 'bg-orange-500 text-white' : currentStep > 1 ? 'bg-emerald-500 text-white' : 'bg-gray-200 text-gray-600'
            ]"
          >
            {{ currentStep > 1 ? '✓' : '1' }}
          </span>
          <span>1. Tải tệp & Thiết lập</span>
        </div>

        <div class="h-0.5 flex-1 mx-3 bg-orange-100 hidden sm:block"></div>

        <div
          :class="[
            'flex items-center gap-2 transition-colors',
            currentStep === 2 ? 'text-orange-600 font-bold' : currentStep > 2 ? 'text-emerald-600' : 'text-gray-400'
          ]"
        >
          <span
            :class="[
              'w-6 h-6 rounded-full flex items-center justify-center text-xs',
              currentStep === 2 ? 'bg-orange-500 text-white animate-pulse' : currentStep > 2 ? 'bg-emerald-500 text-white' : 'bg-gray-200 text-gray-600'
            ]"
          >
            {{ currentStep > 2 ? '✓' : '2' }}
          </span>
          <span>2. AI Bóc tách dữ liệu</span>
        </div>

        <div class="h-0.5 flex-1 mx-3 bg-orange-100 hidden sm:block"></div>

        <div
          :class="[
            'flex items-center gap-2 transition-colors',
            currentStep === 3 ? 'text-orange-600 font-bold' : 'text-gray-400'
          ]"
        >
          <span
            :class="[
              'w-6 h-6 rounded-full flex items-center justify-center text-xs',
              currentStep === 3 ? 'bg-orange-500 text-white' : 'bg-gray-200 text-gray-600'
            ]"
          >
            3
          </span>
          <span>3. Xem trước & Lưu</span>
        </div>
      </div>

      <!-- ─── Modal Body Content (Scrollable) ──────────────────────────────── -->
      <div class="flex-1 overflow-y-auto p-6 space-y-6">

        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <!-- BƯỚC 1: TẢI TỆP & CHỌN NGÂN HÀNG ĐÍCH                               -->
        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <div v-if="currentStep === 1" class="space-y-5">
          <!-- 1. Chọn Ngân hàng đích hoặc tạo nhanh -->
          <div class="p-4 rounded-2xl bg-orange-50/50 border border-orange-200/80 space-y-3">
            <div class="flex items-center justify-between">
              <label class="text-xs font-bold text-orange-950 flex items-center gap-1.5">
                <FolderIcon class="w-4 h-4 text-orange-600" />
                Ngân hàng câu hỏi đích <span class="text-red-500">*</span>
              </label>
              <button
                type="button"
                @click="isCreatingNewBank = !isCreatingNewBank"
                class="text-[11px] font-bold text-orange-600 hover:text-orange-700 underline cursor-pointer"
              >
                {{ isCreatingNewBank ? '← Chọn ngân hàng đã có' : '+ Tạo ngân hàng mới ngay' }}
              </button>
            </div>

            <!-- Nếu chọn ngân hàng sẵn có -->
            <div v-if="!isCreatingNewBank">
              <select
                v-model="targetBankId"
                class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300 shadow-2xs"
              >
                <option :value="null" disabled>-- Chọn ngân hàng lưu câu hỏi --</option>
                <option v-for="b in banks" :key="b.id" :value="b.id">
                  {{ b.name }} ({{ b.courseName }} - {{ b.totalQuestions || 0 }} câu)
                </option>
              </select>
            </div>

            <!-- Nếu tạo ngân hàng mới tại chỗ -->
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
              <div>
                <label class="block text-[11px] font-semibold text-gray-600 mb-1">Khóa học</label>
                <select
                  v-model="newBankCourseId"
                  class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
                >
                  <option :value="null" disabled>-- Chọn khóa học --</option>
                  <option v-for="c in courses" :key="c.id" :value="c.id">{{ c.name }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-semibold text-gray-600 mb-1">Tên ngân hàng mới</label>
                <input
                  type="text"
                  v-model="newBankName"
                  placeholder="VD: Bộ đề thi giữa kỳ Toán tư duy..."
                  class="w-full text-xs font-medium bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-300"
                />
              </div>
            </div>
          </div>

          <!-- 2. Vùng kéo thả File Upload -->
          <div class="space-y-2">
            <label class="text-xs font-bold text-gray-800 flex items-center justify-between">
              <span>Tệp tài liệu đề thi (.docx hoặc .pdf) <span class="text-red-500">*</span></span>
              <span class="text-[11px] text-gray-400 font-normal">Hỗ trợ tối đa 50MB</span>
            </label>

            <!-- Dropzone -->
            <div
              @dragover.prevent="isDragging = true"
              @dragleave.prevent="isDragging = false"
              @drop.prevent="handleFileDrop"
              :class="[
                'border-2 border-dashed rounded-3xl p-6 sm:p-8 text-center transition-all cursor-pointer flex flex-col items-center justify-center gap-3',
                isDragging
                  ? 'border-orange-500 bg-orange-100/60 scale-[1.01]'
                  : selectedFile
                  ? 'border-emerald-300 bg-emerald-50/40'
                  : 'border-orange-200 bg-orange-50/20 hover:border-orange-400 hover:bg-orange-50/50'
              ]"
              @click="triggerFileInput"
            >
              <input
                ref="fileInputRef"
                type="file"
                accept=".docx,.pdf"
                class="hidden"
                @change="handleFileInputChange"
              />

              <div
                :class="[
                  'w-14 h-14 rounded-2xl flex items-center justify-center shadow-xs transition-transform',
                  selectedFile ? 'bg-emerald-500 text-white' : 'bg-orange-100 text-orange-600'
                ]"
              >
                <DocumentArrowUpIcon v-if="!selectedFile" class="w-7 h-7" />
                <CheckCircleIcon v-else class="w-8 h-8" />
              </div>

              <div v-if="!selectedFile">
                <p class="text-sm font-bold text-gray-800">
                  Kéo thả tệp đề thi vào đây, hoặc <span class="text-orange-600 underline">duyệt từ máy</span>
                </p>
                <p class="text-xs text-gray-400 mt-1">
                  Định dạng được hỗ trợ: Microsoft Word (.docx) và Tài liệu PDF văn bản (.pdf)
                </p>
              </div>

              <!-- Thẻ hiển thị file đã chọn -->
              <div v-else class="w-full max-w-md bg-white p-3.5 rounded-2xl border border-emerald-200 shadow-2xs flex items-center justify-between gap-3" @click.stop>
                <div class="flex items-center gap-3 min-w-0">
                  <span class="text-2xl flex-shrink-0">
                    {{ selectedFile.name.endsWith('.pdf') ? '📄' : '📝' }}
                  </span>
                  <div class="min-w-0 text-left">
                    <p class="text-xs font-bold text-gray-900 truncate">{{ selectedFile.name }}</p>
                    <p class="text-[11px] text-gray-500">{{ formatFileSize(selectedFile.size) }}</p>
                  </div>
                </div>
                <button
                  type="button"
                  @click="clearFile"
                  class="px-2.5 py-1 text-xs text-red-600 hover:bg-red-50 rounded-lg font-semibold cursor-pointer"
                  title="Chọn lại tệp khác"
                >
                  Đổi tệp
                </button>
              </div>
            </div>
          </div>

          <!-- 3. Smart Features Highlight -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div class="p-3 rounded-2xl bg-white border border-orange-100 shadow-2xs flex items-start gap-2.5">
              <span class="text-base p-1.5 rounded-xl bg-orange-100 text-orange-700">🧩</span>
              <div>
                <h4 class="text-xs font-bold text-gray-800">Nhận diện mọi dạng câu</h4>
                <p class="text-[11px] text-gray-500 mt-0.5">Trắc nghiệm ABCD, điền kết quả hoặc tự luận giải thích.</p>
              </div>
            </div>

            <div class="p-3 rounded-2xl bg-white border border-orange-100 shadow-2xs flex items-start gap-2.5">
              <span class="text-base p-1.5 rounded-xl bg-amber-100 text-amber-800">🖼️</span>
              <div>
                <h4 class="text-xs font-bold text-gray-800">Tự trích xuất hình vẽ</h4>
                <p class="text-[11px] text-gray-500 mt-0.5">Bóc tách toàn bộ hình ảnh minh họa nhúng trong đề Word/PDF.</p>
              </div>
            </div>

            <div class="p-3 rounded-2xl bg-white border border-orange-100 shadow-2xs flex items-start gap-2.5">
              <span class="text-base p-1.5 rounded-xl bg-emerald-100 text-emerald-800">⚖️</span>
              <div>
                <h4 class="text-xs font-bold text-gray-800">Gán độ khó & điểm chuẩn</h4>
                <p class="text-[11px] text-gray-500 mt-0.5">Tự động đánh giá Dễ (1đ), Trung bình (1.5đ) và Khó (2đ).</p>
              </div>
            </div>
          </div>
        </div>

        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <!-- BƯỚC 2: TIẾN TRÌNH XỬ LÝ (POLLING & STATUS)                         -->
        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <div v-else-if="currentStep === 2" class="py-8 px-4 text-center space-y-6">
          <div class="max-w-md mx-auto space-y-4">
            <!-- Spinner / Status Icon -->
            <div class="relative w-20 h-20 mx-auto">
              <div
                v-if="!processingError"
                class="w-20 h-20 rounded-full border-4 border-orange-200 border-t-orange-500 animate-spin"
              ></div>
              <div
                v-else
                class="w-20 h-20 rounded-full bg-red-100 text-red-600 flex items-center justify-center text-3xl font-bold"
              >
                ⚠️
              </div>
              <div
                v-if="!processingError"
                class="absolute inset-0 flex items-center justify-center text-xs font-extrabold text-orange-900"
              >
                {{ progressPercentage }}%
              </div>
            </div>

            <!-- Status Headline -->
            <div>
              <h3 class="text-base font-bold text-orange-950">
                {{ processingError ? 'Quá trình bóc tách gặp lỗi' : currentStatusText }}
              </h3>
              <p class="text-xs text-gray-500 mt-1">
                {{ processingSubText }}
              </p>
            </div>

            <!-- Progress Bar -->
            <div v-if="!processingError" class="w-full bg-orange-100 rounded-full h-2.5 overflow-hidden shadow-inner">
              <div
                class="bg-gradient-to-r from-orange-400 to-amber-500 h-2.5 rounded-full transition-all duration-500"
                :style="{ width: `${progressPercentage}%` }"
              ></div>
            </div>

            <!-- Error Box with detailed instructions -->
            <div
              v-if="processingError"
              class="p-4 rounded-2xl bg-red-50 border border-red-200 text-left text-xs space-y-2 text-red-900"
            >
              <p class="font-bold">Chi tiết thông báo:</p>
              <p class="font-mono text-[11px] bg-white/80 p-2.5 rounded-xl border border-red-200 break-words">
                {{ processingError }}
              </p>
              <div v-if="processingError.includes('[SCAN_DETECTED]')" class="text-[11px] text-red-700 pt-1">
                💡 <strong>Gợi ý:</strong> File PDF này là bản scan hình ảnh không có văn bản có thể chọn được. Vui lòng chuyển đổi sang file Word (.docx) hoặc dùng file PDF xuất từ Word/LaTeX có chữ.
              </div>
              <div v-else-if="processingError.includes('Port 8000') || processingError.includes('Network Error')" class="text-[11px] text-red-700 pt-1">
                💡 <strong>Gợi ý:</strong> Hãy kiểm tra dịch vụ <code>Question Import Worker</code> đã được khởi chạy trên cổng 8000 hay chưa (<code>python -m uvicorn app.main:app --port 8000</code>).
              </div>
            </div>

            <!-- Processing Stage Pipeline steps -->
            <div v-if="!processingError" class="p-3.5 rounded-2xl bg-orange-50/60 border border-orange-200/70 text-left text-xs space-y-2">
              <div
                v-for="stg in pipelineStages"
                :key="stg.key"
                class="flex items-center justify-between text-[11px]"
              >
                <div class="flex items-center gap-2">
                  <span v-if="stg.status === 'completed'" class="text-emerald-600 font-bold">✓</span>
                  <ArrowPathIcon v-else-if="stg.status === 'active'" class="w-3.5 h-3.5 text-orange-500 animate-spin" />
                  <span v-else class="text-gray-300">•</span>
                  <span :class="stg.status === 'active' ? 'font-bold text-orange-950' : stg.status === 'completed' ? 'text-gray-700' : 'text-gray-400'">
                    {{ stg.label }}
                  </span>
                </div>
                <span class="text-[10px] uppercase font-semibold text-gray-400">
                  {{ stg.status === 'completed' ? 'Xong' : stg.status === 'active' ? 'Đang chạy...' : 'Chờ' }}
                </span>
              </div>
            </div>

            <div v-if="processingError" class="pt-2">
              <button
                type="button"
                @click="currentStep = 1"
                class="px-5 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5"
              >
                <ArrowLeftIcon class="w-4 h-4" />
                <span>Quay lại chọn tệp khác</span>
              </button>
            </div>
          </div>
        </div>

        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <!-- BƯỚC 3: XEM TRƯỚC STAGING & CHỈNH SỬA TRƯỚC KHI LƯU                -->
        <!-- ═════════════════════════════════════════════════════════════════════ -->
        <div v-else-if="currentStep === 3" class="space-y-4">
          <!-- Summary Bar & Stats -->
          <div class="p-4 rounded-2xl bg-gradient-to-r from-emerald-50 via-teal-50 to-orange-50 border border-emerald-200 flex flex-wrap items-center justify-between gap-3 text-xs">
            <div class="flex items-center gap-3">
              <span class="w-9 h-9 rounded-xl bg-emerald-500 text-white flex items-center justify-center text-lg shadow-2xs font-bold">
                ✓
              </span>
              <div>
                <h4 class="font-bold text-gray-900 text-sm">
                  Đã bóc tách thành công {{ stagingQuestions.length }} câu hỏi!
                </h4>
                <p class="text-gray-500 text-[11px] mt-0.5">
                  Tệp: <strong>{{ selectedFile?.name }}</strong> • Ngân hàng đích: <strong class="text-orange-700">{{ currentBankName }}</strong>
                </p>
              </div>
            </div>

            <!-- Stats badges -->
            <div class="flex items-center gap-1.5 text-[11px] font-semibold">
              <span class="px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-900">
                MC: {{ countByType('MULTIPLE_CHOICE') }}
              </span>
              <span class="px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-900">
                Đúng/Sai: {{ countByType('TRUE_FALSE') }}
              </span>
              <span class="px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-900">
                Điền số: {{ countByType('FILL_IN') }}
              </span>
              <span class="px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-900">
                Tính theo nhịp: {{ countByType('TIMED_SEQUENCE') }}
              </span>
              <span class="px-2.5 py-1 rounded-lg bg-white border border-orange-200 text-orange-900">
                Tự luận: {{ countByType('ESSAY') }}
              </span>
            </div>
          </div>

          <!-- Selection & Filter Bar -->
          <div class="flex flex-wrap items-center justify-between gap-3 text-xs px-1">
            <div class="flex items-center gap-3">
              <label class="flex items-center gap-1.5 font-bold text-orange-950 cursor-pointer">
                <input
                  type="checkbox"
                  :checked="isAllSelected"
                  @change="toggleSelectAllStaging"
                  class="rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
                />
                Chọn tất cả ({{ stagingQuestions.length }})
              </label>

              <span class="text-orange-700 font-semibold">
                • Đã chọn <strong class="text-orange-900">{{ selectedStagingIndexes.length }}</strong> câu để nạp vào ngân hàng
              </span>
            </div>

            <div class="flex items-center gap-2">
              <select
                v-model="previewFilterType"
                class="text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-2.5 py-1.5 focus:outline-none"
              >
                <option value="">Tất cả dạng câu</option>
                <option value="MULTIPLE_CHOICE">Trắc nghiệm</option>
                <option value="TRUE_FALSE">Đúng / Sai</option>
                <option value="FILL_IN">Điền số</option>
                <option value="TIMED_SEQUENCE">Tính nhẩm theo nhịp</option>
                <option value="ESSAY">Tự luận</option>
              </select>
            </div>
          </div>

          <!-- Staging Questions List -->
          <div class="space-y-3.5 max-h-[50vh] overflow-y-auto pr-1">
            <div
              v-for="(q, idx) in filteredStagingQuestions"
              :key="idx"
              :class="[
                'p-4 rounded-2xl border transition-all space-y-3',
                selectedStagingIndexes.includes(idx)
                  ? 'bg-white border-orange-300 shadow-xs'
                  : 'bg-gray-50/70 border-gray-200 opacity-60'
              ]"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex items-center gap-2.5 flex-wrap">
                  <input
                    type="checkbox"
                    :value="idx"
                    v-model="selectedStagingIndexes"
                    class="rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
                  />
                  <span class="text-[11px] font-mono font-bold text-orange-800 bg-orange-100/70 px-2 py-0.5 rounded-md">
                    #{{ idx + 1 }}
                  </span>

                  <!-- Type Badge -->
                  <select
                    v-model="q.type"
                    class="text-[10px] font-bold px-2 py-1 rounded-full border border-gray-200 bg-white text-gray-800"
                    aria-label="Loại câu hỏi đã nhận diện"
                  >
                    <option value="MULTIPLE_CHOICE">Trắc nghiệm</option>
                    <option value="TRUE_FALSE">Đúng / Sai</option>
                    <option value="FILL_IN">Điền khuyết</option>
                    <option value="TIMED_SEQUENCE">Tính nhẩm theo nhịp</option>
                    <option value="ESSAY">Tự luận</option>
                  </select>

                  <!-- Editable Difficulty Badge -->
                  <select
                    v-model="q.difficulty"
                    class="text-[10px] font-bold px-2 py-0.5 rounded-lg border focus:outline-none cursor-pointer bg-white"
                    :class="[
                      q.difficulty === 'EASY' ? 'border-emerald-300 text-emerald-800' :
                      q.difficulty === 'MEDIUM' ? 'border-blue-300 text-blue-800' :
                      'border-amber-300 text-amber-800'
                    ]"
                  >
                    <option value="EASY">Dễ (1.0đ)</option>
                    <option value="MEDIUM">Trung bình (1.5đ)</option>
                    <option value="HARD">Khó (2.0đ)</option>
                  </select>
                </div>

                <div class="flex items-center gap-1">
                  <button
                    type="button"
                    @click="removeStagingQuestion(idx)"
                    class="p-1 text-gray-400 hover:text-red-600 rounded-lg transition-colors cursor-pointer"
                    title="Bỏ câu này khỏi danh sách import"
                  >
                    <TrashIcon class="w-4 h-4" />
                  </button>
                </div>
              </div>

              <!-- Question Content & Options Display -->
              <div class="space-y-2 text-xs">
                <!-- If Multiple Choice -->
                <template v-if="q.type === 'MULTIPLE_CHOICE'">
                  <p class="font-semibold text-gray-900 leading-relaxed whitespace-pre-line">
                    {{ getParsedStagingQuestion(q).questionText }}
                  </p>

                  <div
                    v-if="hasValidMCOptions(getParsedStagingQuestion(q))"
                    class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1"
                  >
                    <div
                      v-for="optKey in ['A', 'B', 'C', 'D']"
                      :key="optKey"
                      :class="[
                        'p-2 rounded-xl border text-[11px] flex items-start gap-2',
                        isCorrectOption(getParsedStagingQuestion(q), optKey)
                          ? 'bg-emerald-50 border-emerald-300 text-emerald-950 font-semibold'
                          : 'bg-orange-50/20 border-orange-100 text-gray-800'
                      ]"
                    >
                      <span
                        :class="[
                          'w-5 h-5 rounded-md flex items-center justify-center font-bold text-[10px] flex-shrink-0',
                          isCorrectOption(getParsedStagingQuestion(q), optKey)
                            ? 'bg-emerald-500 text-white'
                            : 'bg-orange-200/80 text-orange-950'
                        ]"
                      >
                        {{ optKey }}
                      </span>
                      <span class="flex-1">{{ getOptionText(getParsedStagingQuestion(q), optKey) }}</span>
                      <span
                        v-if="isCorrectOption(getParsedStagingQuestion(q), optKey)"
                        class="text-emerald-600 text-[10px] font-bold"
                      >
                        ✓ Đúng
                      </span>
                    </div>
                  </div>
                </template>

                <!-- If Fill-in or Essay -->
                <template v-else>
                  <p class="font-semibold text-gray-900 leading-relaxed whitespace-pre-line">
                    {{ q.content }}
                  </p>
                </template>
              </div>

              <!-- Extracted Image (if available) -->
              <div v-if="q.imageUrl || q.image_url" class="max-w-xs rounded-xl overflow-hidden border border-orange-200 bg-white p-1">
                <img
                  :src="q.imageUrl || q.image_url"
                  alt="Ảnh minh họa bóc tách"
                  class="w-full max-h-36 object-contain"
                  loading="lazy"
                />
              </div>

              <!-- Answer / Explanation -->
              <div class="p-2.5 rounded-xl bg-orange-50/70 border border-orange-200/60 text-[11px] text-gray-800 space-y-1">
                <span class="font-bold text-orange-900">🎯 Đáp án & Lời giải:</span>
                <p class="whitespace-pre-line text-gray-700 bg-white/70 p-2 rounded-lg border border-orange-100">
                  {{ q.answer }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── Modal Footer Action Buttons ──────────────────────────────────── -->
      <div class="px-6 py-4 border-t border-orange-100 bg-gray-50/60 flex items-center justify-between gap-3">
        <!-- Left Side: Back / Cancel -->
        <div>
          <button
            v-if="currentStep === 1"
            type="button"
            @click="closeModal"
            class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-200/60 transition-colors cursor-pointer"
          >
            Đóng
          </button>
          <button
            v-else-if="currentStep === 3"
            type="button"
            @click="currentStep = 1"
            :disabled="isSaving"
            class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-200/60 transition-colors cursor-pointer inline-flex items-center gap-1.5"
          >
            <ArrowLeftIcon class="w-3.5 h-3.5" />
            <span>Tải tệp khác</span>
          </button>
        </div>

        <!-- Right Side: Action Button -->
        <div class="flex items-center gap-2">
          <!-- Step 1 Button -->
          <button
            v-if="currentStep === 1"
            type="button"
            :disabled="!canStartProcess"
            @click="startProcessing"
            class="px-5 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-1.5 transition-all"
          >
            <SparklesIcon class="w-4 h-4" />
            <span>Bắt đầu bóc tách AI</span>
          </button>

          <!-- Step 2 Cancel Button (during polling) -->
          <button
            v-else-if="currentStep === 2 && !processingError"
            type="button"
            @click="cancelProcessing"
            class="px-4 py-2 rounded-xl border border-orange-200 text-xs font-semibold text-gray-600 hover:bg-orange-50 cursor-pointer"
          >
            Hủy tiến trình
          </button>

          <!-- Step 3 Commit to Database Button -->
          <button
            v-else-if="currentStep === 3"
            type="button"
            :disabled="selectedStagingIndexes.length === 0 || isSaving"
            @click="commitQuestionsToBank"
            class="px-6 py-2.5 rounded-xl bg-orange-500 hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed text-white text-xs font-bold shadow-xs cursor-pointer inline-flex items-center gap-2 transition-all"
          >
            <ArrowPathIcon v-if="isSaving" class="w-4 h-4 animate-spin" />
            <CheckIcon v-else class="w-4 h-4" />
            <span>Lưu {{ selectedStagingIndexes.length }} câu hỏi vào Ngân hàng</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import {
  SparklesIcon,
  FolderIcon,
  DocumentArrowUpIcon,
  CheckCircleIcon,
  CheckIcon,
  ArrowPathIcon,
  ArrowLeftIcon,
  TrashIcon,
} from '@heroicons/vue/24/outline'
import importWorkerService from '@/services/importWorker.service'
import questionBankService from '@/services/questionBank.service'
import { parseMultipleChoice } from '@/utils/media.helper'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  banks: {
    type: Array,
    default: () => [],
  },
  selectedBankId: {
    type: [Number, String, null],
    default: null,
  },
  courses: {
    type: Array,
    default: () => [],
  },
})

const emit = defineEmits(['update:modelValue', 'import-success', 'notify'])

// ─── Step Control ──────────────────────────────────────────────────────────
const currentStep = ref(1) // 1: Upload, 2: Processing, 3: Staging Review
const isProcessing = ref(false)
const isSaving = ref(false)
const isDragging = ref(false)
const fileInputRef = ref(null)

// ─── Step 1 State: Source & Destination ─────────────────────────────────────
const targetBankId = ref(null)
const isCreatingNewBank = ref(false)
const newBankCourseId = ref(null)
const newBankName = ref('')
const selectedFile = ref(null)

// ─── Step 2 State: Polling Progress ─────────────────────────────────────────
const progressPercentage = ref(0)
const currentStatusText = ref('Đang kết nối...')
const processingSubText = ref('Chuẩn bị tệp tải lên máy chủ...')
const processingError = ref('')
let activeJobId = null
let cancelPolling = false

const pipelineStages = reactive([
  { key: 'UPLOAD', label: '1. Tải tệp lên máy chủ tiếp nhận', status: 'pending' },
  { key: 'EXTRACT', label: '2. Bóc tách tệp DOCX/PDF & hình ảnh nhúng', status: 'pending' },
  { key: 'CHUNK', label: '3. Phân đoạn Regex các khối câu hỏi', status: 'pending' },
  { key: 'PARSE', label: '4. AI LLM chuẩn hóa nội dung & đáp án', status: 'pending' },
])

// ─── Step 3 State: Staging Questions ────────────────────────────────────────
const stagingQuestions = ref([])
const selectedStagingIndexes = ref([])
const previewFilterType = ref('')

// Initialize or react to props
watch(
  () => props.modelValue,
  (val) => {
    if (val) {
      resetModal()
      if (props.selectedBankId) {
        targetBankId.value = props.selectedBankId
      } else if (props.banks.length > 0) {
        targetBankId.value = props.banks[0].id
      }
      if (props.courses.length > 0) {
        newBankCourseId.value = props.courses[0].id
      }
    }
  }
)

const canStartProcess = computed(() => {
  if (!selectedFile.value) return false
  if (isCreatingNewBank.value) {
    return !!(newBankCourseId.value && newBankName.value.trim())
  }
  return !!targetBankId.value
})

const currentBankName = computed(() => {
  if (isCreatingNewBank.value) {
    return newBankName.value || 'Ngân hàng mới'
  }
  const b = props.banks.find((x) => x.id === targetBankId.value)
  return b ? b.name : 'Ngân hàng được chọn'
})

// ─── Drag & Drop / File Selection ──────────────────────────────────────────
function triggerFileInput() {
  fileInputRef.value?.click()
}

function handleFileInputChange(e) {
  const file = e.target.files?.[0]
  if (file) validateAndSetFile(file)
}

function handleFileDrop(e) {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file) validateAndSetFile(file)
}

function validateAndSetFile(file) {
  const allowedExts = ['.docx', '.pdf']
  const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase()
  if (!allowedExts.includes(ext)) {
    emit('notify', `Định dạng tệp ${ext} không hợp lệ. Vui lòng chọn tệp .docx hoặc .pdf!`, true)
    return
  }
  selectedFile.value = file
}

function clearFile() {
  selectedFile.value = null
  if (fileInputRef.value) fileInputRef.value.value = ''
}

function formatFileSize(bytes) {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

// ─── Step 1 -> Step 2: Start Processing ────────────────────────────────────
async function startProcessing() {
  if (!canStartProcess.value) return

  // Nếu người dùng chọn tạo ngân hàng mới tại chỗ, tạo trước qua API
  let finalBankId = targetBankId.value
  if (isCreatingNewBank.value) {
    try {
      const created = await questionBankService.create({
        courseId: newBankCourseId.value,
        name: newBankName.value.trim(),
        description: 'Được tạo tự động từ Import Worker',
      })
      finalBankId = created.id
      targetBankId.value = created.id
      isCreatingNewBank.value = false
    } catch (err) {
      console.error('Lỗi tạo ngân hàng mới:', err)
      emit('notify', 'Không thể tạo ngân hàng câu hỏi mới. Vui lòng thử lại.', true)
      return
    }
  }

  currentStep.value = 2
  isProcessing.value = true
  processingError.value = ''
  progressPercentage.value = 5
  currentStatusText.value = 'Đang tải tệp lên máy chủ xử lý...'
  processingSubText.value = 'Gửi dữ liệu tới Question Import Worker...'
  cancelPolling = false

  pipelineStages[0].status = 'active'
  pipelineStages[1].status = 'pending'
  pipelineStages[2].status = 'pending'
  pipelineStages[3].status = 'pending'

  try {
    // 1. Gửi file lên Import Worker
    const res = await importWorkerService.uploadFile(selectedFile.value, finalBankId)
    activeJobId = res.job_id
    pipelineStages[0].status = 'completed'
    pipelineStages[1].status = 'active'
    progressPercentage.value = 20
    currentStatusText.value = 'Đang bóc tách văn bản và trích xuất hình ảnh...'
    processingSubText.value = 'Bóc tách các đoạn văn bản và biểu đồ, hình vẽ nhúng...'

    // 2. Bắt đầu Polling tiến độ
    const completedJob = await importWorkerService.pollJobUntilDone(
      activeJobId,
      (statusData) => {
        if (cancelPolling) return
        updatePipelineStatus(statusData)
      },
      2500,
      240
    )

    if (cancelPolling) return

    // 3. Hoàn tất thành công -> Chuyển sang Bước 3
    progressPercentage.value = 100
    pipelineStages.forEach((s) => (s.status = 'completed'))
    currentStatusText.value = 'Trích xuất hoàn tất!'

    stagingQuestions.value = completedJob.questions.map((item) => ({
      ...item,
      bankId: finalBankId,
      // Đảm bảo các thuộc tính tương thích
      imageUrl: item.imageUrl || item.image_url || null,
      difficulty: item.difficulty || 'EASY',
      type: item.type || 'MULTIPLE_CHOICE',
    }))

    // Mặc định chọn tất cả
    selectedStagingIndexes.value = stagingQuestions.value.map((_, i) => i)

    setTimeout(() => {
      currentStep.value = 3
      isProcessing.value = false
    }, 600)
  } catch (err) {
    if (cancelPolling) return
    console.error('Lỗi tiến trình Import Worker:', err)
    processingError.value = typeof err === 'string' ? err : err.message || 'Lỗi không xác định khi xử lý'
    isProcessing.value = false
  }
}

function updatePipelineStatus(statusData) {
  const p = statusData.progress_percentage || 0
  progressPercentage.value = Math.max(progressPercentage.value, p)

  if (statusData.status === 'EXTRACTING') {
    pipelineStages[0].status = 'completed'
    pipelineStages[1].status = 'active'
    currentStatusText.value = 'Đang bóc tách tệp đề thi...'
    processingSubText.value = `Đang đọc nội dung và lưu các hình ảnh nhúng (${p}%)...`
  } else if (statusData.status === 'CHUNKING') {
    pipelineStages[0].status = 'completed'
    pipelineStages[1].status = 'completed'
    pipelineStages[2].status = 'active'
    currentStatusText.value = 'Đang phân đoạn câu hỏi...'
    processingSubText.value = `Phát hiện các khối câu hỏi và gắn thẻ hình ảnh (${p}%)...`
  } else if (statusData.status === 'PARSING') {
    pipelineStages[0].status = 'completed'
    pipelineStages[1].status = 'completed'
    pipelineStages[2].status = 'completed'
    pipelineStages[3].status = 'active'
    const provider = (statusData.parser_provider || 'AI').toLowerCase()
    currentStatusText.value = provider === 'mock' || provider === 'heuristic'
      ? 'Bộ phân tích thử nghiệm đang xử lý đề...'
      : `Mô hình ${provider} AI đang phân tích đề...`
    processingSubText.value = provider === 'mock' || provider === 'heuristic'
      ? `Chế độ này không gọi mô hình AI, chỉ dùng để kiểm thử offline (${p}%).`
      : statusData.total_chunks
        ? `Đang dùng mô hình AI ${provider} phân tích ${statusData.total_questions || 0}/${statusData.total_chunks} câu (${p}%)...`
        : `Đang dùng mô hình AI ${provider} để nhận diện dạng câu hỏi và đáp án (${p}%)...`
  }
}

function cancelProcessing() {
  cancelPolling = true
  isProcessing.value = false
  currentStep.value = 1
}

// ─── Step 3: Staging Filtering & Selection ─────────────────────────────────
const filteredStagingQuestions = computed(() => {
  if (!previewFilterType.value) return stagingQuestions.value
  return stagingQuestions.value.filter((q) => q.type === previewFilterType.value)
})

const isAllSelected = computed(() => {
  return (
    stagingQuestions.value.length > 0 &&
    selectedStagingIndexes.value.length === stagingQuestions.value.length
  )
})

function toggleSelectAllStaging() {
  if (isAllSelected.value) {
    selectedStagingIndexes.value = []
  } else {
    selectedStagingIndexes.value = stagingQuestions.value.map((_, i) => i)
  }
}

function countByType(type) {
  return stagingQuestions.value.filter((q) => q.type === type).length
}

function removeStagingQuestion(idx) {
  stagingQuestions.value.splice(idx, 1)
  // Re-adjust selection
  selectedStagingIndexes.value = selectedStagingIndexes.value
    .filter((i) => i !== idx)
    .map((i) => (i > idx ? i - 1 : i))
}

// Helper Multiple Choice Parser
const parsedCache = reactive({})
function getParsedStagingQuestion(q) {
  const key = `${q.content}_${q.answer}`
  if (!parsedCache[key]) {
    parsedCache[key] = parseMultipleChoice(q.content, q.answer)
  }
  return parsedCache[key]
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

// ─── Step 3: Commit to Database ────────────────────────────────────────────
async function commitQuestionsToBank() {
  const selectedQuestions = selectedStagingIndexes.value
    .map((i) => stagingQuestions.value[i])
    .filter(Boolean)

  if (selectedQuestions.length === 0) {
    emit('notify', 'Vui lòng chọn ít nhất 1 câu hỏi để lưu vào ngân hàng!', true)
    return
  }

  isSaving.value = true
  const bankId = targetBankId.value

  try {
    const payloadQuestions = selectedQuestions.map((q) => ({
      bankId: bankId,
      type: q.type || 'MULTIPLE_CHOICE',
      content: q.content,
      imageUrl: q.imageUrl || q.image_url || null,
      difficulty: q.difficulty || 'EASY',
      answer: q.answer,
    }))

    // Thử dùng API Bulk Create trước
    try {
      await questionBankService.bulkCreateQuestions(bankId, payloadQuestions)
    } catch (bulkErr) {
      console.warn('Bulk endpoint chưa sẵn sàng, chuyển sang lưu tuần tự:', bulkErr)
      // Fallback lưu tuần tự nếu bulk endpoint gặp sự cố
      for (const item of payloadQuestions) {
        await questionBankService.createQuestion(item)
      }
    }

    emit('import-success', bankId, selectedQuestions.length)
    emit('notify', `Đã lưu thành công ${selectedQuestions.length} câu hỏi vào ngân hàng!`)
    closeModal()
  } catch (err) {
    console.error('Lỗi lưu câu hỏi vào ngân hàng:', err)
    emit('notify', 'Có lỗi xảy ra khi lưu câu hỏi vào ngân hàng.', true)
  } finally {
    isSaving.value = false
  }
}

// ─── Modal Helpers ─────────────────────────────────────────────────────────
function closeModal() {
  if (isProcessing.value) return
  emit('update:modelValue', false)
}

function resetModal() {
  currentStep.value = 1
  isProcessing.value = false
  isSaving.value = false
  isDragging.value = false
  selectedFile.value = null
  stagingQuestions.value = []
  selectedStagingIndexes.value = []
  processingError.value = ''
  progressPercentage.value = 0
  isCreatingNewBank.value = false
  newBankName.value = ''
}
</script>
