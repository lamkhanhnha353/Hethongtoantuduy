<template>
    <div class="space-y-6">
        <!-- Header Page -->
        <div>
            <h1 class="text-2xl font-bold text-gray-800">Quản lý Phụ huynh & Học sinh</h1>
            <p class="text-sm text-gray-500 mt-1">Tra cứu phụ huynh, học sinh liên kết và cập nhật hồ sơ</p>
        </div>

        <!-- Search Bar -->
        <div class="w-full max-w-md">
            <SearchBar v-model="keyword" placeholder="Tìm tên, SĐT hoặc email phụ huynh..." />
        </div>

        <!-- Data Table -->
        <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
            <table class="w-full text-left text-sm">
                <thead class="bg-orange-50 text-orange-900 border-b border-orange-100">
                    <tr>
                        <th class="px-6 py-4 font-semibold w-1/4">Họ Tên Phụ huynh</th>
                        <th class="px-6 py-4 font-semibold w-1/4">Thông tin liên hệ</th>
                        <th class="px-6 py-4 font-semibold w-1/3">Học sinh trực thuộc</th>
                        <th class="px-6 py-4 text-right font-semibold">Thao tác</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-gray-100 text-gray-700">
                    <tr v-if="isLoading">
                        <td colspan="4" class="px-6 py-8 text-center text-orange-500">Đang tải dữ liệu...</td>
                    </tr>
                    <tr v-else-if="errorMessage">
                        <td colspan="4" class="px-6 py-8 text-center text-red-600">{{ errorMessage }}</td>
                    </tr>
                    <tr v-else-if="filteredParents.length === 0">
                        <td colspan="4" class="px-6 py-8 text-center text-gray-500">Không tìm thấy tài khoản phụ huynh
                            nào.</td>
                    </tr>
                    <tr v-for="parent in filteredParents" :key="parent.id" class="hover:bg-orange-50/50 transition-colors">
                        <!-- Tên Phụ huynh & Trạng thái -->
                        <td class="px-6 py-4 align-top">
                            <div class="font-semibold text-gray-900 text-base">{{ parent.fullName }}</div>
                            <span :class="[
                                'inline-block mt-1 text-xs px-2.5 py-0.5 rounded-full font-medium',
                                parent.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                            ]">
                                {{ parent.isActive ? 'Hoạt động' : 'Tạm khóa' }}
                            </span>
                        </td>

                        <!-- Liên hệ -->
                        <td class="px-6 py-4 align-top">
                            <div class="font-mono text-gray-800 font-medium">{{ parent.phone }}</div>
                            <div class="text-xs text-gray-400 mt-1">{{ parent.email || 'Chưa cập nhật email' }}</div>
                        </td>

                        <!-- Danh sách con trực thuộc -->
                        <td class="px-6 py-4 align-top">
                            <div v-if="parent.students && parent.students.length > 0" class="space-y-2">
                                <div v-for="student in parent.students" :key="student.id"
                                    class="bg-orange-50/60 border border-orange-100 p-2.5 rounded-xl flex items-center justify-between gap-3">
                                    <div class="flex items-center gap-2.5">
                                        <div
                                            class="w-8 h-8 rounded-full bg-orange-200 text-orange-800 font-bold flex items-center justify-center text-xs flex-shrink-0">
                                            {{ student.fullName.charAt(0).toUpperCase() }}
                                        </div>
                                        <div>
                                            <div class="font-medium text-gray-800 text-xs">{{ student.fullName }}</div>
                                            <div class="text-[11px] text-gray-500 flex items-center gap-2 mt-0.5">
                                                <span>⭐ {{ student.stars || 0 }} sao</span>
                                                <span>🏆 {{ student.totalPoints || 0 }} điểm</span>
                                                <span>🔥 {{ student.streakDays || 0 }} ngày</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <span v-else class="text-gray-400 italic text-xs">Chưa có học sinh liên kết</span>
                        </td>

                        <!-- Thao tác -->
                        <td class="px-6 py-4 text-right align-top">
                            <button @click="openEditModal(parent)"
                                class="text-orange-500 hover:text-orange-700 font-medium mr-3">
                                Sửa
                            </button>
                            <button @click="deleteParent(parent.id)"
                                class="text-red-500 hover:text-red-700 font-medium">
                                Xóa
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Form Sửa Phụ Huynh -->
        <div v-if="showModal" class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
            <div class="bg-white rounded-2xl w-full max-w-md overflow-hidden shadow-xl">
                <div class="px-6 py-4 border-b border-gray-100 bg-orange-50 flex justify-between items-center">
                    <h3 class="font-bold text-orange-900">Sửa thông tin Phụ huynh</h3>
                    <button @click="showModal = false" class="text-gray-400 hover:text-gray-600">&times;</button>
                </div>
                <form @submit.prevent="saveParent" class="p-6 space-y-4">
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Họ và tên *</label>
                        <input v-model="form.fullName" required type="text"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Mã PIN bảo mật</label>
                        <input v-model="form.pin" type="text" placeholder="Nhập mã PIN mới (nếu muốn đổi)..."
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div class="flex justify-end gap-3 mt-6">
                        <button type="button" @click="showModal = false"
                            class="px-4 py-2 text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200 text-sm">
                            Hủy
                        </button>
                        <button type="submit"
                            class="px-4 py-2 text-white bg-orange-500 rounded-lg hover:bg-orange-600 text-sm font-medium">
                            Lưu thay đổi
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import SearchBar from '@/components/common/SearchBar.vue'
import parentService from '@/services/parent.service'

const parents = ref([])
const keyword = ref('')
const isLoading = ref(false)
const showModal = ref(false)
const errorMessage = ref('')

const form = ref({ id: null, fullName: '', pin: '' })

const filteredParents = computed(() => {
    const query = keyword.value.toLowerCase().trim()
    if (!query) return parents.value

    return parents.value.filter(parent =>
        [
            parent.id,
            parent.fullName,
            parent.phone,
            parent.email,
            ...(parent.students ?? []).map(student => student.fullName)
        ].some(value => String(value ?? '').toLowerCase().includes(query))
    )
})

async function fetchParents() {
    isLoading.value = true
    errorMessage.value = ''
    try {
        const res = await parentService.getAll()
        parents.value = Array.isArray(res) ? res : (res.items ?? res.data ?? [])
    } catch (error) {
        errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách phụ huynh.'
    } finally {
        isLoading.value = false
    }
}

function openEditModal(parent) {
    form.value = {
        id: parent.id,
        fullName: parent.fullName,
        pin: parent.pin || ''
    }
    showModal.value = true
}

async function saveParent() {
    try {
        await parentService.update(form.value.id, {
            fullName: form.value.fullName,
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