<template>
    <div class="space-y-6">
        <!-- Header Page -->
        <div>
            <h1 class="text-2xl font-bold text-stone-900">Quản lý Phụ huynh & Học sinh</h1>
            <p class="text-sm text-stone-500 mt-1">Tra cứu phụ huynh, học sinh liên kết và cập nhật hồ sơ</p>
        </div>

        <!-- Search Bar -->
        <div class="w-full max-w-md">
            <SearchBar v-model="keyword" placeholder="Tìm tên, SĐT hoặc email phụ huynh..." />
        </div>

        <!-- Data Table -->
        <div class="bg-white rounded-lg border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
            <table class="w-full text-left text-sm">
                <thead class="bg-stone-100 text-xs font-bold uppercase tracking-wider text-stone-700 border-b-2 border-stone-300">
                    <tr>
                        <th class="px-5 py-3.5 w-1/4">Họ Tên Phụ huynh</th>
                        <th class="px-5 py-3.5 w-1/4">Thông tin liên hệ</th>
                        <th class="px-5 py-3.5 w-1/3">Học sinh trực thuộc</th>
                        <th class="px-5 py-3.5 text-right">Thao tác</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-stone-200 text-stone-700">
                    <tr v-if="isLoading">
                        <td colspan="4" class="px-5 py-8 text-center text-orange-600 font-medium">Đang tải dữ liệu...</td>
                    </tr>
                    <tr v-else-if="errorMessage">
                        <td colspan="4" class="px-5 py-8 text-center text-rose-700 font-medium">{{ errorMessage }}</td>
                    </tr>
                    <tr v-else-if="parents.length === 0">
                        <td colspan="4" class="px-5 py-8 text-center text-stone-500">Không tìm thấy tài khoản phụ huynh
                            nào.</td>
                    </tr>
                    <tr v-for="parent in parents" :key="parent.id" class="hover:bg-orange-50/50 transition-colors">
                        <!-- Tên Phụ huynh & Trạng thái -->
                        <td class="px-5 py-4 align-top">
                            <div class="font-bold text-stone-900 text-sm">{{ parent.fullName }}</div>
                            <span
                                class="mt-1.5 inline-flex items-center rounded border px-2 py-0.5 text-[11px] font-semibold"
                                :class="parent.isActive
                                    ? 'bg-emerald-50 text-emerald-800 border-emerald-300'
                                    : 'bg-rose-50 text-rose-800 border-rose-300'"
                                :aria-label="`Trạng thái tài khoản của ${parent.fullName}`">
                                {{ parent.isActive ? 'Hoạt động' : 'Đã khóa' }}
                            </span>
                        </td>

                        <!-- Liên hệ -->
                        <td class="px-5 py-4 align-top">
                            <div class="font-mono text-stone-900 font-semibold tabular-nums">{{ parent.phone }}</div>
                            <div class="text-xs text-stone-400 mt-1">{{ parent.email || 'Chưa cập nhật email' }}</div>
                        </td>

                        <!-- Danh sách con trực thuộc -->
                        <td class="px-5 py-4 align-top">
                            <div v-if="parent.students && parent.students.length > 0" class="space-y-2">
                                <div v-for="student in parent.students" :key="student.id"
                                    class="bg-stone-50 border border-stone-300 p-2.5 rounded-md flex items-center justify-between gap-3">
                                    <div class="flex items-center gap-2.5">
                                        <div
                                            class="w-8 h-8 rounded-full bg-orange-600 text-white font-bold flex items-center justify-center text-xs flex-shrink-0">
                                            {{ student.fullName.charAt(0).toUpperCase() }}
                                        </div>
                                        <div>
                                            <div class="font-semibold text-stone-900 text-xs">{{ student.fullName }}</div>
                                            <div class="text-[11px] text-stone-500 flex items-center gap-2 mt-0.5 tabular-nums">
                                                <span>⭐ {{ student.stars || 0 }} sao</span>
                                                <span>🏆 {{ student.totalPoints || 0 }} điểm</span>
                                                <span>🔥 {{ student.streakDays || 0 }} ngày</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <span v-else class="text-stone-400 italic text-xs">Chưa có học sinh liên kết</span>
                        </td>

                        <!-- Thao tác -->
                        <td class="px-5 py-4 text-right align-top whitespace-nowrap">
                            <button @click="openEditModal(parent)"
                                class="h-8 px-2.5 rounded-md border border-stone-300 bg-white text-stone-800 hover:bg-stone-100 hover:border-stone-400 text-xs font-semibold mr-2 transition-colors cursor-pointer">
                                Sửa
                            </button>
                            <button @click="deleteParent(parent.id)"
                                class="h-8 px-2.5 rounded-md border border-rose-300 bg-white text-rose-700 hover:bg-rose-50 hover:border-rose-400 text-xs font-semibold transition-colors cursor-pointer">
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
                item-label="phụ huynh"
                @change="fetchParents"
            />
        </div>

        <!-- Modal Form Sửa Phụ Huynh -->
        <div v-if="showModal" class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
            <div class="bg-white rounded-lg border border-stone-300 w-full max-w-md overflow-hidden shadow-xl">
                <div class="px-6 py-4 border-b border-stone-200 bg-stone-50 flex justify-between items-center">
                    <h3 class="font-bold text-stone-900">Sửa thông tin Phụ huynh</h3>
                    <button @click="showModal = false" class="text-stone-400 hover:text-stone-700 p-1 rounded-md hover:bg-stone-200/70 cursor-pointer">&times;</button>
                </div>
                <form @submit.prevent="saveParent" class="p-6 space-y-4">
                    <div>
                        <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Họ và tên *</label>
                        <input v-model="form.fullName" required type="text" maxlength="100"
                            class="w-full h-10 px-3.5 bg-stone-50/70 border border-stone-300 rounded-md focus:bg-white focus:outline-none focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 text-sm text-stone-900 transition-colors" />
                    </div>
                    <div>
                        <label class="block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5">Mã PIN bảo mật</label>
                        <input v-model.trim="form.pin" type="text" inputmode="numeric" minlength="4" maxlength="6"
                            pattern="[0-9]{4,6}" title="Mã PIN gồm 4 đến 6 chữ số"
                            placeholder="Nhập mã PIN mới (nếu muốn đổi)..."
                            class="w-full h-10 px-3.5 bg-stone-50/70 border border-stone-300 rounded-md focus:bg-white focus:outline-none focus:border-orange-600 focus:ring-2 focus:ring-orange-500/20 text-sm text-stone-900 tabular-nums transition-colors" />
                    </div>
                    <div class="flex justify-end gap-2 pt-3 border-t border-stone-200">
                        <button type="button" @click="showModal = false"
                            class="h-9 px-3.5 text-stone-800 bg-white border border-stone-300 rounded-md hover:bg-stone-100 text-sm font-semibold cursor-pointer">
                            Hủy
                        </button>
                        <button type="submit"
                            class="h-9 px-4 text-white bg-orange-600 border border-orange-700 rounded-md hover:bg-orange-700 text-sm font-semibold shadow-xs cursor-pointer">
                            Lưu thay đổi
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
import parentService from '@/services/parent.service'
import { PIN_REGEX, validateTextLength } from '@/utils/validation.helper'

const { page, pageSize, totalCount, totalPages, applyPagedResult, resetPage } = usePagination({
    initialPageSize: 20,
})

const parents = ref([])
const keyword = ref('')
const isLoading = ref(false)
const showModal = ref(false)
const errorMessage = ref('')

const form = ref({ id: null, fullName: '', pin: '' })

let searchTimer = null
let requestToken = 0

watch(keyword, () => {
    if (searchTimer) clearTimeout(searchTimer)
    searchTimer = setTimeout(() => {
        resetPage()
        fetchParents()
    }, 300)
})

onBeforeUnmount(() => {
    if (searchTimer) clearTimeout(searchTimer)
})

async function fetchParents() {
    const token = ++requestToken
    isLoading.value = true
    errorMessage.value = ''
    try {
        const res = await parentService.getPaged({
            page: page.value,
            pageSize: pageSize.value,
            keyword: keyword.value,
        })
        if (token !== requestToken) return

        const items = applyPagedResult(res)
        if (items.length === 0 && page.value > 1 && totalPages.value > 0 && page.value > totalPages.value) {
            page.value = totalPages.value
            await fetchParents()
            return
        }
        parents.value = items
    } catch (error) {
        if (token !== requestToken) return
        errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách phụ huynh.'
    } finally {
        if (token === requestToken) {
            isLoading.value = false
        }
    }
}

function openEditModal(parent) {
    form.value = {
        id: parent.id,
        fullName: parent.fullName,
        pin: ''
    }
    showModal.value = true
}

async function saveParent() {
    const fullNameCheck = validateTextLength(form.value.fullName, {
        label: 'Họ và tên',
        required: true,
        max: 100,
    })
    if (!fullNameCheck.valid) {
        alert(fullNameCheck.message)
        return
    }
    if (form.value.pin && !PIN_REGEX.test(form.value.pin)) {
        alert('Mã PIN phải gồm 4 đến 6 chữ số.')
        return
    }

    try {
        await parentService.update(form.value.id, {
            fullName: form.value.fullName.trim(),
            pin: form.value.pin
        })
        showModal.value = false
        await fetchParents()
    } catch (err) {
        alert(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật thông tin.')
    }
}

async function deleteParent(id) {
    if (confirm('Xóa phụ huynh sẽ xóa luôn tất cả tài khoản học sinh liên kết. Bạn có chắc chắn?')) {
        try {
            await parentService.delete(id)
            await fetchParents()
        } catch (err) {
            alert(err.response?.data?.message || 'Lỗi khi xóa phụ huynh.')
        }
    }
}

onMounted(fetchParents)
</script>