<script setup>
import { computed, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import createApiClient from "@/services/api.service";
import authService from "@/services/auth.service";

const api = createApiClient("/api/payrolls");
const teacherId = authService.getProfileId();
const period = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
const detail = ref(null);
const loading = ref(false);

const payslip = computed(() => detail.value?.payslip ?? null);
const sessions = computed(() => detail.value?.sessions ?? []);
const adjustments = computed(() => detail.value?.adjustments ?? []);
const bonusTotal = computed(() => adjustments.value
  .filter((item) => item.type === "Bonus")
  .reduce((total, item) => total + item.amount, 0));
const penaltyTotal = computed(() => adjustments.value
  .filter((item) => item.type === "Penalty")
  .reduce((total, item) => total + item.amount, 0));

const periodQuery = computed(() => ({
  month: period.value.getMonth() + 1,
  year: period.value.getFullYear(),
}));

// Không cho chọn tháng trong tương lai
function disableFutureMonth(date) {
  const now = new Date();
  return (
    date.getFullYear() > now.getFullYear() ||
    (date.getFullYear() === now.getFullYear() && date.getMonth() > now.getMonth())
  );
}

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

function getErrorMessage(error, fallback) {
  return error?.response?.data?.detail
    ?? error?.response?.data?.message
    ?? error?.message
    ?? fallback;
}

async function loadPayslip() {
  if (!teacherId) {
    detail.value = null;
    ElMessage.error("Không xác định được hồ sơ giáo viên của tài khoản hiện tại. Vui lòng đăng nhập lại.");
    return;
  }

  loading.value = true;
  try {
    const response = await api.get("/detail", {
      params: {
        teacherId,
        ...periodQuery.value,
      },
    });
    detail.value = response.data;
  } catch (error) {
    detail.value = null;
    ElMessage.error(getErrorMessage(error, "Không thể tải phiếu lương."));
  } finally {
    loading.value = false;
  }
}

watch(period, loadPayslip, { immediate: true });
</script>

<template>
  <main v-loading="loading" class="payslip-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Thu nhập cá nhân</p>
        <h1>Phiếu lương giáo viên</h1>
        <p class="page-description">Theo dõi lương theo số buổi dạy hợp lệ và các khoản điều chỉnh minh bạch.</p>
      </div>
      <el-date-picker
        v-model="period"
        type="month"
        format="MM/YYYY"
        placeholder="Chọn tháng"
        :clearable="false"
        :disabled-date="disableFutureMonth"
        aria-label="Chọn tháng tra cứu lương"
      />
    </header>

    <template v-if="payslip">
      <section v-loading="loading" class="summary-grid">
        <el-card class="summary-card net-card" shadow="never">
          <div class="stat-top"><span class="stat-icon">₫</span><span class="stat-label">Tổng thực nhận</span></div>
          <strong class="stat-value tabular-num">{{ formatCurrency(payslip.finalSalary) }}</strong>
          <span class="stat-note tabular-num">Kỳ lương {{ String(payslip.month).padStart(2, "0") }}/{{ payslip.year }}</span>
        </el-card>
        <el-card class="summary-card" shadow="never">
          <div class="stat-top"><span class="stat-icon session-icon">✓</span><span class="stat-label">Buổi dạy hợp lệ</span></div>
          <strong class="stat-value tabular-num">{{ payslip.totalValidSessions }} <small>buổi</small></strong>
          <span class="stat-note">Đã hoàn thành và có điểm danh</span>
        </el-card>
        <el-card class="summary-card" shadow="never">
          <div class="stat-top"><span class="stat-icon adjustment-icon">±</span><span class="stat-label">Thưởng / Phạt</span></div>
          <div class="adjustment-summary tabular-num">
            <span :class="payslip.totalBonus > 0 ? 'bonus' : 'muted-zero'">+{{ formatCurrency(payslip.totalBonus) }}</span>
            <span :class="payslip.totalPenalty > 0 ? 'penalty' : 'muted-zero'">−{{ formatCurrency(payslip.totalPenalty) }}</span>
          </div>
          <span class="stat-note">Tổng thưởng và tổng khấu trừ trong kỳ</span>
        </el-card>
      </section>

      <section class="content-card">
        <div class="section-heading">
          <div>
            <h2>Chi tiết buổi dạy</h2>
            <p>Mỗi buổi hoàn thành có điểm danh được tính một đơn vị lương.</p>
          </div>
          <el-tag type="success" effect="light" class="tabular-num">{{ sessions.filter((item) => item.isEligible).length }} buổi tính lương</el-tag>
        </div>
        <el-table v-loading="loading" :data="sessions" stripe>
          <el-table-column label="Ngày dạy" min-width="140">
            <template #default="{ row }"><span class="tabular-num">{{ formatDate(row.lessonDate) }}</span></template>
          </el-table-column>
          <el-table-column label="Bài học / Chủ đề" min-width="220">
            <template #default="{ row }">{{ row.title || "Chưa đặt tên" }}</template>
          </el-table-column>
          <el-table-column label="Trạng thái" width="145">
            <template #default="{ row }">
              <el-tag :type="row.status === 'COMPLETED' ? 'success' : 'info'" effect="light">{{ row.status }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Điểm danh" width="150">
            <template #default="{ row }">
              <span :class="row.hasAttendance ? 'attendance-done' : 'attendance-missing'">
                {{ row.hasAttendance ? "Đã điểm danh" : "Chưa điểm danh" }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="Tính lương" width="140" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isEligible" type="success" effect="light" class="tabular-num">+1 Đơn vị</el-tag>
              <span v-else class="muted-zero tabular-num">0</span>
            </template>
          </el-table-column>
          <template #empty><el-empty description="Chưa có buổi học trong tháng này." /></template>
        </el-table>
      </section>

      <section class="content-card">
        <div class="section-heading">
          <div>
            <h2>Khoản thưởng / phạt</h2>
            <p>Danh sách điều chỉnh lương cùng lý do được ghi nhận bởi Admin.</p>
          </div>
        </div>
        <el-table :data="adjustments" stripe>
          <el-table-column label="Ngày áp dụng" width="150">
            <template #default="{ row }"><span class="tabular-num">{{ formatDate(row.appliedDate) }}</span></template>
          </el-table-column>
          <el-table-column label="Loại" width="125">
            <template #default="{ row }">
              <el-tag :type="row.type === 'Bonus' ? 'success' : 'danger'" effect="light">
                {{ row.type === "Bonus" ? "Thưởng" : "Phạt" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Số tiền" width="180" align="right">
            <template #default="{ row }">
              <strong :class="[row.type === 'Bonus' ? 'bonus' : 'penalty', 'tabular-num']">
                {{ row.type === "Bonus" ? "+" : "−" }}{{ formatCurrency(row.amount) }}
              </strong>
            </template>
          </el-table-column>
          <el-table-column prop="reason" label="Lý do" min-width="280" />
          <template #empty><el-empty description="Không có khoản thưởng hoặc phạt trong kỳ." /></template>
        </el-table>
      </section>
    </template>

    <el-card v-else-if="!loading" class="empty-card" shadow="never">
      <el-empty description="Không có dữ liệu phiếu lương cho tháng đã chọn." />
    </el-card>
  </main>
</template>

<style scoped>
.payslip-page { display: grid; gap: 20px; max-width: 1280px; margin: 0 auto; padding-bottom: 36px; color: #1c1917; }
.page-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; flex-wrap: wrap; }
.eyebrow { margin: 0 0 4px; color: #ea580c; font-size: 11px; font-weight: 700; letter-spacing: .12em; text-transform: uppercase; }
h1 { margin: 0; color: #1c1917; font-size: clamp(22px, 2.5vw, 28px); font-weight: 700; letter-spacing: -.02em; }
.page-description { margin: 6px 0 0; color: #57534e; font-size: 13.5px; }
.tabular-num { font-variant-numeric: tabular-nums; }
.muted-zero { color: #a8a29e; font-weight: 500; }
.summary-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }
.summary-card { min-height: 144px; border: 1px solid #d6d3d1; border-radius: 8px; background: #ffffff; box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04); }
.summary-card :deep(.el-card__body) { display: flex; height: 100%; flex-direction: column; padding: 18px; }
.net-card { border-left: 4px solid #ea580c; background: #ffffff; }
.stat-top { display: flex; align-items: center; gap: 10px; }
.stat-icon { display: grid; width: 32px; height: 32px; place-items: center; border-radius: 6px; border: 1px solid #fed7aa; background: #fff7ed; color: #ea580c; font-size: 16px; font-weight: 700; }
.session-icon { border-color: #a7f3d0; background: #ecfdf5; color: #047857; }
.adjustment-icon { border-color: #d6d3d1; background: #f5f5f4; color: #44403c; }
.stat-label { color: #57534e; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.04em; }
.stat-value { margin-top: 12px; color: #1c1917; font-size: clamp(20px, 2.8vw, 26px); font-weight: 700; }
.net-card .stat-value { color: #c2410c; }
.stat-value small { color: #78716c; font-size: 13px; font-weight: 600; }
.stat-note { margin-top: auto; padding-top: 8px; color: #78716c; font-size: 11.5px; }
.adjustment-summary { display: flex; flex-wrap: wrap; gap: 6px 16px; margin-top: 14px; font-size: 15px; font-weight: 700; }
.bonus { color: #047857; }
.penalty { color: #be123c; }
.content-card { overflow: hidden; border: 1px solid #d6d3d1; border-radius: 8px; background: #ffffff; box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04); }
.section-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 16px 20px; border-bottom: 1px solid #e7e5e4; background: #fafaf9; }
.section-heading h2 { margin: 0; color: #1c1917; font-size: 16px; font-weight: 700; }
.section-heading p { margin: 4px 0 0; color: #57534e; font-size: 12.5px; }
.attendance-done { color: #047857; font-size: 12.5px; font-weight: 600; }
.attendance-missing { color: #78716c; font-size: 12.5px; }
.empty-card { border: 1px solid #d6d3d1; border-radius: 8px; background: #ffffff; }
.empty-card :deep(.el-card__body) { padding: 16px; }
:deep(.el-tag) { border-radius: 4px; font-weight: 600; }
:deep(.el-table th.el-table__cell) { background: #f5f5f4; color: #44403c; font-size: 11.5px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; border-bottom: 2px solid #d6d3d1; }
:deep(.el-table td.el-table__cell) { font-variant-numeric: tabular-nums; }
:deep(.el-table__row:hover > td.el-table__cell) { background-color: #fff7ed !important; }
@media (max-width: 760px) {
  .summary-grid { grid-template-columns: 1fr; }
  .summary-card { min-height: 126px; }
}
</style>
