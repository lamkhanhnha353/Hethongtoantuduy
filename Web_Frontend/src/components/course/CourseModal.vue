<template>
  <transition name="modal-fade">
    <div
      v-if="show"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs overflow-y-auto"
      @click.self="handleClose"
    >
      <div
        class="bg-white rounded-2xl shadow-xl border border-orange-100 w-full max-w-2xl overflow-hidden transform transition-all my-8"
        role="dialog"
      >
        <!-- ─── Header ──────────────────────────────────────────────────────── -->
        <div class="px-6 py-4 border-b border-orange-100 flex items-center justify-between bg-gradient-to-r from-orange-50 to-amber-50">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-xl bg-orange-500 text-white flex items-center justify-center shadow-xs">
              <BookOpenIcon class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-base font-bold text-orange-950">
                <template v-if="isEdit">Chỉnh sửa Khóa học: {{ form.name || `#${course?.id}` }}</template>
                <template v-else>Thêm Khóa học mới</template>
              </h3>
              <p class="text-xs text-gray-500">
                <template v-if="isEdit">Cập nhật thông tin chi tiết, hình ảnh và quyền lợi khóa học</template>
                <template v-else>Điền thông tin để tạo khóa học toán tư duy mới</template>
              </p>
            </div>
          </div>
          <button
            @click="handleClose"
            class="text-gray-400 hover:text-gray-600 p-1.5 rounded-lg hover:bg-orange-100/60 transition-colors"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- ─── Alert Messages ──────────────────────────────────────────────── -->
        <div v-if="errorMessage" class="mx-6 mt-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
          <ExclamationCircleIcon class="w-4 h-4 text-red-500 flex-shrink-0 mt-0.5" />
          <span class="flex-1 leading-relaxed">{{ errorMessage }}</span>
        </div>

        <div v-if="successMessage" class="mx-6 mt-4 p-3 rounded-xl bg-green-50 border border-green-200 text-green-700 text-xs flex items-center gap-2">
          <CheckCircleIcon class="w-4 h-4 text-green-600 flex-shrink-0" />
          <span class="flex-1 font-medium">{{ successMessage }}</span>
        </div>

        <!-- ─── Form Body ───────────────────────────────────────────────────── -->
        <form @submit.prevent="handleSubmit" class="p-6 space-y-5 max-h-[75vh] overflow-y-auto">
          <!-- Mã khóa học + Trạng thái -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Mã khóa học <span class="text-red-500">*</span>
              </label>
              <input
                v-model.trim="form.code"
                type="text"
                required
                placeholder="VD: MATH-SOROBAN-01"
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-mono font-semibold text-gray-800 uppercase focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              />
              <p class="text-[10px] text-gray-400 mt-1">Mã định danh duy nhất của khóa học</p>
            </div>

            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Trạng thái <span class="text-red-500">*</span>
              </label>
              <select
                v-model="form.status"
                required
                class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-semibold text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
              >
                <option value="ACTIVE">Đang mở (ACTIVE)</option>
                <option value="UPCOMING">Sắp mở (UPCOMING)</option>
                <option value="DRAFT">Bản nháp (DRAFT)</option>
                <option value="INACTIVE">Tạm dừng (INACTIVE)</option>
              </select>
            </div>
          </div>

          <!-- Tên khóa học -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Tên khóa học <span class="text-red-500">*</span>
            </label>
            <input
              v-model.trim="form.name"
              type="text"
              required
              placeholder="VD: Toán Tư Duy Bàn Tính Soroban Cho Trẻ Em"
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
            />
          </div>

          <!-- Độ tuổi -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Độ tuổi tối thiểu <span class="text-red-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model.number="form.minAge"
                  type="number"
                  min="3"
                  max="20"
                  required
                  placeholder="5"
                  class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
                />
                <span class="absolute right-3 top-2.5 text-xs text-gray-400">tuổi</span>
              </div>
            </div>

            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Độ tuổi tối đa <span class="text-red-500">*</span>
              </label>
              <div class="relative">
                <input
                  v-model.number="form.maxAge"
                  type="number"
                  :min="form.minAge || 3"
                  max="25"
                  required
                  placeholder="12"
                  class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden"
                />
                <span class="absolute right-3 top-2.5 text-xs text-gray-400">tuổi</span>
              </div>
            </div>
          </div>

          <!-- Mô tả -->
          <div>
            <label class="block text-xs font-bold text-gray-700 mb-1.5">
              Mô tả chi tiết khóa học
            </label>
            <textarea
              v-model="form.description"
              rows="3"
              placeholder="Giới thiệu mục tiêu đào tạo, đối tượng học viên và phương pháp giảng dạy..."
              class="w-full px-3.5 py-2.5 bg-gray-50 border border-gray-200 rounded-xl text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 transition-all outline-hidden resize-y"
            ></textarea>
          </div>

          <!-- Ảnh bìa & Video giới thiệu (tải trực tiếp lên Cloudinary) -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Ảnh bìa -->
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Ảnh bìa khóa học
              </label>

              <div
                v-if="form.imageUrl"
                class="relative w-full h-28 rounded-xl overflow-hidden border border-orange-100 bg-gray-50 group"
              >
                <img
                  :src="form.imageUrl"
                  alt="Ảnh bìa khóa học"
                  class="w-full h-full object-cover"
                  @error="imagePreviewError = true"
                  @load="imagePreviewError = false"
                />
                <div v-if="imagePreviewError" class="absolute inset-0 flex items-center justify-center bg-gray-100 text-gray-400 text-[10px]">
                  Không tải được ảnh
                </div>
                <div class="absolute inset-0 bg-black/45 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                  <label class="inline-flex items-center gap-1 px-2.5 py-1.5 bg-white/90 hover:bg-white text-gray-700 text-[10px] font-semibold rounded-lg cursor-pointer transition-colors">
                    <ArrowUpTrayIcon class="w-3.5 h-3.5" />
                    Đổi ảnh
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      class="sr-only"
                      :disabled="uploadingCover"
                      @change="handleCoverImageChange"
                    />
                  </label>
                  <button
                    type="button"
                    @click="form.imageUrl = ''; imagePreviewError = false"
                    class="inline-flex items-center gap-1 px-2.5 py-1.5 bg-red-500/90 hover:bg-red-500 text-white text-[10px] font-semibold rounded-lg transition-colors"
                  >
                    <TrashIcon class="w-3.5 h-3.5" />
                    Xóa
                  </button>
                </div>
                <div v-if="uploadingCover" class="absolute inset-0 bg-black/55 flex items-center justify-center gap-2 text-white text-[10px] font-semibold">
                  <svg class="animate-spin h-4 w-4" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  Đang tải lên Cloudinary...
                </div>
              </div>

              <label
                v-else
                class="flex flex-col items-center justify-center w-full h-28 rounded-xl border-2 border-dashed border-orange-200 bg-orange-50/40 hover:bg-orange-50 cursor-pointer transition-colors"
                :class="{ 'opacity-60 pointer-events-none': uploadingCover }"
              >
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  class="sr-only"
                  :disabled="uploadingCover"
                  @change="handleCoverImageChange"
                />
                <ArrowUpTrayIcon class="w-6 h-6 text-orange-400 mb-1" />
                <span class="text-xs font-semibold text-orange-600">
                  {{ uploadingCover ? 'Đang tải lên Cloudinary...' : 'Tải ảnh bìa lên' }}
                </span>
                <span class="text-[10px] text-gray-400 mt-0.5">JPG, PNG, WEBP · tối đa 10MB</span>
              </label>

              <p class="text-[10px] text-gray-400 mt-1">
                Ảnh được tải trực tiếp lên Cloudinary, không cần dán link.
              </p>
            </div>

            <!-- Video giới thiệu -->
            <div>
              <label class="block text-xs font-bold text-gray-700 mb-1.5">
                Video giới thiệu khóa học
              </label>

              <div v-if="form.videoUrl" class="space-y-2">
                <div v-if="isYouTubeUrl(form.videoUrl)" class="w-full h-28 rounded-xl overflow-hidden border border-orange-100 bg-black">
                  <iframe
                    :src="getYouTubeEmbedUrl(form.videoUrl)"
                    class="w-full h-full"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen
                  ></iframe>
                </div>
                <video
                  v-else
                  :src="form.videoUrl"
                  controls
                  class="w-full h-28 rounded-xl border border-orange-100 bg-black object-contain"
                ></video>

                <div class="flex items-center gap-2">
                  <label class="inline-flex items-center gap-1 px-2.5 py-1.5 bg-orange-50 hover:bg-orange-100 text-orange-700 text-[10px] font-semibold rounded-lg cursor-pointer transition-colors">
                    <ArrowUpTrayIcon class="w-3.5 h-3.5" />
                    {{ uploadingVideo ? 'Đang tải...' : 'Đổi video' }}
                    <input
                      type="file"
                      accept="video/mp4,video/quicktime,video/webm,video/x-m4v"
                      class="sr-only"
                      :disabled="uploadingVideo"
                      @change="handleVideoChange"
                    />
                  </label>
                  <button
                    type="button"
                    @click="form.videoUrl = ''"
                    class="inline-flex items-center gap-1 px-2.5 py-1.5 text-red-600 hover:bg-red-50 text-[10px] font-semibold rounded-lg transition-colors"
                  >
                    <TrashIcon class="w-3.5 h-3.5" />
                    Xóa video
                  </button>
                </div>
                <p class="text-[10px] text-gray-400 break-all">{{ form.videoUrl }}</p>
              </div>

              <label
                v-else
                class="flex flex-col items-center justify-center w-full h-28 rounded-xl border-2 border-dashed border-orange-200 bg-orange-50/40 hover:bg-orange-50 cursor-pointer transition-colors"
                :class="{ 'opacity-60 pointer-events-none': uploadingVideo }"
              >
                <input
                  type="file"
                  accept="video/mp4,video/quicktime,video/webm,video/x-m4v"
                  class="sr-only"
                  :disabled="uploadingVideo"
                  @change="handleVideoChange"
                />
                <VideoCameraIcon class="w-6 h-6 text-orange-400 mb-1" />
                <span class="text-xs font-semibold text-orange-600">
                  {{ uploadingVideo ? 'Đang tải lên Cloudinary...' : 'Tải video lên' }}
                </span>
                <span class="text-[10px] text-gray-400 mt-0.5">MP4, MOV, WEBM · tối đa 100MB</span>
              </label>

              <p class="text-[10px] text-gray-400 mt-1">
                Video được tải trực tiếp lên Cloudinary, không cần dán link.
              </p>
            </div>
          </div>

          <!-- Quyền lợi khóa học (Benefits) -->
          <div class="pt-2 border-t border-orange-100">
            <div class="flex items-center justify-between mb-2">
              <label class="text-xs font-bold text-gray-700 flex items-center gap-1.5">
                <SparklesIcon class="w-3.5 h-3.5 text-orange-500" />
                Lợi ích khóa học (Benefits)
              </label>
              <button
                type="button"
                @click="addBenefit"
                class="text-xs text-orange-600 hover:text-orange-700 font-semibold inline-flex items-center gap-1"
              >
                <PlusIcon class="w-3.5 h-3.5" />
                Thêm lợi ích
              </button>
            </div>

            <div v-if="form.benefits.length === 0" class="text-[11px] text-gray-400 italic">
              Chưa có lợi ích nào. Bấm "Thêm lợi ích" để bổ sung điểm nổi bật.
            </div>

            <div v-else class="space-y-2">
              <div
                v-for="(benefit, index) in form.benefits"
                :key="index"
                class="flex items-center gap-2"
              >
                <span class="w-5 h-5 rounded-full bg-orange-100 text-orange-700 text-[10px] font-bold flex items-center justify-center flex-shrink-0">
                  {{ index + 1 }}
                </span>
                <input
                  v-model.trim="form.benefits[index]"
                  type="text"
                  placeholder="VD: Rèn luyện tính toán siêu tốc, phát triển tư duy logic hai bán cầu não..."
                  class="flex-1 px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg text-xs font-medium text-gray-800 focus:bg-white focus:border-orange-500 focus:ring-1 focus:ring-orange-500 outline-hidden"
                />
                <button
                  type="button"
                  @click="removeBenefit(index)"
                  class="text-gray-400 hover:text-red-500 p-1.5 rounded-lg hover:bg-red-50 transition-colors"
                >
                  <TrashIcon class="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>

          <!-- Ảnh cơ sở vật chất (FacilityImages) - tải trực tiếp lên Cloudinary -->
          <div class="pt-2 border-t border-orange-100">
            <div class="flex items-center justify-between mb-2">
              <label class="text-xs font-bold text-gray-700 flex items-center gap-1.5">
                <PhotoIcon class="w-3.5 h-3.5 text-orange-500" />
                Ảnh cơ sở vật chất (FacilityImages)
              </label>
              <label class="text-xs text-orange-600 hover:text-orange-700 font-semibold inline-flex items-center gap-1 cursor-pointer">
                <PlusIcon class="w-3.5 h-3.5" />
                Thêm ảnh
                <input
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp"
                  class="sr-only"
                  :disabled="uploadingFacilityIndex !== -1"
                  @change="handleFacilityImagesChange"
                />
              </label>
            </div>

            <div v-if="form.facilityImages.length === 0" class="text-[11px] text-gray-400 italic">
              Chưa có ảnh cơ sở vật chất nào. Bấm "Thêm ảnh" để tải ảnh lên Cloudinary.
            </div>

            <div v-else class="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <div
                v-for="(imgUrl, index) in form.facilityImages"
                :key="index"
                class="relative h-24 rounded-xl overflow-hidden border border-orange-100 bg-gray-50 group"
              >
                <img
                  v-if="imgUrl"
                  :src="imgUrl"
                  alt="Ảnh cơ sở vật chất"
                  class="w-full h-full object-cover"
                />
                <div v-else class="w-full h-full flex items-center justify-center text-[10px] text-gray-400">
                  Đang tải ảnh...
                </div>

                <div class="absolute inset-0 bg-black/45 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-1.5">
                  <label class="p-1.5 bg-white/90 hover:bg-white text-gray-700 rounded-lg cursor-pointer transition-colors" title="Đổi ảnh">
                    <ArrowUpTrayIcon class="w-3.5 h-3.5" />
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      class="sr-only"
                      :disabled="uploadingFacilityIndex === index"
                      @change="handleFacilityImageReplace($event, index)"
                    />
                  </label>
                  <button
                    type="button"
                    @click="removeFacilityImage(index)"
                    class="p-1.5 bg-red-500/90 hover:bg-red-500 text-white rounded-lg transition-colors"
                    title="Xóa ảnh"
                  >
                    <TrashIcon class="w-3.5 h-3.5" />
                  </button>
                </div>

                <div v-if="uploadingFacilityIndex === index" class="absolute inset-0 bg-black/55 flex items-center justify-center gap-1.5 text-white text-[10px] font-semibold">
                  <svg class="animate-spin h-4 w-4" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  Đang tải...
                </div>
              </div>

              <label
                class="h-24 rounded-xl border-2 border-dashed border-orange-200 bg-orange-50/40 hover:bg-orange-50 cursor-pointer flex flex-col items-center justify-center transition-colors"
                :class="{ 'opacity-60 pointer-events-none': uploadingFacilityIndex !== -1 }"
              >
                <input
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp"
                  class="sr-only"
                  :disabled="uploadingFacilityIndex !== -1"
                  @change="handleFacilityImagesChange"
                />
                <PlusIcon class="w-5 h-5 text-orange-400 mb-0.5" />
                <span class="text-[10px] font-semibold text-orange-600">Thêm ảnh</span>
              </label>
            </div>

            <p class="text-[10px] text-gray-400 mt-1">
              Ảnh được tải trực tiếp lên Cloudinary, tối đa 10MB/ảnh.
            </p>
          </div>

          <!-- ─── Footer Buttons ────────────────────────────────────────────── -->
          <div class="flex items-center justify-between pt-4 border-t border-orange-100">
            <button
              v-if="isEdit"
              type="button"
              @click="confirmDelete"
              :disabled="saving || deleting"
              class="inline-flex items-center gap-1 px-3 py-2 text-xs font-semibold text-red-600 hover:text-red-700 hover:bg-red-50 rounded-xl transition-colors disabled:opacity-50"
            >
              <TrashIcon class="w-4 h-4" />
              <span>Xóa khóa học</span>
            </button>
            <div v-else></div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                @click="handleClose"
                :disabled="saving || deleting"
                class="px-4 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-100 rounded-xl transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                :disabled="saving || deleting || uploadingCover || uploadingVideo || uploadingFacilityIndex !== -1"
                class="inline-flex items-center gap-1.5 px-5 py-2 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-xl shadow-sm transition-colors disabled:opacity-50"
              >
                <svg v-if="saving" class="animate-spin h-3.5 w-3.5 text-white" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                <span>{{ isEdit ? 'Lưu thay đổi' : 'Tạo khóa học' }}</span>
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  </transition>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import {
  BookOpenIcon,
  XMarkIcon,
  PlusIcon,
  TrashIcon,
  SparklesIcon,
  PhotoIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
  ArrowUpTrayIcon,
  VideoCameraIcon,
} from '@heroicons/vue/24/outline'
import courseService from '@/services/course.service'
import { getYouTubeEmbedUrl, isYouTubeUrl } from '@/utils/media.helper'

// ─── Giới hạn upload media (đồng bộ với backend) ────────────────────────────
const MAX_IMAGE_BYTES = 10 * 1024 * 1024
const MAX_VIDEO_BYTES = 100 * 1024 * 1024
const ALLOWED_IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']
const ALLOWED_VIDEO_EXTENSIONS = ['.mp4', '.mov', '.webm', '.m4v']
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const ALLOWED_VIDEO_TYPES = ['video/mp4', 'video/quicktime', 'video/webm', 'video/x-m4v']

const props = defineProps({
  show: { type: Boolean, default: false },
  course: { type: Object, default: null }, // If null -> create mode, else edit mode
})

const emit = defineEmits(['close', 'saved', 'deleted'])

const isEdit = computed(() => Boolean(props.course && props.course.id))

const saving = ref(false)
const deleting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const imagePreviewError = ref(false)
const uploadingCover = ref(false)
const uploadingVideo = ref(false)
const uploadingFacilityIndex = ref(-1)

const form = ref({
  code: '',
  name: '',
  description: '',
  minAge: 5,
  maxAge: 12,
  imageUrl: '',
  videoUrl: '',
  status: 'ACTIVE',
  benefits: [],
  facilityImages: [],
})

function initForm() {
  errorMessage.value = ''
  successMessage.value = ''
  imagePreviewError.value = false
  uploadingCover.value = false
  uploadingVideo.value = false
  uploadingFacilityIndex.value = -1

  if (props.course) {
    let parsedBenefits = []
    if (Array.isArray(props.course.benefits)) {
      parsedBenefits = [...props.course.benefits]
    } else if (typeof props.course.benefits === 'string' && props.course.benefits) {
      try {
        parsedBenefits = JSON.parse(props.course.benefits)
      } catch {
        parsedBenefits = [props.course.benefits]
      }
    }

    let parsedFacilityImages = []
    if (Array.isArray(props.course.facilityImages)) {
      parsedFacilityImages = [...props.course.facilityImages]
    } else if (typeof props.course.facilityImages === 'string' && props.course.facilityImages) {
      try {
        parsedFacilityImages = JSON.parse(props.course.facilityImages)
      } catch {
        parsedFacilityImages = [props.course.facilityImages]
      }
    }

    form.value = {
      code: props.course.code || '',
      name: props.course.name || '',
      description: props.course.description || '',
      minAge: props.course.minAge ?? 5,
      maxAge: props.course.maxAge ?? 12,
      imageUrl: props.course.imageUrl || '',
      videoUrl: props.course.videoUrl || '',
      status: (props.course.status || 'ACTIVE').toUpperCase(),
      benefits: parsedBenefits,
      facilityImages: parsedFacilityImages,
    }
  } else {
    form.value = {
      code: '',
      name: '',
      description: '',
      minAge: 5,
      maxAge: 12,
      imageUrl: '',
      videoUrl: '',
      status: 'ACTIVE',
      benefits: [],
      facilityImages: [],
    }
  }
}

watch(
  () => [props.show, props.course],
  () => {
    if (props.show) {
      initForm()
    }
  },
  { immediate: true }
)

function addBenefit() {
  form.value.benefits.push('')
}

function removeBenefit(index) {
  form.value.benefits.splice(index, 1)
}

function getFileExtension(fileName) {
  const name = (fileName || '').toLowerCase()
  const dot = name.lastIndexOf('.')
  return dot >= 0 ? name.slice(dot) : ''
}

function validateImageFile(file) {
  const extension = getFileExtension(file?.name)
  if (
    !ALLOWED_IMAGE_EXTENSIONS.includes(extension) ||
    (file.type && !ALLOWED_IMAGE_TYPES.includes(file.type))
  ) {
    errorMessage.value = 'Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.'
    return false
  }
  if (file.size > MAX_IMAGE_BYTES) {
    errorMessage.value = 'Ảnh vượt quá giới hạn 10MB.'
    return false
  }
  return true
}

function validateVideoFile(file) {
  const extension = getFileExtension(file?.name)
  const contentTypeAllowed =
    !file.type ||
    file.type.startsWith('video/') ||
    ALLOWED_VIDEO_TYPES.includes(file.type)
  if (!ALLOWED_VIDEO_EXTENSIONS.includes(extension) || !contentTypeAllowed) {
    errorMessage.value = 'Chỉ chấp nhận video MP4, MOV, WEBM hoặc M4V.'
    return false
  }
  if (file.size > MAX_VIDEO_BYTES) {
    errorMessage.value = 'Video vượt quá giới hạn 100MB.'
    return false
  }
  return true
}

function getUploadErrorMessage(err, fallback) {
  return (
    err?.response?.data?.message ||
    err?.response?.data?.detail ||
    err?.message ||
    fallback
  )
}

async function handleCoverImageChange(event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  errorMessage.value = ''
  if (!validateImageFile(file)) return

  uploadingCover.value = true
  try {
    const result = await courseService.uploadMedia(file, 'image')
    if (!result?.url) throw new Error('Máy chủ không trả về URL ảnh.')
    form.value.imageUrl = result.url
    imagePreviewError.value = false
  } catch (err) {
    console.error('Lỗi khi tải ảnh bìa lên Cloudinary:', err)
    errorMessage.value = getUploadErrorMessage(err, 'Tải ảnh bìa lên Cloudinary thất bại.')
  } finally {
    uploadingCover.value = false
  }
}

async function handleVideoChange(event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  errorMessage.value = ''
  if (!validateVideoFile(file)) return

  uploadingVideo.value = true
  try {
    const result = await courseService.uploadMedia(file, 'video')
    if (!result?.url) throw new Error('Máy chủ không trả về URL video.')
    form.value.videoUrl = result.url
  } catch (err) {
    console.error('Lỗi khi tải video lên Cloudinary:', err)
    errorMessage.value = getUploadErrorMessage(err, 'Tải video lên Cloudinary thất bại.')
  } finally {
    uploadingVideo.value = false
  }
}

async function handleFacilityImagesChange(event) {
  const input = event.target
  const files = Array.from(input.files || [])
  input.value = ''
  if (files.length === 0 || uploadingFacilityIndex.value !== -1) return

  errorMessage.value = ''
  for (const file of files) {
    if (!validateImageFile(file)) return
  }

  for (const file of files) {
    const index = form.value.facilityImages.length
    form.value.facilityImages.push('')
    uploadingFacilityIndex.value = index
    try {
      const result = await courseService.uploadMedia(file, 'image')
      if (!result?.url) throw new Error('Máy chủ không trả về URL ảnh.')
      form.value.facilityImages[index] = result.url
    } catch (err) {
      form.value.facilityImages.splice(index, 1)
      console.error('Lỗi khi tải ảnh cơ sở vật chất lên Cloudinary:', err)
      errorMessage.value = getUploadErrorMessage(err, 'Tải ảnh cơ sở vật chất lên Cloudinary thất bại.')
      break
    } finally {
      uploadingFacilityIndex.value = -1
    }
  }
}

async function handleFacilityImageReplace(event, index) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  if (!file || uploadingFacilityIndex.value !== -1) return

  errorMessage.value = ''
  if (!validateImageFile(file)) return

  uploadingFacilityIndex.value = index
  try {
    const result = await courseService.uploadMedia(file, 'image')
    if (!result?.url) throw new Error('Máy chủ không trả về URL ảnh.')
    form.value.facilityImages[index] = result.url
  } catch (err) {
    console.error('Lỗi khi thay ảnh cơ sở vật chất trên Cloudinary:', err)
    errorMessage.value = getUploadErrorMessage(err, 'Thay ảnh cơ sở vật chất thất bại.')
  } finally {
    uploadingFacilityIndex.value = -1
  }
}

function removeFacilityImage(index) {
  if (uploadingFacilityIndex.value !== -1) return
  form.value.facilityImages.splice(index, 1)
}

function isUploadingMedia() {
  return uploadingCover.value || uploadingVideo.value || uploadingFacilityIndex.value !== -1
}

function handleClose() {
  if (saving.value || deleting.value) return
  if (isUploadingMedia()) {
    errorMessage.value = 'Vui lòng chờ quá trình tải media lên Cloudinary hoàn tất.'
    return
  }
  emit('close')
}

async function handleSubmit() {
  errorMessage.value = ''
  successMessage.value = ''

  if (isUploadingMedia()) {
    errorMessage.value = 'Vui lòng chờ quá trình tải media lên Cloudinary hoàn tất.'
    return
  }

  if (!form.value.code?.trim()) {
    errorMessage.value = 'Mã khóa học không được để trống.'
    return
  }
  if (!form.value.name?.trim()) {
    errorMessage.value = 'Tên khóa học không được để trống.'
    return
  }
  if (form.value.minAge > form.value.maxAge) {
    errorMessage.value = 'Độ tuổi tối thiểu không được lớn hơn độ tuổi tối đa.'
    return
  }

  const payload = {
    code: form.value.code.trim().toUpperCase(),
    name: form.value.name.trim(),
    description: form.value.description?.trim() || null,
    minAge: Number(form.value.minAge),
    maxAge: Number(form.value.maxAge),
    imageUrl: form.value.imageUrl?.trim() || null,
    videoUrl: form.value.videoUrl?.trim() || null,
    status: form.value.status,
    benefits: form.value.benefits.filter(b => b && b.trim()).map(b => b.trim()),
    facilityImages: form.value.facilityImages.filter(img => img && img.trim()).map(img => img.trim()),
  }

  saving.value = true
  try {
    let result
    if (isEdit.value) {
      await courseService.update(props.course.id, payload)
      successMessage.value = 'Cập nhật khóa học thành công!'
      result = { ...props.course, ...payload }
    } else {
      result = await courseService.create(payload)
      successMessage.value = 'Tạo mới khóa học thành công!'
    }

    setTimeout(() => {
      emit('saved', result)
      emit('close')
    }, 700)
  } catch (err) {
    console.error('Lỗi khi lưu khóa học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Đã xảy ra lỗi khi lưu thông tin khóa học. Vui lòng kiểm tra lại.'
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!props.course?.id) return
  const confirmMsg = `Bạn có chắc chắn muốn xóa khóa học "${props.course.name}" (Mã: ${props.course.code}) không?\nLưu ý: Chỉ có thể xóa khóa học chưa có cấp độ đào tạo.`
  if (!window.confirm(confirmMsg)) return

  deleting.value = true
  errorMessage.value = ''
  try {
    await courseService.delete(props.course.id)
    successMessage.value = 'Xóa khóa học thành công!'
    setTimeout(() => {
      emit('deleted', props.course.id)
      emit('close')
    }, 700)
  } catch (err) {
    console.error('Lỗi khi xóa khóa học:', err)
    errorMessage.value =
      err?.response?.data?.message ||
      err?.message ||
      'Không thể xóa khóa học (Có thể khóa học đang có cấp độ hoặc dữ liệu liên kết).'
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}
</style>
