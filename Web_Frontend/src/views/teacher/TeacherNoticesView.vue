<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-orange-950">Gửi thông báo & Nhắc nhở</h1>
        <p class="text-xs text-gray-500 mt-0.5">
          Giao tiếp với phụ huynh, gửi lời nhắc học tập hoặc thông báo chung cho lớp
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="showComposeModal = true"
        >
          <PaperAirplaneIcon class="w-4 h-4" />
          Soạn tin nhắn mới
        </button>
      </div>
    </div>

    <!-- ─── Filter Tabs & History ────────────────────────────────────────── -->
    <div class="bg-white rounded-2xl border border-orange-100 shadow-sm p-4 space-y-3">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="flex items-center bg-orange-100/70 p-1 rounded-xl text-xs font-semibold">
          <button
            @click="activeTab = 'sent'"
            :class="[
              'px-3.5 py-1.5 rounded-lg transition-all cursor-pointer',
              activeTab === 'sent' ? 'bg-orange-400 text-white shadow-xs' : 'text-orange-900',
            ]"
          >
            Lịch sử đã gửi ({{ sentList.length }})
          </button>
          <button
            @click="activeTab = 'center'"
            :class="[
              'px-3.5 py-1.5 rounded-lg transition-all cursor-pointer',
              activeTab === 'center' ? 'bg-orange-400 text-white shadow-xs' : 'text-orange-900',
            ]"
          >
            Thông báo trung tâm
          </button>
        </div>

        <SearchBar v-model="search" placeholder="Tìm kiếm thông báo..." class="max-w-xs" />
      </div>
    </div>

    <!-- ─── Sent Notices List ────────────────────────────────────────────── -->
    <div v-if="filteredList.length === 0" class="bg-white rounded-2xl border border-orange-100 p-12 text-center">
      <EmptyState
        :icon="EnvelopeIcon"
        message="Chưa có thông báo nào"
        sub="Nhấn 'Soạn tin nhắn mới' để gửi thông báo đầu tiên đến phụ huynh."
      />
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="msg in filteredList"
        :key="msg.id"
        class="bg-white rounded-2xl border border-orange-100 p-5 shadow-sm hover:border-orange-300 transition-colors flex flex-col sm:flex-row sm:items-start justify-between gap-4"
      >
        <div class="flex items-start gap-3.5">
          <div
            :class="[
              'w-11 h-11 rounded-xl flex items-center justify-center text-white flex-shrink-0 shadow-2xs',
              msg.recipientType === 'class' ? 'bg-orange-400' : 'bg-blue-400'
            ]"
          >
            <component :is="msg.recipientType === 'class' ? UserGroupIcon : UserIcon" class="w-5 h-5" />
          </div>

          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <span class="text-sm font-bold text-gray-900">{{ msg.title }}</span>
              <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-orange-100 text-orange-900">
                Gửi tới: {{ msg.recipientName }}
              </span>
              <span class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                ✓ Đã gửi
              </span>
            </div>

            <p class="text-xs text-gray-600 mt-1.5 leading-relaxed">
              {{ msg.content }}
            </p>

            <div class="flex flex-wrap items-center gap-3 text-[11px] text-gray-400 mt-2">
              <span>⏰ {{ msg.sentAt }}</span>
              <span>📱 Kênh: {{ msg.channels.join(', ') }}</span>
              <span>👀 Đã đọc: {{ msg.readCount }} / {{ msg.totalRecipients }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── Compose Modal ────────────────────────────────────────────────── -->
    <div
      v-if="showComposeModal"
      class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4 backdrop-blur-2xs"
    >
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-xl border border-orange-100 space-y-4">
        <div class="flex items-center justify-between border-b border-orange-100 pb-3">
          <h3 class="text-base font-bold text-orange-950">Soạn thông báo / Nhắc nhở mới</h3>
          <button @click="showComposeModal = false" class="text-gray-400 hover:text-gray-600 text-lg">✕</button>
        </div>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-gray-700 mb-1">Đối tượng người nhận</label>
            <select v-model="newNotice.recipientType" class="w-full rounded-xl border border-orange-200 p-2.5">
              <option value="class">Cả lớp học (Toán Tư Duy 3A)</option>
              <option value="single">Phụ huynh học sinh cụ thể</option>
            </select>
          </div>

          <div v-if="newNotice.recipientType === 'single'">
            <label class="block font-semibold text-gray-700 mb-1">Chọn học viên</label>
            <select v-model="newNotice.studentName" class="w-full rounded-xl border border-orange-200 p-2.5">
              <option value="Nguyễn Minh Quân (PH: Nguyễn Văn Tuấn)">Nguyễn Minh Quân (PH: Nguyễn Văn Tuấn)</option>
              <option value="Trần Bảo An (PH: Lê Thu Hương)">Trần Bảo An (PH: Lê Thu Hương)</option>
              <option value="Lê Hoàng Phúc (PH: Lê Hoàng Nam)">Lê Hoàng Phúc (PH: Lê Hoàng Nam)</option>
            </select>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Tiêu đề thông báo</label>
            <input
              type="text"
              v-model="newNotice.title"
              placeholder="VD: Nhắc nhở hoàn thành bài tập về nhà tuần 44"
              class="w-full rounded-xl border border-orange-200 p-2.5"
            />
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Nội dung tin nhắn</label>
            <textarea
              v-model="newNotice.content"
              rows="4"
              placeholder="Nhập nội dung cần gửi đến phụ huynh..."
              class="w-full rounded-xl border border-orange-200 p-2.5"
            ></textarea>
          </div>

          <div>
            <label class="block font-semibold text-gray-700 mb-1">Kênh gửi</label>
            <div class="flex items-center gap-4 text-gray-700">
              <label class="flex items-center gap-1.5 cursor-pointer">
                <input type="checkbox" checked class="rounded text-orange-500" />
                App Phụ huynh
              </label>
              <label class="flex items-center gap-1.5 cursor-pointer">
                <input type="checkbox" checked class="rounded text-orange-500" />
                Tin nhắn SMS
              </label>
            </div>
          </div>
        </div>

        <div class="pt-3 border-t border-orange-100 flex items-center justify-end gap-2">
          <button
            type="button"
            @click="showComposeModal = false"
            class="px-4 py-2 rounded-xl text-xs font-semibold text-gray-600 hover:bg-gray-100"
          >
            Hủy
          </button>
          <button
            type="button"
            @click="sendNotice"
            class="px-4 py-2 rounded-xl bg-orange-400 hover:bg-orange-500 text-white text-xs font-bold shadow-xs"
          >
            Gửi thông báo ngay
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import {
  PaperAirplaneIcon,
  EnvelopeIcon,
  UserGroupIcon,
  UserIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const activeTab = ref('sent')
const search = ref('')
const showComposeModal = ref(false)

const newNotice = ref({
  recipientType: 'class',
  studentName: 'Nguyễn Minh Quân (PH: Nguyễn Văn Tuấn)',
  title: '',
  content: '',
})

const sentList = ref([
  {
    id: 1,
    title: 'Nhắc nhở nộp bài tập tuần 44 - Dãy số quy luật',
    recipientType: 'class',
    recipientName: 'Lớp Toán Tư Duy 3A',
    content: 'Kính gửi quý phụ huynh, hiện tại còn 4 học sinh chưa nộp bài tập tuần 44. Kính nhờ phụ huynh hỗ trợ nhắc các con hoàn thành trước 20h00 tối nay.',
    sentAt: '09:30 - Hôm nay',
    channels: ['App Phụ huynh', 'SMS'],
    readCount: 14,
    totalRecipients: 18,
  },
  {
    id: 2,
    title: 'Khen ngợi học sinh có tiến bộ vượt bậc trong buổi học',
    recipientType: 'single',
    recipientName: 'PH em Trần Bảo An',
    content: 'Chào phụ huynh, hôm nay em An rất hăng hái phát biểu và tìm ra lời giải hình học rất sáng tạo. Thầy gửi lời khen đến em!',
    sentAt: '16:00 - Hôm qua',
    channels: ['App Phụ huynh'],
    readCount: 1,
    totalRecipients: 1,
  },
])

const filteredList = computed(() => {
  if (!search.value.trim()) return sentList.value
  const q = search.value.toLowerCase().trim()
  return sentList.value.filter(m => m.title.toLowerCase().includes(q) || m.content.toLowerCase().includes(q))
})

function sendNotice() {
  if (!newNotice.value.title || !newNotice.value.content) {
    alert('Vui lòng nhập tiêu đề và nội dung tin nhắn!')
    return
  }

  sentList.value.unshift({
    id: Date.now(),
    title: newNotice.value.title,
    recipientType: newNotice.value.recipientType,
    recipientName: newNotice.value.recipientType === 'class' ? 'Lớp Toán Tư Duy 3A' : newNotice.value.studentName,
    content: newNotice.value.content,
    sentAt: 'Vừa xong',
    channels: ['App Phụ huynh', 'SMS'],
    readCount: 0,
    totalRecipients: newNotice.value.recipientType === 'class' ? 18 : 1,
  })

  showComposeModal.value = false
  newNotice.value.title = ''
  newNotice.value.content = ''
  alert('Đã gửi thông báo thành công đến phụ huynh!')
}
</script>
