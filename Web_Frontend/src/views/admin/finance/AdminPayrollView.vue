<script setup>
import { computed, ref, watch } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import createApiClient from "@/services/api.service";

const api = createApiClient("/api/payrolls");
const period = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
const payrolls = ref([]);
const loading = ref(false);
const batchLoading = ref(false);
const detailLoading = ref(false);
const adjustmentLoading = ref(false);
const emailLoadingId = ref(null);
const previewLoading = ref(false);

const detailDialogVisible = ref(false);
const adjustmentDialogVisible = ref(false);
const previewDialogVisible = ref(false);
const selectedPayroll = ref(null);
const selectedDetail = ref(null);
const previewHtml = ref("");

const adjustmentForm = ref({
  teacherId: null,
  type: "Bonus",
  amount: null,
  reason: "",
  appliedDate: new Date(),
});

const periodQuery = computed(() => ({
  month: period.value.getMonth() + 1,
  year: period.value.getFullYear(),
}));

const formatCurrency = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const formatDate = (value) => {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat("vi-VN").format(date);
};

const formatPeriod = () =>
  `${String(periodQuery.value.month).padStart(2, "0")}/${periodQuery.value.year}`;

const emailStatusType = (status) => {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "sent") return "success";
  if (["pending", "queued", "processing"].includes(normalized)) return "warning";
  if (normalized === "failed") return "danger";
  return "info";
};

const emailStatusLabel = (status) => {
  const normalized = (status ?? "").toLowerCase();
  if (normalized === "sent") return "Sent";
  if (["pending", "queued", "processing"].includes(normalized)) return "Pending";
  if (normalized === "failed") return "Failed";
  return "NotSent";
};

function getErrorMessage(error, fallback) {
  return error?.response?.data?.detail
    ?? error?.response?.data?.message
    ?? error?.message
    ?? fallback;
}

async function loadPayrolls() {
  loading.value = true;
  try {
    const response = await api.get("/admin/summary", { params: periodQuery.value });
    payrolls.value = Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    payrolls.value = [];
    ElMessage.error(getErrorMessage(error, "Không thể tải bảng lương."));
  } finally {
    loading.value = false;
  }
}

watch(period, loadPayrolls, { immediate: true });

async function openLessonDetails(payroll) {
  selectedPayroll.value = payroll;
  selectedDetail.value = null;
  detailDialogVisible.value = true;
  detailLoading.value = true;
  try {
    const response = await api.get("/detail", {
      params: {
        teacherId: payroll.teacherId,
        ...periodQuery.value,
      },
    });
    selectedDetail.value = response.data;
  } catch (error) {
    detailDialogVisible.value = false;
    ElMessage.error(getErrorMessage(error, "Không thể tải chi tiết phiếu lương."));
  } finally {
    detailLoading.value = false;
  }
}

function openAdjustmentForm(payroll) {
  selectedPayroll.value = payroll;
  adjustmentForm.value = {
    teacherId: payroll.teacherId,
    type: "Bonus",
    amount: null,
    reason: "",
    appliedDate: new Date(period.value.getFullYear(), period.value.getMonth(), 1),
  };
  adjustmentDialogVisible.value = true;
}

function toLocalDateString(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

async function submitAdjustment() {
  const form = adjustmentForm.value;
  if (!form.teacherId || !form.amount || form.amount <= 0 || !form.reason.trim() || !form.appliedDate) {
    ElMessage.warning("Vui lòng nhập đầy đủ thông tin điều chỉnh hợp lệ.");
    return;
  }

  adjustmentLoading.value = true;
  try {
    await api.post("/adjustments", {
      teacherId: form.teacherId,
      type: form.type,
      amount: Number(form.amount),
      reason: form.reason.trim(),
      appliedDate: toLocalDateString(form.appliedDate),
    });
    adjustmentDialogVisible.value = false;
    ElMessage.success("Đã thêm khoản điều chỉnh lương.");
    await loadPayrolls();
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "Không thể thêm khoản điều chỉnh."));
  } finally {
    adjustmentLoading.value = false;
  }
}

async function openEmailPreview(payroll) {
  if (!payroll.payrollId) {
    ElMessage.warning("Phiếu lương chưa được tạo, vui lòng tải lại bảng lương.");
    return;
  }

  selectedPayroll.value = payroll;
  previewHtml.value = "";
  previewDialogVisible.value = true;
  previewLoading.value = true;
  try {
    const response = await api.get(`/${payroll.payrollId}/preview-email`, {
      responseType: "text",
    });
    previewHtml.value = typeof response.data === "string"
      ? response.data
      : JSON.stringify(response.data);
  } catch (error) {
    previewDialogVisible.value = false;
    ElMessage.error(getErrorMessage(error, "Không thể tạo bản xem trước email."));
  } finally {
    previewLoading.value = false;
  }
}

async function sendSingleEmail(payroll) {
  if (!payroll.payrollId) {
    ElMessage.warning("Phiếu lương chưa được tạo, vui lòng tải lại bảng lương.");
    return;
  }

  try {
    await ElMessageBox.confirm(
      `Đưa email phiếu lương tháng ${formatPeriod()} của ${payroll.teacherName} vào hàng đợi?`,
      "Xác nhận gửi email",
      { confirmButtonText: "Gửi email", cancelButtonText: "Hủy", type: "warning" },
    );
  } catch {
    return;
  }

  emailLoadingId.value = payroll.payrollId;
  try {
    const response = await api.post(`/${payroll.payrollId}/send-email`);
    ElMessage.success(response.data?.message ?? "Email đã được đưa vào hàng đợi.");
    await loadPayrolls();
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "Không thể gửi email phiếu lương."));
  } finally {
    emailLoadingId.value = null;
  }
}

async function sendBatchEmails() {
  try {
    await ElMessageBox.confirm(
      `Gửi email phiếu lương cho tất cả giáo viên trong tháng ${formatPeriod()}?`,
      "Gửi email hàng loạt",
      { confirmButtonText: "Xác nhận gửi", cancelButtonText: "Hủy", type: "warning" },
    );
  } catch {
    return;
  }

  batchLoading.value = true;
  try {
    const response = await api.post("/send-batch-emails", null, { params: periodQuery.value });
    const queued = response.data?.queuedCount ?? 0;
    const skipped = response.data?.skippedCount ?? 0;
    ElMessage.success(`Đã đưa ${queued} email vào hàng đợi; bỏ qua ${skipped} email.`);
    await loadPayrolls();
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "Không thể gửi email hàng loạt."));
  } finally {
    batchLoading.value = false;
  }
}
</script>

<template>
  <main class="payroll-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Quản lý tài chính</p>
        <h1>Bảng lương giáo viên</h1>
        <p class="page-description">Đối soát buổi dạy hợp lệ, các khoản điều chỉnh và trạng thái gửi phiếu lương.</p>
      </div>
      <div class="header-actions">
        <el-date-picker
          v-model="period"
          type="month"
          format="MM/YYYY"
          placeholder="Chọn tháng"
          :clearable="false"
          aria-label="Chọn tháng tính lương"
        />
        <el-button type="primary" :loading="batchLoading" @click="sendBatchEmails">
          Gửi Email Hàng Loạt
        </el-button>
      </div>
    </header>

    <section class="table-card">
      <el-table v-loading="loading" :data="payrolls" row-key="payrollId" stripe>
        <el-table-column label="Giáo viên" min-width="200">
          <template #default="{ row }">
            <div class="teacher-cell">
              <span class="teacher-avatar">{{ row.teacherName?.charAt(0) ?? "G" }}</span>
              <span>
                <strong>{{ row.teacherName }}</strong>
                <small>{{ row.teacherEmail || "Chưa có email" }}</small>
              </span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="totalValidSessions" label="Buổi hợp lệ" min-width="120" align="center">
          <template #header>
            <span>Buổi hợp lệ <el-tooltip content="Lesson COMPLETED và có ít nhất một bản ghi điểm danh."><span class="help-icon">ⓘ</span></el-tooltip></span>
          </template>
          <template #default="{ row }">
            <span class="session-count">{{ row.totalValidSessions }}</span>
          </template>
        </el-table-column>
        <el-table-column label="Lương cơ bản" min-width="150" align="right">
          <template #default="{ row }">{{ formatCurrency(row.baseSalary) }}</template>
        </el-table-column>
        <el-table-column label="Thưởng / Phạt" min-width="165" align="right">
          <template #default="{ row }">
            <span class="adjustment-values">
              <span class="bonus">+{{ formatCurrency(row.totalBonus) }}</span>
              <span class="penalty">−{{ formatCurrency(row.totalPenalty) }}</span>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="Thực nhận" min-width="155" align="right">
          <template #default="{ row }"><strong class="net-pay">{{ formatCurrency(row.finalSalary) }}</strong></template>
        </el-table-column>
        <el-table-column label="Trạng thái Email" min-width="135" align="center">
          <template #default="{ row }">
            <el-tooltip v-if="row.emailErrorMessage" :content="row.emailErrorMessage">
              <el-tag :type="emailStatusType(row.emailStatus)" effect="light">{{ emailStatusLabel(row.emailStatus) }}</el-tag>
            </el-tooltip>
            <el-tag v-else :type="emailStatusType(row.emailStatus)" effect="light">{{ emailStatusLabel(row.emailStatus) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Thao tác" fixed="right" width="170" align="center">
          <template #default="{ row }">
            <div class="row-actions">
              <el-button size="small" type="primary" plain @click="openLessonDetails(row)">Chi tiết</el-button>
              <el-dropdown trigger="click">
                <el-button size="small" class="more-actions-button">
                  Thao tác <span class="dropdown-caret">⌄</span>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click="openAdjustmentForm(row)">Thêm Phạt/Thưởng</el-dropdown-item>
                    <el-dropdown-item :disabled="!row.payrollId" @click="openEmailPreview(row)">Preview Mail</el-dropdown-item>
                    <el-dropdown-item
                      :disabled="!row.payrollId || emailLoadingId === row.payrollId"
                      @click="sendSingleEmail(row)"
                    >
                      {{ emailLoadingId === row.payrollId ? "Đang gửi..." : "Gửi Mail" }}
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
        <template #empty>
          <el-empty description="Không có dữ liệu lương trong tháng này." />
        </template>
      </el-table>
      <footer class="table-footer">Kỳ lương: <strong>{{ formatPeriod() }}</strong> · {{ payrolls.length }} giáo viên</footer>
    </section>

    <el-dialog v-model="detailDialogVisible" :title="`Chi tiết lương — ${selectedPayroll?.teacherName ?? ''}`" width="min(980px, 94vw)" destroy-on-close>
      <div v-loading="detailLoading" class="dialog-content">
        <template v-if="selectedDetail">
          <div class="detail-summary">
            <span>{{ selectedDetail.payslip.totalValidSessions }} buổi hợp lệ</span>
            <strong>{{ formatCurrency(selectedDetail.payslip.finalSalary) }}</strong>
          </div>
          <el-table :data="selectedDetail.sessions" max-height="420" stripe>
            <el-table-column prop="lessonId" label="Mã buổi" width="100" />
            <el-table-column label="Ngày dạy" min-width="150">
              <template #default="{ row }">{{ formatDate(row.lessonDate) }}</template>
            </el-table-column>
            <el-table-column prop="title" label="Bài học / Chủ đề" min-width="220">
              <template #default="{ row }">{{ row.title || "Chưa đặt tên" }}</template>
            </el-table-column>
            <el-table-column prop="status" label="Trạng thái" width="135" />
            <el-table-column label="Điểm danh" width="130">
              <template #default="{ row }">{{ row.hasAttendance ? "Đã điểm danh" : "Chưa điểm danh" }}</template>
            </el-table-column>
            <el-table-column label="Tính lương" width="130" align="center">
              <template #default="{ row }">
                <el-tag :type="row.isEligible ? 'success' : 'info'">{{ row.isEligible ? "+1 Đơn vị" : "0" }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
          <h3 class="adjustments-heading">Khoản phạt / thưởng đối soát</h3>
          <el-table :data="selectedDetail.adjustments" size="small">
            <el-table-column label="Ngày" width="130"><template #default="{ row }">{{ formatDate(row.appliedDate) }}</template></el-table-column>
            <el-table-column label="Loại" width="120"><template #default="{ row }"><el-tag :type="row.type === 'Bonus' ? 'success' : 'danger'">{{ row.type === "Bonus" ? "Thưởng" : "Phạt" }}</el-tag></template></el-table-column>
            <el-table-column label="Số tiền" width="150" align="right"><template #default="{ row }">{{ formatCurrency(row.amount) }}</template></el-table-column>
            <el-table-column prop="reason" label="Lý do" min-width="220" />
            <template #empty><el-empty description="Không có khoản điều chỉnh." :image-size="60" /></template>
          </el-table>
        </template>
      </div>
    </el-dialog>

    <el-dialog v-model="adjustmentDialogVisible" title="Thêm khoản phạt / thưởng" width="min(540px, 94vw)" destroy-on-close>
      <el-form label-position="top" @submit.prevent="submitAdjustment">
        <el-form-item label="Giáo viên">
          <el-input :model-value="selectedPayroll?.teacherName" disabled />
        </el-form-item>
        <el-form-item label="Loại điều chỉnh" required>
          <el-radio-group v-model="adjustmentForm.type">
            <el-radio-button value="Bonus">Thưởng</el-radio-button>
            <el-radio-button value="Penalty">Phạt</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="Số tiền (VND)" required>
          <el-input-number v-model="adjustmentForm.amount" :min="1" :precision="0" :step="50000" class="full-width" />
        </el-form-item>
        <el-form-item label="Ngày áp dụng" required>
          <el-date-picker v-model="adjustmentForm.appliedDate" type="date" format="DD/MM/YYYY" :clearable="false" class="full-width" />
        </el-form-item>
        <el-form-item label="Lý do" required>
          <el-input v-model="adjustmentForm.reason" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="Nhập lý do điều chỉnh" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="adjustmentDialogVisible = false">Hủy</el-button>
        <el-button type="primary" :loading="adjustmentLoading" @click="submitAdjustment">Lưu điều chỉnh</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="previewDialogVisible" :title="`Preview phiếu lương — ${selectedPayroll?.teacherName ?? ''}`" width="min(900px, 94vw)" destroy-on-close>
      <div v-loading="previewLoading" class="preview-frame">
        <iframe v-if="previewHtml" :srcdoc="previewHtml" sandbox="" title="Bản xem trước email phiếu lương" />
      </div>
    </el-dialog>
  </main>
</template>

<style scoped>
.payroll-page { display: grid; gap: 22px; max-width: 1500px; margin: 0 auto; padding-bottom: 36px; color: #1f2937; }
.page-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; flex-wrap: wrap; }
.eyebrow { margin: 0 0 5px; color: #ea580c; font-size: 11px; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
h1 { margin: 0; color: #431407; font-size: clamp(24px, 3vw, 32px); font-weight: 800; letter-spacing: -.03em; }
.page-description { margin: 7px 0 0; color: #6b7280; font-size: 14px; }
.header-actions { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.table-card { overflow: hidden; border: 1px solid #ffedd5; border-radius: 18px; background: #fff; box-shadow: 0 8px 24px rgb(124 45 18 / 5%); }
.table-footer { padding: 13px 18px; border-top: 1px solid #fff7ed; color: #6b7280; font-size: 12px; }
.table-footer strong { color: #9a3412; }
.teacher-cell { display: flex; align-items: center; gap: 10px; }
.teacher-cell strong, .teacher-cell small { display: block; }
.teacher-cell strong { color: #292524; font-size: 13px; }
.teacher-cell small { margin-top: 3px; color: #9ca3af; font-size: 11px; }
.teacher-avatar { display: grid; width: 36px; height: 36px; flex: 0 0 36px; place-items: center; border-radius: 12px; background: #fff7ed; color: #c2410c; font-weight: 800; }
.session-count { display: inline-grid; min-width: 30px; height: 30px; place-items: center; border-radius: 10px; background: #fff7ed; color: #c2410c; font-weight: 800; }
.adjustment-values { display: grid; gap: 3px; font-size: 12px; }
.bonus { color: #15803d; }
.penalty { color: #dc2626; }
.net-pay { color: #9a3412; }
.row-actions { display: flex; align-items: center; justify-content: center; gap: 7px; white-space: nowrap; }
.more-actions-button { color: #57534e; border-color: #e7e5e4; }
.more-actions-button:hover,
.more-actions-button:focus { color: #c2410c; border-color: #fdba74; background: #fff7ed; }
.dropdown-caret { margin-left: 5px; color: #78716c; font-size: 12px; }
.dialog-content { min-height: 120px; }
.detail-summary { display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; padding: 12px 15px; border-radius: 12px; background: #fff7ed; color: #7c2d12; font-size: 13px; }
.detail-summary strong { font-size: 17px; }
.adjustments-heading { margin: 22px 0 10px; color: #431407; font-size: 15px; }
.preview-frame { min-height: 60vh; }
.preview-frame iframe { width: 100%; height: 68vh; border: 1px solid #e5e7eb; border-radius: 10px; background: white; }
.full-width { width: 100%; }
:deep(.el-table th.el-table__cell) { background: #fff7ed; color: #7c2d12; font-size: 12px; }
:deep(.el-table .cell) { line-height: 1.45; }
.help-icon { margin-left: 3px; color: #9a3412; cursor: help; }
@media (max-width: 640px) {
  .payroll-page { gap: 16px; }
  .header-actions { width: 100%; }
  .header-actions :deep(.el-date-editor) { flex: 1; }
  .header-actions :deep(.el-button) { width: 100%; margin-left: 0; }
}
</style>
