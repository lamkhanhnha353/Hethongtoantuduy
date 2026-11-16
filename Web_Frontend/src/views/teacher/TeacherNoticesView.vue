<template>
  <div class="space-y-5">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-stone-900">Gửi thông báo & Nhắc nhở</h1>
        <p class="text-xs text-stone-500 mt-0.5">
          Giao tiếp với phụ huynh, gửi lời nhắc học tập hoặc thông báo chung cho lớp
        </p>
      </div>
      <div class="flex items-center gap-2">
        <button
          type="button"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          @click="showComposeModal = true"
        >
          <PaperAirplaneIcon class="w-4 h-4" />
          Soạn tin nhắn mới
        </button>
      </div>
    </div>

    <!-- ─── Filter Tabs & History ────────────────────────────────────────── -->
    <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-4 space-y-3">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div class="inline-flex items-center bg-stone-100 border border-stone-300 p-1 rounded-md text-xs font-semibold tabular-nums">
          <button
            @click="switchTab('sent')"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              activeTab === 'sent' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Lịch sử đã gửi ({{ sentList.length }})
          </button>
          <button
            @click="switchTab('center')"
            :class="[
              'h-7 px-3 rounded transition-colors cursor-pointer',
              activeTab === 'center' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            Thông báo trung tâm
          </button>
          <button
            @click="switchTab('inbox')"
            :class="[
              'inline-flex items-center gap-1.5 h-7 px-3 rounded transition-colors cursor-pointer',
              activeTab === 'inbox' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
            ]"
          >
            <span>Thông báo của tôi</span>
            <span
              v-if="unreadCount > 0"
              :class="[
                'inline-flex min-w-4 items-center justify-center rounded-full px-1.5 py-0.5 text-[10px] font-bold leading-none',
                activeTab === 'inbox' ? 'bg-white text-orange-700' : 'bg-orange-600 text-white',
              ]"
            >
              {{ unreadCount > 99 ? '99+' : unreadCount }}
            </span>
          </button>
        </div>

        <SearchBar v-if="activeTab !== 'inbox'" v-model="search" placeholder="Tìm kiếm thông báo..." class="max-w-xs" />
      </div>
    </div>

    <!-- ─── Personal Notification Inbox ──────────────────────────────────── -->
    <NotificationInboxPanel v-if="activeTab === 'inbox'" />

    <!-- ─── Sent Notices List ────────────────────────────────────────────── -->
    <template v-else>
    <div v-if="filteredList.length === 0" class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] p-12 text-center">
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
        class="bg-white rounded-lg border border-stone-300 p-5 shadow-[0_1px_2px_rgba(0,0,0,0.04)] hover:border-stone-400 transition-colors flex flex-col sm:flex-row sm:items-start justify-between gap-4"
      >
        <div class="flex items-start gap-3.5">
          <div
            :class="[
              'w-10 h-10 rounded-md border flex items-center justify-center flex-shrink-0',
              msg.recipientType === 'class' ? 'bg-orange-50 text-orange-700 border-orange-200' : 'bg-stone-100 text-stone-700 border-stone-300'
            ]"
          >
            <component :is="msg.recipientType === 'class' ? UserGroupIcon : UserIcon" class="w-5 h-5" />
          </div>

          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <span class="text-sm font-bold text-stone-900">{{ msg.title }}</span>
              <span class="text-[11px] font-semibold px-2 py-0.5 rounded border border-orange-300 bg-orange-50 text-orange-800">
                Gửi tới: {{ msg.recipientName }}
              </span>
              <span class="text-[11px] font-semibold px-2 py-0.5 rounded border border-emerald-300 bg-emerald-50 text-emerald-800">
                ✓ Đã gửi
              </span>
            </div>

            <p class="text-xs text-stone-600 mt-1.5 leading-relaxed">
              {{ msg.content }}
            </p>

            <div class="flex flex-wrap items-center gap-3 text-[11px] text-stone-500 mt-2 tabular-nums">
              <span>⏰ {{ msg.sentAt }}</span>
              <span>📱 Kênh: {{ msg.channels.join(', ') }}</span>
              <span>👀 Đã đọc: {{ msg.readCount }} / {{ msg.totalRecipients }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    </template>

    <!-- ─── Compose Modal ────────────────────────────────────────────────── -->
    <div
      v-if="showComposeModal"
      class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4 backdrop-blur-xs"
    >
      <div class="bg-white rounded-lg max-w-lg w-full shadow-xl border border-stone-300 overflow-hidden">
        <div class="flex items-center justify-between bg-stone-50 border-b border-stone-200 px-6 py-4">
          <h3 class="text-base font-bold text-stone-900">Soạn thông báo / Nhắc nhở mới</h3>
          <button @click="showComposeModal = false" class="text-stone-400 hover:text-stone-700 text-lg cursor-pointer">✕</button>
        </div>

        <div class="p-6 space-y-4 text-xs">
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Đối tượng người nhận</label>
            <select v-model="newNotice.recipientType" class="w-full h-10 rounded-md bg-stone-50/70 border border-stone-300 px-3 text-stone-900 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none">
              <option value="class">Cả lớp học (Toán Tư Duy 3A)</option>
              <option value="single">Phụ huynh học sinh cụ thể</option>
            </select>
          </div>

          <div v-if="newNotice.recipientType === 'single'">
            <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Chọn học viên</label>
            <select v-model="newNotice.studentName" class="w-full h-10 rounded-md bg-stone-50/70 border border-stone-300 px-3 text-stone-900 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none">
              <option value="Nguyễn Minh Quân (PH: Nguyễn Văn Tuấn)">Nguyễn Minh Quân (PH: Nguyễn Văn Tuấn)</option>
              <option value="Trần Bảo An (PH: Lê Thu Hương)">Trần Bảo An (PH: Lê Thu Hương)</option>
              <option value="Lê Hoàng Phúc (PH: Lê Hoàng Nam)">Lê Hoàng Phúc (PH: Lê Hoàng Nam)</option>
            </select>
          </div>

          <div>
            <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Tiêu đề thông báo</label>
            <input
              type="text"
              v-model="newNotice.title"
              maxlength="200"
              placeholder="VD: Nhắc nhở hoàn thành bài tập về nhà tuần 44"
              class="w-full h-10 rounded-md bg-stone-50/70 border border-stone-300 px-3 text-stone-900 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
            />
            <p class="text-[11px] text-stone-400 mt-1 text-right tabular-nums">{{ newNotice.title.length }}/200 ký tự</p>
          </div>

          <div>
            <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Nội dung tin nhắn</label>
            <textarea
              v-model="newNotice.content"
              rows="4"
              maxlength="2000"
              placeholder="Nhập nội dung cần gửi đến phụ huynh..."
              class="w-full rounded-md bg-stone-50/70 border border-stone-300 p-3 text-stone-900 focus:bg-white focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 outline-none"
            ></textarea>
            <p class="text-[11px] text-stone-400 mt-1 text-right tabular-nums">{{ newNotice.content.length }}/2000 ký tự</p>
          </div>

          <div>
            <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Kênh gửi</label>
            <div class="flex items-center gap-4 text-stone-700">
              <label class="flex items-center gap-1.5 cursor-pointer">
                <input type="checkbox" checked class="rounded text-orange-600 focus:ring-orange-500" />
                App Phụ huynh
              </label>
              <label class="flex items-center gap-1.5 cursor-pointer">
                <input type="checkbox" checked class="rounded text-orange-600 focus:ring-orange-500" />
                Tin nhắn SMS
              </label>
            </div>
          </div>
        </div>

        <div class="px-6 py-3.5 bg-stone-50 border-t border-stone-200 flex items-center justify-end gap-2">
          <button
            type="button"
            @click="showComposeModal = false"
            class="h-9 px-3.5 rounded-md text-xs font-semibold bg-white border border-stone-300 text-stone-800 hover:bg-stone-100 hover:border-stone-400 transition-colors cursor-pointer"
          >
            Hủy
          </button>
          <button
            type="button"
            @click="sendNotice"
            class="h-9 px-4 rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 text-xs font-semibold shadow-xs transition-colors cursor-pointer"
          >
            Gửi thông báo ngay
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  PaperAirplaneIcon,
  EnvelopeIcon,
  UserGroupIcon,
  UserIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import NotificationInboxPanel from '@/components/notification/NotificationInboxPanel.vue'
import { useNotificationState } from '@/services/notification-state'
import { validateTextLength } from '@/utils/validation.helper'

const route = useRoute()
const router = useRouter()
const { unreadCount } = useNotificationState()

const activeTab = ref(route.query.tab === 'inbox' || route.query.id ? 'inbox' : 'sent')

function switchTab(tab) {
  activeTab.value = tab
  const nextQuery = tab === 'inbox' ? { tab: 'inbox' } : {}
  router.replace({ query: nextQuery })
}

watch(
  () => [route.query.tab, route.query.id],
  ([tab, id]) => {
    if (tab === 'inbox' || id) {
      activeTab.value = 'inbox'
    } else if (tab === 'sent' || tab === 'center') {
      activeTab.value = tab
    }
  }
)
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
  const titleCheck = validateTextLength(newNotice.value.title, {
    label: 'Tiêu đề thông báo',
    required: true,
    max: 200,
  })
  if (!titleCheck.valid) {
    alert(titleCheck.message)
    return
  }

  const contentCheck = validateTextLength(newNotice.value.content, {
    label: 'Nội dung tin nhắn',
    required: true,
    max: 2000,
  })
  if (!contentCheck.valid) {
    alert(contentCheck.message)
    return
  }

  sentList.value.unshift({
    id: Date.now(),
    title: newNotice.value.title.trim(),
    recipientType: newNotice.value.recipientType,
    recipientName: newNotice.value.recipientType === 'class' ? 'Lớp Toán Tư Duy 3A' : newNotice.value.studentName,
    content: newNotice.value.content.trim(),
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
