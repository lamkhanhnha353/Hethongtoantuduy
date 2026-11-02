<template>
    <div class="space-y-6">
        <!-- Header Page -->
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
                <h1 class="text-2xl font-bold text-gray-800">Quản lý Giáo viên</h1>
                <p class="text-sm text-gray-500 mt-1">Tra cứu và cập nhật hồ sơ, chuyên môn giáo viên</p>
            </div>
            <button @click="openCreateModal"
                class="inline-flex items-center justify-center gap-2 rounded-lg bg-orange-500 px-4 py-2 text-sm font-medium text-white hover:bg-orange-600">
                <span aria-hidden="true">+</span>
                Thêm giáo viên
            </button>
        </div>

        <!-- Search Bar -->
        <div class="w-full max-w-md">
            <SearchBar v-model="keyword" placeholder="Tìm tên, SĐT hoặc email giáo viên..." />
        </div>

        <!-- Data Table -->
        <div class="bg-white rounded-2xl border border-orange-100 shadow-sm overflow-hidden">
            <table class="w-full text-left text-sm">
                <thead class="bg-orange-50 text-orange-900 border-b border-orange-100">
                    <tr>
                        <th class="px-6 py-4 font-semibold">Họ & Tên Giáo viên</th>
                        <th class="px-6 py-4 font-semibold">Thông tin liên hệ</th>
                        <th class="px-6 py-4 font-semibold">Chuyên môn</th>
                        <th class="px-6 py-4 font-semibold">Trạng thái</th>
                        <th class="px-6 py-4 text-right font-semibold">Thao tác</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-gray-100 text-gray-700">
                    <tr v-if="isLoading">
                        <td colspan="5" class="px-6 py-8 text-center text-orange-500">Đang tải dữ liệu...</td>
                    </tr>
                    <tr v-else-if="errorMessage">
                        <td colspan="5" class="px-6 py-8 text-center text-red-600">{{ errorMessage }}</td>
                    </tr>
                    <tr v-else-if="filteredTeachers.length === 0">
                        <td colspan="5" class="px-6 py-8 text-center text-gray-500">Không tìm thấy dữ liệu giáo viên.
                        </td>
                    </tr>
                    <tr v-for="teacher in filteredTeachers" :key="teacher.id" class="hover:bg-orange-50/50 transition-colors">
                        <td class="px-6 py-4 font-semibold text-gray-900">{{ teacher.fullName }}</td>
                        <td class="px-6 py-4">
                            <div class="font-mono text-gray-800 font-medium">{{ teacher.phone || 'Chưa cập nhật' }}
                            </div>
                            <div class="text-xs text-gray-400 mt-0.5">{{ teacher.email || 'Chưa cập nhật email' }}</div>
                        </td>
                        <td class="px-6 py-4">
                            <span
                                class="bg-orange-100 text-orange-800 py-1 px-3 rounded-full text-xs font-medium inline-block">
                                {{ teacher.specialize || 'Toán Tư Duy' }}
                            </span>
                        </td>
                        <td class="px-6 py-4">
                            <select :value="teacher.isActive ? 'active' : 'blocked'"
                                :disabled="statusUpdating[teacher.id]"
                                :aria-label="`Trạng thái tài khoản của ${teacher.fullName}`"
                                @change="updateTeacherStatus(teacher, $event)"
                                :class="[
                                    'rounded-lg border px-3 py-1.5 text-xs font-medium focus:outline-none focus:ring-2 focus:ring-orange-300 disabled:opacity-60',
                                    teacher.isActive ? 'border-green-200 bg-green-50 text-green-700' : 'border-red-200 bg-red-50 text-red-700'
                                ]">
                                <option value="active">Active</option>
                                <option value="blocked">Blocked</option>
                            </select>
                        </td>
                        <td class="px-6 py-4 text-right">
                            <button @click="openEditModal(teacher)"
                                class="text-orange-500 hover:text-orange-700 font-medium mr-3">
                                Sửa
                            </button>
                            <button @click="deleteTeacher(teacher.id)"
                                class="text-red-500 hover:text-red-700 font-medium">
                                Xóa
                            </button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Form Giáo Viên -->
        <div v-if="showModal" class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
            <div class="bg-white rounded-2xl w-full max-w-md overflow-hidden shadow-xl">
                <div class="px-6 py-4 border-b border-gray-100 bg-orange-50 flex justify-between items-center">
                    <h3 class="font-bold text-orange-900">{{ isCreating ? 'Thêm Giáo viên mới' : 'Sửa thông tin Giáo viên' }}</h3>
                    <button type="button" @click="closeModal" class="text-gray-400 hover:text-gray-600">&times;</button>
                </div>
                <form @submit.prevent="saveTeacher" class="p-6 space-y-4">
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Họ và tên *</label>
                        <input v-model="form.fullName" required type="text"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Số điện thoại (Tài khoản đăng
                            nhập)</label>
                        <input v-if="!isCreating" :value="form.phone" disabled type="tel"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg bg-gray-50 text-gray-500 text-sm font-mono cursor-not-allowed" />
                        <input v-else v-model.trim="form.phone" required type="tel" autocomplete="tel"
                            placeholder="Nhập số điện thoại đăng nhập"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm font-mono" />
                    </div>
                    <div v-if="isCreating">
                        <label class="block text-sm font-medium text-gray-700 mb-1">Mật khẩu ban đầu *</label>
                        <input v-model="form.password" required minlength="6" type="password" autocomplete="new-password"
                            placeholder="Ít nhất 6 ký tự"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
                        <input v-model.trim="form.email" type="email" autocomplete="email"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div v-if="!isCreating">
                        <label class="block text-sm font-medium text-gray-700 mb-1">Chuyên môn / Lĩnh vực giảng
                            dạy</label>
                        <input v-model="form.specialize" type="text"
                            placeholder="VD: Toán tư duy Soroban, Fingermath..."
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:border-orange-500 text-sm" />
                    </div>
                    <div class="flex justify-end gap-3 mt-6">
                        <button type="button" @click="closeModal" :disabled="isSaving"
                            class="px-4 py-2 text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200 text-sm">
                            Hủy
                        </button>
                        <button type="submit" :disabled="isSaving"
                            class="px-4 py-2 text-white bg-orange-500 rounded-lg hover:bg-orange-600 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-60">
                            {{ isSaving ? 'Đang lưu...' : (isCreating ? 'Tạo giáo viên' : 'Lưu thay đổi') }}
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
import teacherService from '@/services/teacher.service'

const teachers = ref([])
const keyword = ref('')
const isLoading = ref(false)
const showModal = ref(false)
const isCreating = ref(false)
const isSaving = ref(false)
const statusUpdating = ref({})
const errorMessage = ref('')

const emptyForm = () => ({ id: null, fullName: '', phone: '', email: '', specialize: '', password: '' })
const form = ref(emptyForm())

const filteredTeachers = computed(() => {
    const query = keyword.value.toLowerCase().trim()
    if (!query) return teachers.value

    return teachers.value.filter(teacher =>
        [teacher.id, teacher.fullName, teacher.phone, teacher.email, teacher.specialize]
            .some(value => String(value ?? '').toLowerCase().includes(query))
    )
})

async function fetchTeachers() {
    isLoading.value = true
    errorMessage.value = ''
    try {
        const res = await teacherService.getAll()
        teachers.value = Array.isArray(res) ? res : (res.items ?? res.data ?? [])
    } catch (error) {
        errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách giáo viên.'
    } finally {
        isLoading.value = false
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

async function updateTeacherStatus(teacher, event) {
    const previousStatus = teacher.isActive
    const isActive = event.target.value === 'active'
    if (previousStatus === isActive) return

    statusUpdating.value = { ...statusUpdating.value, [teacher.id]: true }
    try {
        await teacherService.updateStatus(teacher.id, isActive)
        teacher.isActive = isActive
    } catch (err) {
        event.target.value = previousStatus ? 'active' : 'blocked'
        alert(err.response?.data?.message || 'Không thể cập nhật trạng thái giáo viên.')
    } finally {
        const updating = { ...statusUpdating.value }
        delete updating[teacher.id]
        statusUpdating.value = updating
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