<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Ngân hàng câu hỏi & Bài tập</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Tra cứu, lọc câu hỏi theo chuyên đề và trích xuất bộ đề/bài tập cho học sinh
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          :disabled="selectedQuestions.length === 0"
          :class="{ 'opacity-50 cursor-not-allowed': selectedQuestions.length === 0 }"
          @click="exportExam"
        >
          <ArrowDownTrayIcon class="w-4 h-4" />
          Trích xuất bộ đề ({{ selectedQuestions.length }})
        </button>
      </div>
    </div>

    <!-- ─── Search & Filters ─────────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm space-y-3">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
        <!-- Search bar -->
        <div class="md:col-span-2">
          <label class="block text-xs font-semibold text-gray-600 mb-1">Từ khóa câu hỏi</label>
          <SearchBar
            v-model="search"
            placeholder="Tìm theo nội dung câu hỏi, dạng bài..."
          />
        </div>

        <!-- Grade / Topic filter -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Chuyên đề</label>
          <select
            v-model="topicFilter"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả chuyên đề</option>
            <option value="arithmetic">Số học & Tính toán nhanh</option>
            <option value="geometry">Hình học & Tư duy không gian</option>
            <option value="logic">Toán tư duy logic & Quy luật</option>
            <option value="word_problems">Toán đố có lời văn</option>
          </select>
        </div>

        <!-- Difficulty level -->
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Mức độ nhận thức</label>
          <select
            v-model="levelFilter"
            class="w-full text-xs font-semibold bg-white border border-orange-200 text-gray-800 rounded-xl px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-orange-300"
          >
            <option value="all">Tất cả mức độ</option>
            <option value="easy">Nhận biết</option>
            <option value="medium">Thông hiểu</option>
            <option value="hard">Vận dụng</option>
            <option value="expert">Vận dụng cao</option>
          </select>
        </div>
      </div>
    </div>

    <!-- ─── Selection Bar ────────────────────────────────────────────────── -->
    <div class="flex items-center justify-between text-xs text-gray-500">
      <div class="flex items-center gap-2">
        <label class="flex items-center gap-1.5 font-semibold text-orange-950 cursor-pointer">
          <input
            type="checkbox"
            :checked="isAllSelected"
            @change="toggleSelectAll"
            class="rounded text-orange-500 focus:ring-orange-400"
          />
          Chọn tất cả ({{ filteredQuestions.length }})
        </label>
        <span v-if="selectedQuestions.length > 0" class="text-orange-600 font-bold">
          • Đã chọn {{ selectedQuestions.length }} câu
        </span>
      </div>
      <p>Hiển thị {{ filteredQuestions.length }} câu hỏi phù hợp</p>
    </div>

    <!-- ─── Questions List ───────────────────────────────────────────────── -->
    <div v-if="filteredQuestions.length === 0" class="bg-white rounded-2xl border border-orange-100 p-12 text-center">
      <EmptyState
        :icon="DocumentTextIcon"
        message="Không tìm thấy câu hỏi nào"
        sub="Hãy thử thay đổi từ khóa hoặc bộ lọc chuyên đề / mức độ."
      />
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="q in filteredQuestions"
        :key="q.id"
        class="bg-white rounded-2xl border border-orange-100 p-4 shadow-sm hover:border-orange-300 transition-all flex items-start gap-3.5"
      >
        <input
          type="checkbox"
          :value="q.id"
          v-model="selectedQuestions"
          class="mt-1 rounded text-orange-500 focus:ring-orange-400 cursor-pointer"
        />

        <div class="flex-1 min-w-0">
          <div class="flex flex-wrap items-center gap-2 mb-1.5">
            <span class="text-[11px] font-mono font-bold text-orange-700 bg-orange-50 border border-orange-200 px-2 py-0.5 rounded">
              {{ q.code }}
            </span>
            <span class="text-[10px] font-semibold text-orange-900 bg-orange-100 px-2 py-0.5 rounded-full">
              {{ q.topicLabel }}
            </span>
            <span
              :class="[
                'text-[10px] font-bold px-2 py-0.5 rounded-full',
                q.level === 'easy' ? 'bg-emerald-100 text-emerald-800' :
                q.level === 'medium' ? 'bg-blue-100 text-blue-800' :
                q.level === 'hard' ? 'bg-amber-100 text-amber-800' :
                'bg-red-100 text-red-800'
              ]"
            >
              {{ q.levelLabel }}
            </span>
          </div>

          <p class="text-xs font-semibold text-gray-900 leading-relaxed">
            {{ q.content }}
          </p>

          <!-- Choices if multiple choice -->
          <div v-if="q.options" class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2 text-xs text-gray-700">
            <div
              v-for="(opt, idx) in q.options"
              :key="idx"
              :class="[
                'p-2 rounded-lg border text-[11px]',
                q.showAnswer && opt.isCorrect
                  ? 'border-emerald-300 bg-emerald-50 text-emerald-900 font-bold'
                  : 'border-gray-100 bg-gray-50/50'
              ]"
            >
              <span class="font-bold mr-1">{{ ['A', 'B', 'C', 'D'][idx] }}.</span>
              {{ opt.text }}
            </div>
          </div>

          <!-- Answer & Solution Toggle -->
          <div class="mt-3 flex items-center justify-between pt-2 border-t border-orange-50 text-xs">
            <button
              type="button"
              @click="q.showAnswer = !q.showAnswer"
              class="text-orange-600 hover:text-orange-800 font-semibold cursor-pointer"
            >
              {{ q.showAnswer ? 'Ẩn đáp án & lời giải' : 'Xem đáp án & lời giải' }}
            </button>
            <span class="text-[11px] text-gray-400">Đã dùng trong 3 kỳ kiểm tra</span>
          </div>

          <div v-if="q.showAnswer" class="mt-2 p-2.5 rounded-xl bg-orange-50 border border-orange-200 text-xs text-orange-950">
            <p class="font-bold text-orange-900">💡 Lời giải chi tiết:</p>
            <p class="mt-0.5 text-gray-700 leading-relaxed">{{ q.solution }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  DocumentTextIcon,
  ArrowDownTrayIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const search = ref('')
const topicFilter = ref('all')
const levelFilter = ref('all')
const selectedQuestions = ref([])

const questionsList = ref([
  {
    id: 1,
    code: 'CH-TD01',
    topic: 'logic',
    topicLabel: 'Toán Tư Duy Logic',
    level: 'easy',
    levelLabel: 'Nhận biết',
    content: 'Một hình vuông có chu vi là 36cm. Hỏi diện tích của hình vuông đó bằng bao nhiêu cm²?',
    options: [
      { text: '81 cm²', isCorrect: true },
      { text: '64 cm²', isCorrect: false },
      { text: '36 cm²', isCorrect: false },
      { text: '72 cm²', isCorrect: false },
    ],
    solution: 'Cạnh của hình vuông là: 36 : 4 = 9 (cm). Diện tích hình vuông là: 9 x 9 = 81 (cm²).',
    showAnswer: false,
  },
  {
    id: 2,
    code: 'CH-TD02',
    topic: 'arithmetic',
    topicLabel: 'Số học & Tính nhanh',
    level: 'medium',
    levelLabel: 'Thông hiểu',
    content: 'Tính nhanh tổng của dãy số sau: S = 2 + 4 + 6 + 8 + ... + 48 + 50.',
    options: [
      { text: '650', isCorrect: true },
      { text: '625', isCorrect: false },
      { text: '550', isCorrect: false },
      { text: '700', isCorrect: false },
    ],
    solution: 'Dãy số có số số hạng là: (50 - 2) : 2 + 1 = 25 số hạng. Tổng S = (50 + 2) x 25 : 2 = 650.',
    showAnswer: false,
  },
  {
    id: 3,
    code: 'CH-TD03',
    topic: 'word_problems',
    topicLabel: 'Toán đố có lời văn',
    level: 'hard',
    levelLabel: 'Vận dụng',
    content: 'Vừa gà vừa chó bó lại cho tròn, 36 con, 100 chân chẵn. Hỏi có bao nhiêu con gà và bao nhiêu con chó?',
    options: [
      { text: '22 con gà, 14 con chó', isCorrect: true },
      { text: '20 con gà, 16 con chó', isCorrect: false },
      { text: '24 con gà, 12 con chó', isCorrect: false },
      { text: '18 con gà, 18 con chó', isCorrect: false },
    ],
    solution: 'Giả sử 36 con đều là gà thì có 72 chân. Số chân thiếu: 100 - 72 = 28 chân. Số chó: 28 : 2 = 14 con. Số gà: 36 - 14 = 22 con.',
    showAnswer: false,
  },
])

const filteredQuestions = computed(() => {
  return questionsList.value.filter(q => {
    const matchSearch =
      !search.value.trim() ||
      q.content.toLowerCase().includes(search.value.toLowerCase()) ||
      q.code.toLowerCase().includes(search.value.toLowerCase())
    const matchTopic = topicFilter.value === 'all' || q.topic === topicFilter.value
    const matchLevel = levelFilter.value === 'all' || q.level === levelFilter.value
    return matchSearch && matchTopic && matchLevel
  })
})

const isAllSelected = computed(() => {
  return (
    filteredQuestions.value.length > 0 &&
    filteredQuestions.value.every(q => selectedQuestions.value.includes(q.id))
  )
})

function toggleSelectAll() {
  if (isAllSelected.value) {
    selectedQuestions.value = []
  } else {
    selectedQuestions.value = filteredQuestions.value.map(q => q.id)
  }
}

function exportExam() {
  alert(`Đang trích xuất đề thi gồm ${selectedQuestions.value.length} câu hỏi đã chọn sang định dạng Word/PDF...`)
}
</script>
