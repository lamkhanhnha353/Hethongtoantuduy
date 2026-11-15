<template>
  <div class="h-[calc(100vh-7.5rem)] min-h-[560px] flex flex-col gap-3">
    <!-- ─── Header ───────────────────────────────────────────────────────── -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 flex-shrink-0">
      <div>
        <nav class="flex items-center gap-2 text-xs font-medium text-orange-600 mb-1">
          <span>Giáo viên</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-stone-400" />
          <span>Giao tiếp &amp; Thống kê</span>
          <ChevronRightIcon class="w-3.5 h-3.5 text-stone-400" />
          <span>Chat với phụ huynh</span>
        </nav>
        <h1 class="text-2xl md:text-3xl font-bold text-stone-900 tracking-tight">Chat với phụ huynh</h1>
        <p class="text-xs md:text-sm text-stone-500 mt-0.5">
          Trao đổi trực tiếp với phụ huynh của học sinh trong các lớp bạn phụ trách
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <span
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded text-[11px] font-bold border"
          :class="connectionBadgeClass"
        >
          <span class="w-2 h-2 rounded-full" :class="connectionDotClass" />
          {{ connectionLabel }}
        </span>

        <button
          type="button"
          @click="refreshAll"
          :disabled="isRefreshing"
          class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md bg-white hover:bg-stone-100 border border-stone-300 hover:border-stone-400 text-stone-800 text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
        >
          <ArrowPathIcon class="w-4 h-4 text-stone-600" :class="{ 'animate-spin': isRefreshing }" />
          <span>Làm mới</span>
        </button>
      </div>
    </div>

    <!-- ─── Error banner ─────────────────────────────────────────────────── -->
    <div
      v-if="errorMessage"
      class="flex items-start gap-2 px-4 py-3 rounded-lg bg-rose-50 border border-rose-300 text-rose-800 text-xs font-medium flex-shrink-0"
    >
      <ExclamationTriangleIcon class="w-4 h-4 flex-shrink-0 mt-0.5" />
      <span class="flex-1">{{ errorMessage }}</span>
      <button
        type="button"
        @click="errorMessage = ''"
        class="text-rose-500 hover:text-rose-700 cursor-pointer"
        title="Đóng thông báo"
      >
        <XMarkIcon class="w-4 h-4" />
      </button>
    </div>

    <!-- ─── Chat shell ───────────────────────────────────────────────────── -->
    <div
      class="flex-1 min-h-0 bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden grid lg:grid-cols-[330px_1fr]"
    >
      <!-- ═══ Left: conversations / contacts ═══════════════════════════════ -->
      <aside
        :class="[
          'border-r border-stone-200 flex flex-col min-h-0 bg-stone-50',
          activePartner ? 'hidden lg:flex' : 'flex',
        ]"
      >
        <!-- Tabs -->
        <div class="p-3 pb-2 flex-shrink-0 border-b border-stone-200/80">
          <div class="flex items-center bg-stone-200/70 p-1 rounded-md text-xs font-semibold">
            <button
              type="button"
              @click="activeTab = 'conversations'"
              :class="[
                'flex-1 px-3 py-1.5 rounded transition-all cursor-pointer tabular-nums',
                activeTab === 'conversations' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
              ]"
            >
              Hội thoại ({{ conversations.length }})
            </button>
            <button
              type="button"
              @click="activeTab = 'contacts'"
              :class="[
                'flex-1 px-3 py-1.5 rounded transition-all cursor-pointer tabular-nums',
                activeTab === 'contacts' ? 'bg-orange-600 text-white shadow-xs' : 'text-stone-700 hover:text-stone-900',
              ]"
            >
              Phụ huynh ({{ contacts.length }})
            </button>
          </div>

          <div class="mt-2.5">
            <SearchBar v-model="search" :placeholder="activeTab === 'conversations' ? 'Tìm hội thoại...' : 'Tìm phụ huynh, học sinh...'" />
          </div>
        </div>

        <!-- List -->
        <div class="flex-1 overflow-y-auto min-h-0 px-2 py-2 space-y-1">
          <!-- Loading -->
          <div v-if="isLoadingList" class="py-10 text-center text-orange-600 space-y-2">
            <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto" />
            <p class="text-xs font-semibold">Đang tải dữ liệu...</p>
          </div>

          <!-- Conversations -->
          <template v-else-if="activeTab === 'conversations'">
            <div v-if="filteredConversations.length === 0" class="py-10 px-4 text-center space-y-2">
              <ChatBubbleLeftRightIcon class="w-9 h-9 mx-auto text-stone-300" />
              <p class="text-xs font-semibold text-stone-600">Chưa có hội thoại nào</p>
              <button
                type="button"
                @click="activeTab = 'contacts'"
                class="text-[11px] font-bold text-orange-600 hover:text-orange-700 cursor-pointer"
              >
                Chọn phụ huynh để bắt đầu trò chuyện →
              </button>
            </div>

            <button
              v-for="conversation in filteredConversations"
              :key="conversation.id"
              type="button"
              @click="selectConversation(conversation)"
              :class="[
                'w-full text-left flex items-start gap-2.5 px-2.5 py-2.5 rounded-md transition-colors cursor-pointer',
                activeConversation?.id === conversation.id
                  ? 'bg-white border border-orange-400 shadow-xs'
                  : 'hover:bg-white border border-transparent',
              ]"
            >
              <div class="w-10 h-10 rounded-full bg-orange-100 border border-orange-200 text-orange-900 font-bold flex items-center justify-center text-xs flex-shrink-0 overflow-hidden">
                <img
                  v-if="conversation.partnerAvatar"
                  :src="conversation.partnerAvatar"
                  :alt="conversation.partnerName"
                  class="w-full h-full object-cover"
                />
                <span v-else>{{ getInitials(conversation.partnerName) }}</span>
              </div>

              <div class="min-w-0 flex-1">
                <div class="flex items-center justify-between gap-2">
                  <p class="text-xs font-bold text-stone-900 truncate">{{ conversation.partnerName || 'Phụ huynh' }}</p>
                  <span class="text-[10px] text-stone-400 flex-shrink-0 tabular-nums">{{ formatRelativeTime(conversation.lastMessageAt) }}</span>
                </div>
                <div class="flex items-center justify-between gap-2 mt-0.5">
                  <p class="text-[11px] text-stone-500 truncate">
                    {{ conversation.lastMessage || 'Bắt đầu cuộc trò chuyện' }}
                  </p>
                  <span
                    v-if="conversation.unreadCount > 0"
                    class="min-w-[18px] h-[18px] px-1 rounded-full bg-orange-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0 tabular-nums"
                  >
                    {{ conversation.unreadCount > 9 ? '9+' : conversation.unreadCount }}
                  </span>
                </div>
              </div>
            </button>
          </template>

          <!-- Contacts -->
          <template v-else>
            <div v-if="filteredContacts.length === 0" class="py-10 px-4 text-center space-y-2">
              <UserGroupIcon class="w-9 h-9 mx-auto text-stone-300" />
              <p class="text-xs font-semibold text-stone-600">Không tìm thấy phụ huynh</p>
              <p class="text-[11px] text-stone-400">
                Phụ huynh sẽ xuất hiện khi học sinh của họ được xếp vào lớp bạn phụ trách.
              </p>
            </div>

            <button
              v-for="contact in filteredContacts"
              :key="contact.parentId"
              type="button"
              @click="selectContact(contact)"
              :class="[
                'w-full text-left flex items-start gap-2.5 px-2.5 py-2.5 rounded-md transition-colors cursor-pointer',
                activePartner?.id === contact.parentId
                  ? 'bg-white border border-orange-400 shadow-xs'
                  : 'hover:bg-white border border-transparent',
              ]"
            >
              <div class="w-10 h-10 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-800 font-bold flex items-center justify-center text-xs flex-shrink-0">
                {{ getInitials(contact.parentName) }}
              </div>

              <div class="min-w-0 flex-1">
                <div class="flex items-center justify-between gap-2">
                  <p class="text-xs font-bold text-stone-900 truncate">{{ contact.parentName }}</p>
                  <span
                    v-if="conversationIdOf(contact.parentId)"
                    class="text-[9px] font-bold px-1.5 py-0.5 rounded border border-emerald-300 bg-emerald-50 text-emerald-800 flex-shrink-0"
                  >
                    Đã chat
                  </span>
                </div>
                <p class="text-[11px] text-stone-500 truncate mt-0.5">
                  PH của {{ contact.children.join(', ') || 'học sinh' }}
                </p>
                <p v-if="contact.classNames.length" class="text-[10px] text-orange-700 font-medium truncate">
                  {{ contact.classNames.join(' • ') }}
                </p>
              </div>
            </button>
          </template>
        </div>
      </aside>

      <!-- ═══ Right: message thread ════════════════════════════════════════ -->
      <section
        :class="['flex flex-col min-h-0', activePartner ? 'flex' : 'hidden lg:flex']"
      >
        <template v-if="activePartner">
          <!-- Thread header -->
          <div class="px-4 py-3 border-b border-stone-200 flex items-center gap-3 flex-shrink-0 bg-white">
            <button
              type="button"
              @click="clearActiveChat"
              class="lg:hidden p-1.5 -ml-1 rounded-md text-stone-700 hover:bg-stone-100 transition-colors cursor-pointer"
              title="Quay lại danh sách"
            >
              <ArrowLeftIcon class="w-5 h-5" />
            </button>

            <div class="w-10 h-10 rounded-full bg-orange-100 border border-orange-200 text-orange-900 font-bold flex items-center justify-center text-xs flex-shrink-0 overflow-hidden">
              <img
                v-if="activePartner.avatar"
                :src="activePartner.avatar"
                :alt="activePartner.name"
                class="w-full h-full object-cover"
              />
              <span v-else>{{ getInitials(activePartner.name) }}</span>
            </div>

            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2 flex-wrap">
                <h2 class="text-sm font-bold text-stone-900 truncate">{{ activePartner.name }}</h2>
                <span
                  v-if="!activeConversation"
                  class="text-[9px] font-bold px-1.5 py-0.5 rounded border border-amber-300 bg-amber-50 text-amber-800"
                >
                  Hội thoại mới
                </span>
              </div>
              <p v-if="activePartner.children.length" class="text-[11px] text-stone-500 truncate">
                PH của {{ activePartner.children.join(', ') }}
                <span v-if="activePartner.classNames.length"> • {{ activePartner.classNames.join(' • ') }}</span>
              </p>
              <p v-else-if="activePartner.phone" class="text-[11px] text-stone-500 tabular-nums">{{ activePartner.phone }}</p>
            </div>

            <div class="flex items-center gap-1.5 flex-shrink-0">
              <a
                v-if="activePartner.phone"
                :href="`tel:${activePartner.phone}`"
                class="p-2 rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-emerald-50 hover:text-emerald-700 hover:border-emerald-300 transition-colors"
                :title="`Gọi ${activePartner.phone}`"
              >
                <PhoneIcon class="w-4 h-4" />
              </a>
              <a
                v-if="activePartner.email"
                :href="`mailto:${activePartner.email}`"
                class="p-2 rounded-md border border-stone-300 bg-white text-stone-700 hover:bg-stone-100 hover:border-stone-400 transition-colors"
                :title="activePartner.email"
              >
                <EnvelopeIcon class="w-4 h-4" />
              </a>
            </div>
          </div>

          <!-- Messages -->
          <div ref="messagesContainer" class="flex-1 overflow-y-auto min-h-0 px-4 py-4 space-y-3 bg-stone-50/70">
            <!-- Load older -->
            <div v-if="hasMore" class="text-center">
              <button
                type="button"
                @click="loadOlderMessages"
                :disabled="isLoadingMore"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-white border border-stone-300 hover:border-stone-400 text-[11px] font-semibold text-stone-700 hover:bg-stone-100 transition-colors cursor-pointer disabled:opacity-50"
              >
                <ArrowPathIcon v-if="isLoadingMore" class="w-3.5 h-3.5 animate-spin" />
                <ChevronDoubleUpIcon v-else class="w-3.5 h-3.5" />
                {{ isLoadingMore ? 'Đang tải...' : 'Tải tin nhắn cũ hơn' }}
              </button>
            </div>

            <!-- Loading -->
            <div v-if="isLoadingMessages" class="py-12 text-center text-orange-600 space-y-2">
              <ArrowPathIcon class="w-6 h-6 animate-spin mx-auto" />
              <p class="text-xs font-semibold">Đang tải tin nhắn...</p>
            </div>

            <!-- Empty -->
            <div v-else-if="messages.length === 0" class="py-12 text-center space-y-2">
              <ChatBubbleLeftRightIcon class="w-10 h-10 mx-auto text-stone-300" />
              <p class="text-sm font-bold text-stone-700">Chưa có tin nhắn nào</p>
              <p class="text-xs text-stone-500">
                Gửi lời chào đầu tiên để bắt đầu trao đổi với phụ huynh.
              </p>
            </div>

            <!-- Message list -->
            <template v-else>
              <div v-for="(message, index) in messages" :key="message.id">
                <div v-if="showDayDivider(index)" class="flex justify-center my-3">
                  <span class="px-2.5 py-0.5 rounded bg-white border border-stone-300 text-[10px] font-semibold text-stone-600 shadow-2xs tabular-nums">
                    {{ formatDay(message.createdAt) }}
                  </span>
                </div>

                <div :class="['flex gap-2 group', isMine(message) ? 'justify-end' : 'justify-start']">
                  <div
                    v-if="!isMine(message)"
                    class="w-7 h-7 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-800 font-bold flex items-center justify-center text-[10px] flex-shrink-0 mt-1"
                  >
                    {{ getInitials(activePartner.name) }}
                  </div>

                  <div class="max-w-[80%] sm:max-w-[70%] min-w-0">
                    <div
                      :class="[
                        'px-3.5 py-2.5 text-sm shadow-2xs',
                        isMine(message)
                          ? 'bg-orange-600 text-white rounded-lg rounded-br-sm'
                          : 'bg-white text-stone-800 border border-stone-300 rounded-lg rounded-bl-sm',
                      ]"
                    >
                      <p
                        v-if="message.messageType === 'TEXT'"
                        class="whitespace-pre-wrap break-words leading-relaxed"
                      >
                        {{ message.content }}
                      </p>

                      <a
                        v-else-if="message.messageType === 'IMAGE' && message.attachmentUrl"
                        :href="message.attachmentUrl"
                        target="_blank"
                        rel="noopener"
                        class="block"
                      >
                        <img
                          :src="message.attachmentUrl"
                          :alt="message.content || 'Hình ảnh đính kèm'"
                          class="max-h-64 max-w-full rounded-md object-cover"
                        />
                      </a>

                      <a
                        v-else-if="message.attachmentUrl"
                        :href="message.attachmentUrl"
                        target="_blank"
                        rel="noopener"
                        class="flex items-center gap-2 underline underline-offset-2 break-all"
                      >
                        <DocumentIcon class="w-4 h-4 flex-shrink-0" />
                        <span>{{ message.content || 'Tệp đính kèm' }}</span>
                      </a>

                      <p v-else class="italic opacity-80">Tin nhắn không hỗ trợ hiển thị.</p>
                    </div>

                    <div
                      :class="[
                        'flex items-center gap-2 mt-1 text-[10px] text-stone-400 tabular-nums',
                        isMine(message) ? 'justify-end' : 'justify-start',
                      ]"
                    >
                      <span>{{ formatMessageTime(message.createdAt) }}</span>
                      <span v-if="isMine(message)" class="font-semibold text-orange-600">Đã gửi</span>
                      <button
                        v-if="isMine(message)"
                        type="button"
                        @click="askDelete(message)"
                        class="opacity-0 group-hover:opacity-100 text-stone-400 hover:text-rose-600 transition-all cursor-pointer"
                        title="Xóa tin nhắn"
                      >
                        <TrashIcon class="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </template>
          </div>

          <!-- Composer -->
          <div class="border-t border-stone-200 p-3 bg-white flex-shrink-0">
            <div
              v-if="isUploading"
              class="mb-2 flex items-center gap-2 text-[11px] font-semibold text-orange-600"
            >
              <ArrowPathIcon class="w-3.5 h-3.5 animate-spin" />
              Đang tải tệp lên máy chủ...
            </div>

            <div class="flex items-end gap-2">
              <button
                type="button"
                @click="fileInput?.click()"
                :disabled="isSending || isUploading"
                class="h-[44px] w-[44px] flex items-center justify-center rounded-md text-stone-700 bg-white hover:bg-stone-100 border border-stone-300 hover:border-stone-400 transition-colors cursor-pointer disabled:opacity-50"
                title="Gửi tệp hoặc hình ảnh"
              >
                <PaperClipIcon class="w-5 h-5" />
              </button>
              <input
                ref="fileInput"
                type="file"
                class="hidden"
                accept="image/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip"
                @change="handleFileSelected"
              />

              <textarea
                v-model="composerText"
                rows="1"
                maxlength="2000"
                @keydown="onComposerKeydown"
                placeholder="Nhập tin nhắn gửi phụ huynh..."
                class="flex-1 resize-none max-h-32 min-h-[44px] px-3.5 py-2.5 text-sm bg-stone-50/70 border border-stone-300 rounded-md text-stone-900 placeholder:text-stone-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-orange-500/20 focus:border-orange-600 transition-all"
              ></textarea>

              <button
                type="button"
                @click="sendText"
                :disabled="!composerText.trim() || isSending || isUploading"
                class="h-[44px] w-[44px] flex items-center justify-center rounded-md bg-orange-600 hover:bg-orange-700 active:bg-orange-800 border border-orange-700 text-white shadow-xs transition-colors cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed"
                title="Gửi tin nhắn"
              >
                <ArrowPathIcon v-if="isSending" class="w-5 h-5 animate-spin" />
                <PaperAirplaneIcon v-else class="w-5 h-5" />
              </button>
            </div>

            <p class="text-[10px] text-stone-400 mt-1.5 tabular-nums">
              Enter để gửi • Shift + Enter để xuống dòng • Tệp tối đa 20MB
            </p>
          </div>
        </template>

        <!-- Empty state -->
        <div v-else class="flex-1 flex items-center justify-center p-6">
          <EmptyState
            :icon="ChatBubbleLeftRightIcon"
            message="Chọn một phụ huynh để bắt đầu trò chuyện"
            sub="Chọn hội thoại có sẵn hoặc chuyển sang tab Phụ huynh để tìm người cần trao đổi."
          />
        </div>
      </section>
    </div>

    <!-- ─── Delete confirmation ──────────────────────────────────────────── -->
    <div
      v-if="deleteTarget"
      class="fixed inset-0 bg-black/40 backdrop-blur-xs z-50 flex items-center justify-center p-4"
      @click.self="deleteTarget = null"
    >
      <div class="bg-white rounded-lg w-full max-w-sm p-6 shadow-2xl border border-stone-300 space-y-4">
        <div class="flex items-start gap-3">
          <div class="w-10 h-10 rounded-md bg-rose-50 border border-rose-200 text-rose-700 flex items-center justify-center flex-shrink-0">
            <TrashIcon class="w-5 h-5" />
          </div>
          <div>
            <h3 class="text-base font-bold text-stone-900">Xóa tin nhắn?</h3>
            <p class="text-xs text-stone-500 mt-1">
              Tin nhắn sẽ bị xóa vĩnh viễn ở cả hai phía và không thể khôi phục.
            </p>
          </div>
        </div>

        <p
          v-if="deleteTarget.content"
          class="text-xs text-stone-700 bg-stone-50 border border-stone-200 rounded-md p-3 line-clamp-3 break-words"
        >
          {{ deleteTarget.content }}
        </p>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-stone-200">
          <button
            type="button"
            @click="deleteTarget = null"
            :disabled="isDeleting"
            class="h-9 px-3.5 rounded-md text-xs font-semibold bg-white border border-stone-300 hover:border-stone-400 text-stone-800 hover:bg-stone-100 transition-colors cursor-pointer disabled:opacity-50"
          >
            Hủy
          </button>
          <button
            type="button"
            @click="confirmDelete"
            :disabled="isDeleting"
            class="inline-flex items-center gap-1.5 h-9 px-3.5 rounded-md text-xs font-semibold bg-rose-600 hover:bg-rose-700 border border-rose-700 text-white shadow-xs transition-colors cursor-pointer disabled:opacity-50"
          >
            <ArrowPathIcon v-if="isDeleting" class="w-3.5 h-3.5 animate-spin" />
            Xóa tin nhắn
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import {
  ArrowLeftIcon,
  ArrowPathIcon,
  ChatBubbleLeftRightIcon,
  ChevronDoubleUpIcon,
  ChevronRightIcon,
  DocumentIcon,
  EnvelopeIcon,
  ExclamationTriangleIcon,
  PaperAirplaneIcon,
  PaperClipIcon,
  PhoneIcon,
  TrashIcon,
  UserGroupIcon,
  XMarkIcon,
} from '@heroicons/vue/24/outline'
import SearchBar from '@/components/common/SearchBar.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import chatService from '@/services/chat.service'
import classService from '@/services/class.service'
import teacherService from '@/services/teacher.service'
import { getProfileId } from '@/services/auth-storage'
import { useUserState } from '@/services/user-state'

const PAGE_SIZE = 30
const MAX_FILE_SIZE = 20 * 1024 * 1024
const PARTNER_TYPE = 'PARENT'

const route = useRoute()
const { fetchProfile } = useUserState()

// ─── State ───────────────────────────────────────────────────────────────────
const conversations = ref([])
const contacts = ref([])
const messages = ref([])

const activeTab = ref('conversations')
const search = ref('')

const activeConversation = ref(null)
const activeContact = ref(null)

const composerText = ref('')
const fileInput = ref(null)
const deleteTarget = ref(null)

const isLoadingConversations = ref(false)
const isLoadingContacts = ref(false)
const isLoadingMessages = ref(false)
const isLoadingMore = ref(false)
const isSending = ref(false)
const isUploading = ref(false)
const isDeleting = ref(false)
const isRefreshing = ref(false)

const messagePage = ref(1)
const hasMore = ref(false)
const errorMessage = ref('')

const connectionStatus = ref('connecting')
const messagesContainer = ref(null)

// ─── Computed ────────────────────────────────────────────────────────────────
const isLoadingList = computed(() => isLoadingConversations.value || isLoadingContacts.value)

const contactMap = computed(() => {
  const map = new Map()
  contacts.value.forEach((contact) => map.set(contact.parentId, contact))
  return map
})

const activePartner = computed(() => {
  if (activeConversation.value) {
    const conversation = activeConversation.value
    const contact = contactMap.value.get(conversation.partnerId)
    return {
      id: conversation.partnerId,
      name: conversation.partnerName || contact?.parentName || 'Phụ huynh',
      avatar: conversation.partnerAvatar || null,
      phone: contact?.parentPhone || '',
      email: contact?.parentEmail || '',
      children: contact?.children || [],
      classNames: contact?.classNames || [],
    }
  }

  if (activeContact.value) {
    const contact = activeContact.value
    return {
      id: contact.parentId,
      name: contact.parentName,
      avatar: null,
      phone: contact.parentPhone || '',
      email: contact.parentEmail || '',
      children: contact.children || [],
      classNames: contact.classNames || [],
    }
  }

  return null
})

const filteredConversations = computed(() => {
  const keyword = search.value.trim().toLowerCase()
  if (!keyword) return conversations.value
  return conversations.value.filter((conversation) => {
    return (
      String(conversation.partnerName || '').toLowerCase().includes(keyword) ||
      String(conversation.lastMessage || '').toLowerCase().includes(keyword)
    )
  })
})

const filteredContacts = computed(() => {
  const keyword = search.value.trim().toLowerCase()
  if (!keyword) return contacts.value
  return contacts.value.filter((contact) => {
    const haystack = [
      contact.parentName,
      contact.parentPhone,
      contact.parentEmail,
      ...(contact.children || []),
      ...(contact.classNames || []),
    ]
      .join(' ')
      .toLowerCase()
    return haystack.includes(keyword)
  })
})

const connectionLabel = computed(() => {
  switch (connectionStatus.value) {
    case 'connected':
      return 'Realtime đã kết nối'
    case 'reconnecting':
      return 'Đang kết nối lại...'
    case 'connecting':
      return 'Đang kết nối...'
    default:
      return 'Realtime ngoại tuyến'
  }
})

const connectionBadgeClass = computed(() => {
  switch (connectionStatus.value) {
    case 'connected':
      return 'bg-emerald-50 border-emerald-300 text-emerald-800'
    case 'reconnecting':
    case 'connecting':
      return 'bg-amber-50 border-amber-300 text-amber-800'
    default:
      return 'bg-stone-100 border-stone-300 text-stone-600'
  }
})

const connectionDotClass = computed(() => {
  switch (connectionStatus.value) {
    case 'connected':
      return 'bg-emerald-500'
    case 'reconnecting':
    case 'connecting':
      return 'bg-amber-500 animate-pulse'
    default:
      return 'bg-stone-400'
  }
})

// ─── Lifecycle ───────────────────────────────────────────────────────────────
onMounted(async () => {
  connectRealtime()
  await Promise.all([loadConversations(), loadContacts()])
  applyRouteTarget()
})

onUnmounted(() => {
  chatService.disconnect()
})

// ─── Data loading ────────────────────────────────────────────────────────────
async function loadConversations({ silent = false } = {}) {
  if (!silent) isLoadingConversations.value = true
  try {
    const items = await chatService.getConversations()
    conversations.value = items
      .map((conversation) => ({
        ...conversation,
        unreadCount: Number(conversation.unreadCount) || 0,
      }))
      .sort((a, b) => toTimestamp(b.lastMessageAt) - toTimestamp(a.lastMessageAt))

    if (activeConversation.value) {
      const fresh = conversations.value.find((item) => item.id === activeConversation.value.id)
      if (fresh) activeConversation.value = fresh
    }
  } catch (error) {
    if (!silent) errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách hội thoại.')
  } finally {
    if (!silent) isLoadingConversations.value = false
  }
}

async function loadContacts() {
  isLoadingContacts.value = true
  try {
    const userProfile = await fetchProfile()
    let teacherProfileId = getProfileId()
    const role = (userProfile?.role || '').toLowerCase()
    const teacherFullName = (userProfile?.fullName || '').trim().toLowerCase()
    const teacherPhone = (userProfile?.phone || '').trim()

    if (!teacherProfileId && role === 'teacher') {
      try {
        const teachersData = await teacherService.getAll()
        const rawTeachers = Array.isArray(teachersData)
          ? teachersData
          : teachersData?.items || teachersData?.data || []
        const matched = rawTeachers.find(
          (teacher) =>
            (teacherPhone && teacher.phone === teacherPhone) ||
            (teacherFullName &&
              teacher.fullName &&
              teacher.fullName.trim().toLowerCase() === teacherFullName) ||
            (userProfile?.accountId && teacher.accountId === userProfile.accountId),
        )
        if (matched) teacherProfileId = matched.id
      } catch (error) {
        console.warn('Không thể tra cứu hồ sơ giáo viên:', error)
      }
    }

    const assigned = await classService.getAssignedClasses(userProfile, teacherProfileId)
    const classList = Array.isArray(assigned) ? assigned : []
    const results = await Promise.allSettled(
      classList.map((classroom) => classService.getStudents(classroom.id)),
    )

    const contactMap = new Map()
    results.forEach((result, index) => {
      if (result.status !== 'fulfilled') return
      const classroom = classList[index]
      const raw = result.value
      const students = Array.isArray(raw) ? raw : raw?.items || []

      students.forEach((student) => {
        if (!student.parentId) return
        if (!contactMap.has(student.parentId)) {
          contactMap.set(student.parentId, {
            parentId: student.parentId,
            parentName: student.parentName || `Phụ huynh #${student.parentId}`,
            parentPhone: student.parentPhone || '',
            parentEmail: student.parentEmail || '',
            children: [],
            classNames: [],
          })
        }

        const contact = contactMap.get(student.parentId)
        if (student.studentName) contact.children.push(student.studentName)
        if (classroom?.name) contact.classNames.push(classroom.name)
        if (!contact.parentPhone && student.parentPhone) contact.parentPhone = student.parentPhone
        if (!contact.parentEmail && student.parentEmail) contact.parentEmail = student.parentEmail
      })
    })

    contacts.value = Array.from(contactMap.values())
      .map((contact) => ({
        ...contact,
        children: Array.from(new Set(contact.children)),
        classNames: Array.from(new Set(contact.classNames)),
      }))
      .sort((a, b) => a.parentName.localeCompare(b.parentName, 'vi'))
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách phụ huynh.')
  } finally {
    isLoadingContacts.value = false
  }
}

async function refreshAll() {
  isRefreshing.value = true
  errorMessage.value = ''
  try {
    await Promise.all([loadConversations(), loadContacts()])
    if (activeConversation.value) {
      await loadMessages(activeConversation.value.id)
    }
    if (connectionStatus.value === 'disconnected') {
      connectRealtime()
    }
  } finally {
    isRefreshing.value = false
  }
}

function applyRouteTarget() {
  const parentId = Number.parseInt(route.query.parentId, 10)
  if (Number.isNaN(parentId)) return

  const conversation = conversations.value.find((item) => item.partnerId === parentId)
  if (conversation) {
    selectConversation(conversation)
    return
  }

  const contact = contacts.value.find((item) => item.parentId === parentId)
  if (contact) selectContact(contact)
}

// ─── Selection ───────────────────────────────────────────────────────────────
function selectConversation(conversation) {
  activeConversation.value = conversation
  activeContact.value = null
  conversation.unreadCount = 0
  messages.value = []
  messagePage.value = 1
  hasMore.value = false
  loadMessages(conversation.id)
}

function selectContact(contact) {
  const existing = conversations.value.find((item) => item.partnerId === contact.parentId)
  if (existing) {
    selectConversation(existing)
    return
  }

  activeConversation.value = null
  activeContact.value = contact
  messages.value = []
  messagePage.value = 1
  hasMore.value = false
  errorMessage.value = ''
}

function clearActiveChat() {
  activeConversation.value = null
  activeContact.value = null
  messages.value = []
  hasMore.value = false
}

function conversationIdOf(parentId) {
  return conversations.value.find((item) => item.partnerId === parentId)?.id || null
}

// ─── Message loading ─────────────────────────────────────────────────────────
async function loadMessages(conversationId) {
  isLoadingMessages.value = true
  errorMessage.value = ''
  try {
    const items = await chatService.getMessages(conversationId, 1)
    messages.value = items
    messagePage.value = 1
    hasMore.value = items.length >= PAGE_SIZE
    await scrollToBottom()
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải tin nhắn của hội thoại.')
  } finally {
    isLoadingMessages.value = false
  }
}

async function loadOlderMessages() {
  if (!activeConversation.value || isLoadingMore.value || !hasMore.value) return
  isLoadingMore.value = true
  const nextPage = messagePage.value + 1
  try {
    const items = await chatService.getMessages(activeConversation.value.id, nextPage)
    const existingIds = new Set(messages.value.map((message) => message.id))
    const older = items.filter((message) => !existingIds.has(message.id))
    messages.value = [...older, ...messages.value]
    messagePage.value = nextPage
    hasMore.value = items.length >= PAGE_SIZE
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải thêm tin nhắn cũ.')
  } finally {
    isLoadingMore.value = false
  }
}

// ─── Sending ─────────────────────────────────────────────────────────────────
async function sendText() {
  const content = composerText.value.trim()
  const partnerId = activePartner.value?.id
  if (!content || !partnerId || isSending.value) return

  if (content.length > 2000) {
    errorMessage.value = 'Tin nhắn không được vượt quá 2000 ký tự.'
    return
  }

  isSending.value = true
  errorMessage.value = ''
  try {
    const message = await chatService.sendMessage({
      receiverId: partnerId,
      receiverType: PARTNER_TYPE,
      messageType: 'TEXT',
      content,
    })
    composerText.value = ''
    await handleOutgoingMessage(message)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể gửi tin nhắn. Vui lòng thử lại.')
  } finally {
    isSending.value = false
  }
}

async function handleFileSelected(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return

  if (file.size > MAX_FILE_SIZE) {
    errorMessage.value = 'Tệp vượt quá dung lượng tối đa 20MB.'
    return
  }

  const partnerId = activePartner.value?.id
  if (!partnerId || isUploading.value) return

  isUploading.value = true
  errorMessage.value = ''
  try {
    const message = await chatService.sendAttachment({
      receiverId: partnerId,
      receiverType: PARTNER_TYPE,
      file,
    })
    await handleOutgoingMessage(message)
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể gửi tệp đính kèm. Vui lòng thử lại.')
  } finally {
    isUploading.value = false
  }
}

async function handleOutgoingMessage(message) {
  if (!message) return

  if (!activeConversation.value && message.conversationId) {
    await loadConversations({ silent: true })
    const conversation = conversations.value.find((item) => item.id === message.conversationId)
    if (conversation) {
      conversation.unreadCount = 0
      activeConversation.value = conversation
      activeContact.value = null
    }
  }

  appendMessage(message)
}

function appendMessage(message) {
  if (!message) return
  if (!messages.value.some((item) => item.id === message.id)) {
    messages.value.push(message)
  }
  updateConversationPreview(message)
  scrollToBottom(true)
}

function updateConversationPreview(message) {
  const conversation = conversations.value.find((item) => item.id === message.conversationId)
  if (!conversation) return
  conversation.lastMessage = buildPreview(message)
  conversation.lastMessageAt = message.createdAt
  conversations.value.sort(
    (a, b) => toTimestamp(b.lastMessageAt) - toTimestamp(a.lastMessageAt),
  )
}

// ─── Delete ──────────────────────────────────────────────────────────────────
function askDelete(message) {
  deleteTarget.value = message
}

async function confirmDelete() {
  const message = deleteTarget.value
  const conversation = activeConversation.value
  if (!message || !conversation || isDeleting.value) return

  isDeleting.value = true
  errorMessage.value = ''
  try {
    await chatService.deleteMessage(conversation.id, message.id)
    messages.value = messages.value.filter((item) => item.id !== message.id)
    deleteTarget.value = null
    await loadConversations({ silent: true })
  } catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể xóa tin nhắn. Vui lòng thử lại.')
  } finally {
    isDeleting.value = false
  }
}

// ─── Realtime ────────────────────────────────────────────────────────────────
async function connectRealtime() {
  connectionStatus.value = 'connecting'
  try {
    await chatService.connect({
      onNewMessage: handleNewMessage,
      onMessageDeleted: handleMessageDeleted,
      onStatusChange: (status) => {
        connectionStatus.value = status
      },
    })
  } catch (error) {
    console.warn('Không thể kết nối SignalR cho chat:', error)
    connectionStatus.value = 'disconnected'
  }
}

function handleNewMessage(message) {
  if (!message?.conversationId) return

  const isCurrentConversation = activeConversation.value?.id === message.conversationId

  if (isCurrentConversation && !messages.value.some((item) => item.id === message.id)) {
    messages.value.push(message)
    scrollToBottom(true)
  }

  const conversation = conversations.value.find((item) => item.id === message.conversationId)
  if (conversation) {
    conversation.lastMessage = buildPreview(message)
    conversation.lastMessageAt = message.createdAt
    if (!isCurrentConversation && message.senderType === 'PARENT') {
      conversation.unreadCount = (conversation.unreadCount || 0) + 1
    }
    conversations.value.sort(
      (a, b) => toTimestamp(b.lastMessageAt) - toTimestamp(a.lastMessageAt),
    )
  } else {
    loadConversations({ silent: true })
  }
}

function handleMessageDeleted(conversationId, messageId) {
  if (activeConversation.value?.id === conversationId) {
    messages.value = messages.value.filter((message) => message.id !== messageId)
  }
  loadConversations({ silent: true })
}

// ─── Helpers ─────────────────────────────────────────────────────────────────
function isMine(message) {
  return String(message?.senderType || '').toUpperCase() === 'TEACHER'
}

function buildPreview(message) {
  if (!message) return ''
  if (message.messageType === 'TEXT') return message.content || ''
  if (message.messageType === 'IMAGE') return '[Hình ảnh]'
  return `[Tệp] ${message.content || ''}`
}

function getInitials(name) {
  if (!name) return 'PH'
  const parts = String(name).trim().split(/\s+/)
  if (parts.length >= 2) {
    return (parts[parts.length - 2][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return String(name).slice(0, 2).toUpperCase()
}

function parseServerDate(value) {
  if (!value) return null
  const raw = String(value)
  const normalized = /(?:z|[+-]\d{2}:?\d{2})$/i.test(raw) ? raw : `${raw}Z`
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? null : date
}

function toTimestamp(value) {
  return parseServerDate(value)?.getTime() ?? 0
}

function formatMessageTime(value) {
  const date = parseServerDate(value)
  if (!date) return ''
  return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

function formatRelativeTime(value) {
  const date = parseServerDate(value)
  if (!date) return ''

  const now = new Date()
  const sameDay = date.toDateString() === now.toDateString()
  if (sameDay) return formatMessageTime(value)

  const yesterday = new Date(now)
  yesterday.setDate(now.getDate() - 1)
  if (date.toDateString() === yesterday.toDateString()) return 'Hôm qua'

  return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}

function formatDay(value) {
  const date = parseServerDate(value)
  if (!date) return ''
  return date.toLocaleDateString('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function showDayDivider(index) {
  if (index === 0) return true
  const current = parseServerDate(messages.value[index]?.createdAt)
  const previous = parseServerDate(messages.value[index - 1]?.createdAt)
  if (!current || !previous) return false
  return current.toDateString() !== previous.toDateString()
}

function getErrorMessage(error, fallback) {
  const data = error?.response?.data
  if (typeof data === 'string' && data.trim()) return data
  return data?.message || data?.title || error?.message || fallback
}

function scrollToBottom(smooth = false) {
  nextTick(() => {
    const container = messagesContainer.value
    if (!container) return
    container.scrollTo({
      top: container.scrollHeight,
      behavior: smooth ? 'smooth' : 'auto',
    })
  })
}

function onComposerKeydown(event) {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    sendText()
  }
}
</script>
