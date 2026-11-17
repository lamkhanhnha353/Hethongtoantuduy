<template>
    <div class="space-y-6">
        <!-- Header Page -->
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
                <h1 class="text-2xl font-bold text-stone-900">Quản lý Giáo viên</h1>
                <p class="mt-1 text-sm text-stone-500">Tra cứu và cập nhật hồ sơ, chuyên môn giáo viên</p>
            </div>
            <button @click="openCreateModal"
                class="inline-flex h-9 items-center justify-center gap-2 rounded-md border border-orange-700 bg-orange-600 px-3.5 text-sm font-semibold text-white shadow-xs transition hover:bg-orange-700">
                <span aria-hidden="true">+</span>
                Thêm giáo viên
            </button>
        </div>

        <!-- Search Bar & Filter -->
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div class="w-full max-w-md">
                <SearchBar v-model="keyword" placeholder="Tìm tên, SĐT hoặc email giáo viên..." />
            </div>
            <select
                v-model="statusFilter"
                aria-label="Lọc trạng thái giáo viên"
                class="h-9 rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20"
            >
                <option value="ALL">Tất cả trạng thái</option>
                <option value="true">Hoạt động</option>
                <option value="false">Đã khóa</option>
            </select>
        </div>

        <!-- Data Table -->
        <div class="overflow-hidden rounded-lg border border-stone-300 bg-white shadow-[0_1px_2px_rgba(0,0,0,0.04)]">
            <table class="w-full text-left text-sm">
                <thead class="border-b-2 border-stone-300 bg-stone-100 text-xs font-bold uppercase tracking-wider text-stone-700">
                    <tr>
                        <th class="px-6 py-3.5">Họ & Tên Giáo viên</th>
                        <th class="px-6 py-3.5">Thông tin liên hệ</th>
                        <th class="px-6 py-3.5">Chuyên môn</th>
                        <th class="px-6 py-3.5">Trạng thái</th>
                        <th class="px-6 py-3.5 text-right">Thao tác</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-stone-200 text-stone-700">
                    <tr v-if="isLoading">
                        <td colspan="5" class="px-6 py-8 text-center text-stone-500">Đang tải dữ liệu...</td>
                    </tr>
                    <tr v-else-if="errorMessage">
                        <td colspan="5" class="px-6 py-8 text-center text-rose-700">{{ errorMessage }}</td>
                    </tr>
                    <tr v-else-if="teachers.length === 0">
                        <td colspan="5" class="px-6 py-8 text-center text-stone-500">Không tìm thấy dữ liệu giáo viên.
                        </td>
                    </tr>
                    <tr v-for="teacher in teachers" :key="teacher.id" class="transition-colors hover:bg-orange-50/50">
                        <td class="px-6 py-4 font-semibold text-stone-900">{{ teacher.fullName }}</td>
                        <td class="px-6 py-4">
                            <div class="font-mono font-medium text-stone-900 tabular-nums">{{ teacher.phone || 'Chưa cập nhật' }}
                            </div>
                            <div class="mt-0.5 text-xs text-stone-500">{{ teacher.email || 'Chưa cập nhật email' }}</div>
                        </td>
                        <td class="px-6 py-4">
                            <span
                                class="inline-block rounded border border-orange-300 bg-orange-50 px-2 py-0.5 text-xs font-semibold text-orange-800">
                                {{ teacher.specialize || 'Toán Tư Duy' }}
                            </span>
                        </td>
                        <td class="px-6 py-4">
                            <span
                                class="inline-flex rounded border px-2 py-0.5 text-xs font-semibold"
                                :class="teacher.isActive
                                    ? 'border-emerald-300 bg-emerald-50 text-emerald-800'
                                    : 'border-rose-300 bg-rose-50 text-rose-800'"
                                :aria-label="`Trạng thái tài khoản của ${teacher.fullName}`">
                                {{ teacher.isActive ? 'Hoạt động' : 'Đã khóa' }}
                            </span>
                        </td>
                        <td class="px-6 py-4 text-right whitespace-nowrap">
                            <button @click="openEditModal(teacher)"
                                class="mr-2 h-8 rounded-md border border-stone-300 bg-white px-2.5 text-xs font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100">
                                Sửa
                            </button>
                            <button @click="deleteTeacher(teacher.id)"
                                class="h-8 rounded-md border border-rose-300 bg-white px-2.5 text-xs font-semibold text-rose-700 transition hover:border-rose-400 hover:bg-rose-50">
                                Xóa
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
            <PaginationBar
                v-model:page="page"
                v-model:page-size="pageSize"
                :total-count="totalCount"
                :total-pages="totalPages"
                :disabled="isLoading"
                item-label="giáo viên"
                @change="fetchTeachers"
            />
        </div>

        <!-- Modal Form Giáo Viên -->
        <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center bg-stone-950/50 p-4">
            <div class="w-full max-w-md overflow-hidden rounded-lg border border-stone-300 bg-white shadow-xl">
                <div class="flex items-center justify-between border-b border-stone-200 bg-stone-50 px-6 py-4">
                    <h3 class="font-bold text-stone-900">{{ isCreating ? 'Thêm Giáo viên mới' : 'Sửa thông tin Giáo viên' }}</h3>
                    <button type="button" @click="closeModal" class="rounded-md p-1 text-stone-400 transition hover:bg-stone-200/70 hover:text-stone-700">&times;</button>
                </div>
                <form @submit.prevent="saveTeacher" class="space-y-4 p-6">
                    <div>
                        <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Họ và tên *</label>
                        <input v-model="form.fullName" required type="text" maxlength="100"
                            class="h-10 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20" />
                    </div>
                    <div>
                        <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Số điện thoại (Tài khoản đăng
                            nhập)</label>
                        <input v-if="!isCreating" :value="form.phone" disabled type="tel"
                            class="h-10 w-full cursor-not-allowed rounded-md border border-stone-300 bg-stone-100 px-3 font-mono text-sm text-stone-500 tabular-nums" />
                        <input v-else v-model.trim="form.phone" required type="tel" autocomplete="tel"
                            inputmode="tel" maxlength="12" pattern="(\+84|0)[0-9]{9}"
                            title="Số điện thoại gồm 10 chữ số (VD: 0912345678) hoặc bắt đầu bằng +84"
                            placeholder="Nhập số điện thoại đăng nhập"
                            class="h-10 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3 font-mono text-sm text-stone-900 tabular-nums outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20" />
                    </div>
                    <div v-if="isCreating">
                        <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Mật khẩu ban đầu *</label>
                        <input v-model="form.password" required minlength="6" maxlength="128" type="password" autocomplete="new-password"
                            placeholder="Ít nhất 6 ký tự"
                            class="h-10 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20" />
                    </div>
                    <div>
                        <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Email</label>
                        <input v-model.trim="form.email" type="email" autocomplete="email" maxlength="256"
                            class="h-10 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20" />
                    </div>
                    <div v-if="!isCreating">
                        <label class="mb-1.5 block text-xs font-semibold uppercase tracking-wide text-stone-700">Chuyên môn / Lĩnh vực giảng
                            dạy</label>
                        <input v-model="form.specialize" type="text" maxlength="100"
                            placeholder="VD: Toán tư duy Soroban, Fingermath..."
                            class="h-10 w-full rounded-md border border-stone-300 bg-stone-50/70 px-3 text-sm text-stone-900 outline-none transition focus:border-orange-600 focus:bg-white focus:ring-2 focus:ring-orange-500/20" />
                    </div>
                    <div class="mt-6 flex justify-end gap-2 border-t border-stone-200 pt-4">
                        <button type="button" @click="closeModal" :disabled="isSaving"
                            class="h-9 rounded-md border border-stone-300 bg-white px-4 text-sm font-semibold text-stone-800 transition hover:border-stone-400 hover:bg-stone-100">
                            Hủy
                        </button>
                        <button type="submit" :disabled="isSaving"
                            class="h-9 rounded-md border border-orange-700 bg-orange-600 px-4 text-sm font-semibold text-white shadow-xs transition hover:bg-orange-700 disabled:cursor-not-allowed disabled:opacity-60">
                            {{ isSaving ? 'Đang lưu...' : (isCreating ? 'Tạo giáo viên' : 'Lưu thay đổi') }}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onBeforeUnmount, onMounted, watch } from 'vue'
import PaginationBar from '@/components/common/PaginationBar.vue'
import SearchBar from '@/components/common/SearchBar.vue'
import { usePagination } from '@/composables/usePagination'
import teacherService from '@/services/teacher.service'
import {
    validateEmail,
    validatePhone,
    validateTextLength,
} from '@/utils/validation.helper'

const { page, pageSize, totalCount, totalPages, applyPagedResult, resetPage } = usePagination({
    initialPageSize: 20,
})

const teachers = ref([])
const keyword = ref('')
const statusFilter = ref('ALL')
const isLoading = ref(false)
const showModal = ref(false)
const isCreating = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')

const emptyForm = () => ({ id: null, fullName: '', phone: '', email: '', specialize: '', password: '' })
const form = ref(emptyForm())

let searchTimer = null
let requestToken = 0

watch(keyword, () => {
    if (searchTimer) clearTimeout(searchTimer)
    searchTimer = setTimeout(() => {
        resetPage()
        fetchTeachers()
    }, 300)
})

watch(statusFilter, () => {
    resetPage()
    fetchTeachers()
})

onBeforeUnmount(() => {
    if (searchTimer) clearTimeout(searchTimer)
})

async function fetchTeachers() {
    const token = ++requestToken
    isLoading.value = true
    errorMessage.value = ''
    try {
        const isActiveParam = statusFilter.value === 'ALL' ? undefined : statusFilter.value === 'true'
        const res = await teacherService.getPaged({
            page: page.value,
            pageSize: pageSize.value,
            keyword: keyword.value,
            isActive: isActiveParam,
        })
        if (token !== requestToken) return

        const items = applyPagedResult(res)
        if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
            page.value = totalPages.value
            await fetchTeachers()
            return
        }
        teachers.value = items
    } catch (error) {
        if (token !== requestToken) return
        errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách giáo viên.'
    } finally {
        if (token === requestToken) {
            isLoading.value = false
        }
    }
}

function openEditModal(teacher) {
    isCreating.value = false
    form.value = {
        id: teacher.id,
        fullName: teacher.fullName,
        phone: teacher.phone,
        email: teacher.email || '',
        specialize: teacher.specialize || '',
        password: ''
    }
    showModal.value = true
}

function openCreateModal() {
    isCreating.value = true
    form.value = emptyForm()
    showModal.value = true
}

function closeModal() {
    if (isSaving.value) return
    showModal.value = false
}

async function saveTeacher() {
    const fullNameCheck = validateTextLength(form.value.fullName, {
        label: 'Họ và tên',
        required: true,
        max: 100,
    })
    if (!fullNameCheck.valid) {
        alert(fullNameCheck.message)
        return
    }

    if (isCreating.value) {
        const phoneCheck = validatePhone(form.value.phone, {
            label: 'Số điện thoại',
            required: true,
        })
        if (!phoneCheck.valid) {
            alert(phoneCheck.message)
            return
        }
        const passwordCheck = validateTextLength(form.value.password, {
            label: 'Mật khẩu ban đầu',
            required: true,
            min: 6,
            max: 128,
        })
        if (!passwordCheck.valid) {
            alert(passwordCheck.message)
            return
        }
    }

    const emailCheck = validateEmail(form.value.email, { label: 'Email' })
    if (!emailCheck.valid) {
        alert(emailCheck.message)
        return
    }

    const specializeCheck = validateTextLength(form.value.specialize, {
        label: 'Chuyên môn',
        max: 100,
    })
    if (!specializeCheck.valid) {
        alert(specializeCheck.message)
        return
    }

    isSaving.value = true
    try {
        if (isCreating.value) {
            await teacherService.create({
                phone: form.value.phone,
                password: form.value.password,
                fullName: form.value.fullName,
                email: form.value.email || null
            })
        } else {
            await teacherService.update(form.value.id, {
                fullName: form.value.fullName,
                email: form.value.email,
                specialize: form.value.specialize
            })
        }

        showModal.value = false
        await fetchTeachers()
    } catch (err) {
        alert(err.response?.data?.message || (isCreating.value
            ? 'Có lỗi xảy ra khi tạo tài khoản giáo viên.'
            : 'Có lỗi xảy ra khi cập nhật hồ sơ giáo viên.'))
    } finally {
        isSaving.value = false
    }
}

async function deleteTeacher(id) {
    if (confirm('Xóa giáo viên sẽ xóa luôn tài khoản hệ thống liên kết. Bạn có chắc chắn?')) {
        try {
            await teacherService.delete(id)
            await fetchTeachers()
        } catch (err) {
            alert(err.response?.data?.message || 'Không thể xóa hồ sơ giáo viên này.')
        }
    }
}

onMounted(fetchTeachers)
</script>